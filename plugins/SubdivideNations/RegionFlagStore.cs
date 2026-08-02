using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Network;
using osu.Framework.Platform;
using osucc.Core;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace SubdivideNations
{
    /// <summary>
    /// Loads region flag images (PNG) from the web and hands back a ready <see cref="Texture"/>.
    /// Only PNG flags can be rendered — osu's texture pipeline is raster-based, so Wikimedia SVG
    /// flags are re-requested as server-rasterized PNG thumbnails and anything else (raw SVGs,
    /// imgur, etc.) degrades to no flag. Bytes are cached in memory and persisted to plugin
    /// storage for offline reuse.
    /// </summary>
    public static class RegionFlagStore
    {
        private const string flagCachePrefix = "flag-";
        private const int thumbWidth = 120;

        private static readonly ConcurrentDictionary<string, byte[]> flagCache = new();
        private static readonly ConcurrentDictionary<string, Task<byte[]?>> inflight = new();
        private static readonly ConcurrentDictionary<string, DateTimeOffset> fetchFailures = new();
        private static readonly SemaphoreSlim downloadGate = new(4);
        private static readonly TimeSpan failureBackoff = TimeSpan.FromMinutes(30);
        private static readonly Regex wikimediaSvg = new(
            @"^https://upload\.wikimedia\.org/(?<prefix>wikipedia/[^/]+/)(?<hash>[^/]+/[^/]+/)(?<file>[^/]+\.svg)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static Storage? storage;
        private static Func<bool> showFlags = static () => true;

        /// <summary>Whether region flag images are enabled (settings toggle).</summary>
        public static bool ShowFlags => showFlags();

        public static void SetShowFlags(Func<bool> enabled) => showFlags = enabled;

        public static void Attach(Storage? pluginStorage) => storage = pluginStorage;

        /// <summary>
        /// Resolves the PNG bytes for a region, downloading and caching them on first use.
        /// Returns <c>null</c> if the flag cannot be rendered (non-PNG source, network failure).
        /// </summary>
        public static Task<byte[]?> GetFlagPngAsync(string regionCode, string flagUrl)
        {
            if (!showFlags() || string.IsNullOrEmpty(flagUrl))
                return Task.FromResult<byte[]?>(null);

            if (flagCache.TryGetValue(regionCode, out var cached))
                return Task.FromResult<byte[]?>(cached);

            // Do not hammer a host that recently rejected us (404/429).
            if (fetchFailures.TryGetValue(regionCode, out var failedAt) && DateTimeOffset.UtcNow - failedAt < failureBackoff)
                return Task.FromResult<byte[]?>(null);

            var task = inflight.GetOrAdd(regionCode, _ => fetchFlagAsync(regionCode, flagUrl));
            return task;
        }

        /// <summary>Builds a <see cref="Texture"/> from PNG bytes. Must run on the update thread.</summary>
        public static Texture? CreateTexture(byte[] pngBytes) => TextureHelper.FromBytes(pngBytes);

        private static async Task<byte[]?> fetchFlagAsync(string regionCode, string flagUrl)
        {
            try
            {
                byte[]? cachedOnDisk = readDisk(regionCode);
                if (cachedOnDisk != null)
                {
                    flagCache[regionCode] = cachedOnDisk;
                    fetchFailures.TryRemove(regionCode, out _);
                    return cachedOnDisk;
                }

                string? pngUrl = toPngUrl(flagUrl);
                if (pngUrl == null)
                    return null;

                await downloadGate.WaitAsync().ConfigureAwait(false);
                try
                {
                    var request = new WebRequest(pngUrl)
                    {
                        Timeout = 15000
                    };

                    await request.PerformAsync().ConfigureAwait(false);

                    byte[]? bytes = request.GetResponseData();
                    if (bytes == null || bytes.Length == 0)
                    {
                        recordFailure(regionCode);
                        return null;
                    }

                    fetchFailures.TryRemove(regionCode, out _);
                    flagCache[regionCode] = bytes;
                    writeDisk(regionCode, bytes);
                    return bytes;
                }
                finally
                {
                    downloadGate.Release();
                }
            }
            catch (Exception ex)
            {
                TimingLog.Error($"SubdivideNations: failed to fetch region flag ({regionCode}): {ex.Message}");
                recordFailure(regionCode);
                return null;
            }
            finally
            {
                inflight.TryRemove(regionCode, out _);
            }
        }

        private static void recordFailure(string regionCode)
            => fetchFailures[regionCode] = DateTimeOffset.UtcNow;

        /// <summary>
        /// Transforms a raw flag URL into a directly downloadable PNG. PNG sources pass through;
        /// Wikimedia SVGs become a thumbnail request (the wiki rasterizes them server-side).
        /// </summary>
        private static string? toPngUrl(string url)
        {
            if (url.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return url;

            var match = wikimediaSvg.Match(url);
            if (!match.Success)
                return null;

            string file = Uri.EscapeDataString(match.Groups["file"].Value);
            return $"https://upload.wikimedia.org/{match.Groups["prefix"].Value}thumb/{match.Groups["hash"].Value}{file}/{thumbWidth}px-{file}.png";
        }

        private static byte[]? readDisk(string regionCode)
        {
            if (storage == null)
                return null;

            try
            {
                string path = $"{flagCachePrefix}{regionCode}.png";
                if (!storage.Exists(path))
                    return null;

                using var stream = storage.GetStream(path);
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                return memory.ToArray();
            }
            catch (Exception ex)
            {
                TimingLog.Error($"SubdivideNations: failed to read cached flag ({regionCode}): {ex.Message}");
                return null;
            }
        }

        private static void writeDisk(string regionCode, byte[] bytes)
        {
            if (storage == null)
                return;

            try
            {
                using var stream = storage.GetStream($"{flagCachePrefix}{regionCode}.png", FileAccess.Write, FileMode.Create);
                stream.Write(bytes);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"SubdivideNations: failed to persist flag ({regionCode}): {ex.Message}");
            }
        }
    }
}
