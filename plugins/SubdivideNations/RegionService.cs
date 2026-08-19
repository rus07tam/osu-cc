using osu.Framework.IO.Network;
using osu.Framework.Platform;
using osucc.Core;
using osucc.Plugin;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SubdivideNations
{
    /// <summary>A resolved sub-national region for a user, joined from the osuworld API and the embedded dataset.</summary>
    public readonly record struct RegionInfo(string CountryCode, string CountryName, string RegionCode, string RegionName, string? FlagUrl);

    /// <summary>
    /// Resolves a user's sub-national region through the osuworld API (the same source the
    /// osu-subdivide-nations web extension uses). Lookups are coalesced into batches of 50,
    /// cached in memory (60 min) and persisted to the plugin storage so restarts do not
    /// re-hit the community API. Any failure resolves to <c>null</c> — never throws.
    /// </summary>
    public static class RegionService
    {
        private const string apiBase = "https://osuworld.octo.moe/api/";
        private const string cacheFilename = "regions-cache.json";
        private const int batchSize = 50;
        private static readonly TimeSpan cacheTtl = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan failureTtl = TimeSpan.FromMinutes(30);

        private static readonly ConcurrentDictionary<int, CacheEntry> memoryCache = new();
        private static readonly ConcurrentDictionary<int, TaskCompletionSource<RegionInfo?>> pending = new();
        private static readonly ConcurrentDictionary<int, string> diskCache = new();
        private static readonly object batchLock = new();
        private static readonly HashSet<int> pendingIds = new();

        private static Dictionary<string, CountryData> countries = new();
        private static Storage? storage;
        private static Func<bool> isEnabled = static () => true;
        private static bool batchScheduled;

        /// <summary>Whether region resolution is active at all (settings toggle).</summary>
        public static bool Enabled => isEnabled();

        /// <summary>The plugin host, set by <see cref="Attach"/> so the service can log into its own file.</summary>
        private static IOsuCcPluginHost host = null!;

        public static void SetEnabled(Func<bool> enabled) => isEnabled = enabled;

        /// <summary>
        /// Loads the embedded region dataset and the persisted lookup cache. Called from
        /// <see cref="IOsuCcPlugin.AttachToGame"/> once the plugin storage exists.
        /// </summary>
        public static void Attach(Storage? pluginStorage, IOsuCcPluginHost host)
        {
            storage = pluginStorage;
            RegionService.host = host;
            loadDataset();
            loadDiskCache();
        }

        /// <summary>
        /// Resolves the region for a user id. Concurrent requests for the same id are deduplicated;
        /// ids unknown at call time are batched and flushed together shortly after.
        /// </summary>
        public static Task<RegionInfo?> GetRegionAsync(int userId)
        {
            if (!Enabled)
                return Task.FromResult<RegionInfo?>(null);

            if (memoryCache.TryGetValue(userId, out var cached))
                return Task.FromResult(cached.Region);

            // Completed task sources may still linger here right after a flush; reuse their result.
            if (pending.TryGetValue(userId, out var existing) && existing.Task.IsCompleted)
                return existing.Task;

            var tcs = pending.GetOrAdd(userId, _ => new TaskCompletionSource<RegionInfo?>(TaskCreationOptions.RunContinuationsAsynchronously));

            lock (batchLock)
            {
                pendingIds.Add(userId);

                if (batchScheduled)
                    return tcs.Task;

                batchScheduled = true;
                _ = scheduleFlushAsync();
            }

            return tcs.Task;
        }

        private static async Task scheduleFlushAsync()
        {
            await Task.Delay(30).ConfigureAwait(false);

            List<int> ids;
            lock (batchLock)
            {
                ids = pendingIds.ToList();
                pendingIds.Clear();
                batchScheduled = false;
            }

            foreach (var chunk in ids.Chunk(batchSize))
                await fetchBatchAsync(chunk).ConfigureAwait(false);
        }

        private static async Task fetchBatchAsync(int[] ids)
        {
            try
            {
                var request = new WebRequest(apiBase + "subdiv/users");
                request.AddParameter("ids", string.Join(",", ids));

                await request.PerformAsync().ConfigureAwait(false);

                string? json = request.GetResponseString();
                if (string.IsNullOrEmpty(json))
                {
                    failAll(ids);
                    return;
                }

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    failAll(ids);
                    return;
                }

                var resolved = new HashSet<int>();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (!element.TryGetProperty("id", out var idElement) || idElement.ValueKind != JsonValueKind.Number)
                        continue;

                    int id = idElement.GetInt32();
                    string? countryCode = getString(element, "country_id");
                    string? regionCode = getString(element, "region_id");

                    complete(id, resolve(countryCode, regionCode));
                    resolved.Add(id);
                }

                foreach (int id in ids)
                {
                    if (!resolved.Contains(id))
                        complete(id, null);
                }
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"osuworld batch fetch failed ({string.Join(",", ids)}): {ex.Message}");
                failAll(ids);
            }
            finally
            {
                writeDiskCache();
            }
        }

        private static void failAll(int[] ids)
        {
            foreach (int id in ids)
                complete(id, null);
        }

        private static void complete(int userId, RegionInfo? region)
        {
            bool known = region != null;
            DateTimeOffset expires = DateTimeOffset.UtcNow + (known ? cacheTtl : failureTtl);

            if (known)
            {
                memoryCache[userId] = new CacheEntry(region!.Value, expires);
                diskCache[userId] = $"{region.Value.CountryCode}|{region.Value.RegionCode}";
            }
            else
            {
                memoryCache[userId] = new CacheEntry(null, expires);
                diskCache[userId] = "0";
            }

            if (pending.TryRemove(userId, out var tcs))
                tcs.TrySetResult(region);
        }

        private static RegionInfo? resolve(string? countryCode, string? regionCode)
        {
            if (string.IsNullOrEmpty(countryCode) || string.IsNullOrEmpty(regionCode))
                return null;

            if (!countries.TryGetValue(countryCode, out var country))
                return null;

            if (!country.Regions.TryGetValue(regionCode, out var region))
                return null;

            return new RegionInfo(
                countryCode,
                country.Name,
                regionCode,
                region.Name,
                string.IsNullOrEmpty(region.Flag) ? null : region.Flag);
        }

        private static string? getString(JsonElement element, string property)
            => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static void loadDataset()
        {
            try
            {
                var assembly = typeof(RegionService).Assembly;
                using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Regions.json");
                if (stream == null)
                {
                    host.Log(LogLevel.Error, "Regions.json embedded resource missing");
                    return;
                }

                using var doc = JsonDocument.Parse(stream);
                var parsed = new Dictionary<string, CountryData>(StringComparer.Ordinal);

                foreach (var countryElement in doc.RootElement.EnumerateObject())
                {
                    if (!countryElement.Value.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                        continue;

                    var regions = new Dictionary<string, RegionData>(StringComparer.Ordinal);
                    if (countryElement.Value.TryGetProperty("regions", out var regionsElement))
                    {
                        foreach (var regionElement in regionsElement.EnumerateObject())
                        {
                            string regionName = getString(regionElement.Value, "name") ?? string.Empty;
                            string flag = getString(regionElement.Value, "flag") ?? string.Empty;
                            regions[regionElement.Name] = new RegionData(regionName, flag);
                        }
                    }

                    parsed[countryElement.Name] = new CountryData(nameElement.GetString() ?? string.Empty, regions);
                }

                countries = parsed;
                host.Log(LogLevel.Info, $"dataset loaded ({countries.Count} countries)");
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"failed to load dataset: {ex.Message}");
            }
        }

        private static void loadDiskCache()
        {
            if (storage == null)
                return;

            try
            {
                if (!storage.Exists(cacheFilename))
                    return;

                using var stream = storage.GetStream(cacheFilename);
                using var doc = JsonDocument.Parse(stream);

                foreach (var element in doc.RootElement.EnumerateObject())
                {
                    if (!int.TryParse(element.Name, out int userId) || element.Value.ValueKind != JsonValueKind.String)
                        continue;

                    string value = element.Value.GetString() ?? string.Empty;
                    bool known = value != "0";

                    var region = known ? resolveFromPair(value) : null;
                    var ttl = known ? TimeSpan.FromHours(6) : failureTtl;
                    memoryCache[userId] = new CacheEntry(region, DateTimeOffset.UtcNow + ttl);
                    diskCache[userId] = value;
                }
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"failed to load lookup cache: {ex.Message}");
            }
        }

        private static RegionInfo? resolveFromPair(string pair)
        {
            string[] parts = pair.Split('|');
            return parts.Length == 2 ? resolve(parts[0], parts[1]) : null;
        }

        private static void writeDiskCache()
        {
            if (storage == null)
                return;

            try
            {
                using var stream = storage.GetStream(cacheFilename, FileAccess.Write, FileMode.Create);
                JsonSerializer.Serialize(stream, diskCache);
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"failed to persist lookup cache: {ex.Message}");
            }
        }

        private readonly record struct CacheEntry(RegionInfo? Region, DateTimeOffset Expires);

        private sealed record CountryData(string Name, Dictionary<string, RegionData> Regions);

        private sealed record RegionData(string Name, string Flag);
    }
}
