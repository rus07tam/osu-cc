using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Screens.Play.Leaderboards;
using osucc.Core;
using osucc.Plugin;
using System.Collections.Concurrent;
using System.Reflection;

namespace FriendsLeaderboard
{
    /// <summary>
    /// Builds the friend leaderboard client-side: for the current beatmap and ruleset, fetches
    /// every friend's (and the local user's) scores through the public per-user endpoint and
    /// completes the intercepted <c>GetScoresRequest</c> with the aggregated result. The request
    /// is finished via the internal <c>TriggerSuccess</c> so the normal leaderboard/scores
    /// consumers work unchanged.
    /// </summary>
    internal static class FriendsScoresAggregator
    {
        private const int maxConcurrency = 16;
        private const int maxDisplayedScores = 50;
        private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan friendsLoadTimeout = TimeSpan.FromSeconds(4);

        private static Func<bool>? enabledProvider;

        private static readonly ConcurrentDictionary<(long, string), CachedResult> cache = new();

        private static readonly FieldInfo? apiField = Reflection.FindField(typeof(APIRequest), "API");
        private static readonly FieldInfo? scopeField = Reflection.FindField(typeof(GetScoresRequest), "scope");
        private static readonly FieldInfo? beatmapField = Reflection.FindField(typeof(GetScoresRequest), "beatmapInfo");
        private static readonly FieldInfo? rulesetField = Reflection.FindField(typeof(GetScoresRequest), "ruleset");

        private static MethodInfo? triggerSuccessMethod;
        private static MethodInfo? triggerFailureMethod;

        public static void SetEnabledProvider(Func<bool> provider) => enabledProvider = provider;

        /// <summary>The plugin host, set by the plugin on load so the aggregator can log into its own file.</summary>
        private static IOsuCcPluginHost host = null!;

        public static void SetHost(IOsuCcPluginHost host) => FriendsScoresAggregator.host = host;

        internal static bool Enabled => enabledProvider?.Invoke() ?? false;

        /// <summary>Whether the request is a friend-scoped leaderboard fetch that this aggregator should handle.</summary>
        public static bool ShouldIntercept(APIRequest request)
        {
            if (!Enabled || request is not GetScoresRequest)
                return false;

            return scopeField?.GetValue(request) is BeatmapLeaderboardScope scope && scope == BeatmapLeaderboardScope.Friend;
        }

        /// <summary>
        /// Called from the <c>APIRequest.Perform</c> prefix (API queue thread). Reads the request's
        /// criteria and kicks off the aggregation on a background task, waiting for the friends list
        /// to populate first.
        /// </summary>
        public static void BeginAggregation(APIRequest request)
        {
            try
            {
                var api = apiField?.GetValue(request) as IAPIProvider;
                var beatmap = beatmapField?.GetValue(request) as IBeatmapInfo;
                var ruleset = rulesetField?.GetValue(request) as IRulesetInfo;

                if (api == null || beatmap == null || beatmap.OnlineID <= 0 || ruleset == null || string.IsNullOrEmpty(ruleset.ShortName))
                {
                    fail(request);
                    return;
                }

                long beatmapId = beatmap.OnlineID;
                string rulesetName = ruleset.ShortName;
                var cacheKey = (beatmapId, rulesetName);

                if (cache.TryGetValue(cacheKey, out var cached) && cached.IsFresh)
                {
                    complete(request, cached.Collection);
                    return;
                }

                cache.TryRemove(cacheKey, out _);

                int localId = api.LocalUser.Value?.Id ?? 0;

                Task.Run(async () =>
                {
                    // the friends list may still be loading (GetFriendsRequest is queued on login), so wait
                    // briefly for it rather than aggregating over only the local user and caching that empties out.
                    await waitForFriends(api);

                    var users = collectUsers(api);
                    if (users.Length == 0)
                    {
                        complete(request, new APIScoresCollection());
                        return;
                    }

                    aggregate(request, api, beatmapId, rulesetName, localId, users, cacheKey);
                });
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"aggregation start failed: {ex}");
                fail(request);
            }
        }

        /// <summary>
        /// Waits up to <see cref="friendsLoadTimeout"/> for the friends list to populate, so the aggregation
        /// runs against the real friend list instead of just the local user when <c>GetFriendsRequest</c>
        /// is still in flight. Returns immediately when the list is already non-empty.
        /// </summary>
        private static async Task waitForFriends(IAPIProvider api)
        {
            if (api.LocalUserState.Friends.Count > 0)
                return;

            var deadline = DateTimeOffset.UtcNow + friendsLoadTimeout;

            while (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(50);

                if (api.LocalUserState.Friends.Count > 0)
                    return;
            }
        }

        private static (int Id, APIUser? User)[] collectUsers(IAPIProvider api)
        {
            var users = new List<(int, APIUser?)>();

            var local = api.LocalUser.Value;
            if (local != null && local.Id > 0)
                users.Add((local.Id, local));

            foreach (var relation in api.LocalUserState.Friends)
            {
                if (relation.TargetID > 0)
                    users.Add((relation.TargetID, relation.TargetUser));
            }

            return users.GroupBy(u => u.Item1).Select(g => g.First()).ToArray();
        }

        private static void aggregate(APIRequest request, IAPIProvider api, long beatmapId, string rulesetName, int localId, (int Id, APIUser? User)[] users, (long, string) cacheKey)
        {
            try
            {
                complete(request, buildCollection(api, beatmapId, rulesetName, localId, users, cacheKey));
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"aggregation failed: {ex}");
                fail(request);
            }
        }

        private static APIScoresCollection buildCollection(IAPIProvider api, long beatmapId, string rulesetName, int localId, (int Id, APIUser? User)[] users, (long, string) cacheKey)
        {
            var results = new ConcurrentBag<SoloScoreInfo>();

            Parallel.ForEach(users, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency }, user =>
            {
                var req = new FriendScoresRequest(beatmapId, user.Id, rulesetName);

                try
                {
                    api.Perform(req);

                    if (req.Response?.Scores is not { Count: > 0 } scores)
                        return;

                    foreach (var score in scores)
                    {
                        if (!score.Passed)
                            continue;

                        score.User ??= user.User;
                        results.Add(score);
                    }
                }
                catch (Exception ex)
                {
                    host.Log(LogLevel.Info, $"no scores for user {user.Id}: {ex.Message}");
                }
            });

            var allScores = results
                .GroupBy(s => s.ID ?? s.LegacyScoreId)
                .Select(g => g.OrderByDescending(s => s.TotalScore).First())
                .OrderByDescending(s => s.TotalScore)
                .ToList();

            var localScore = allScores.FirstOrDefault(s => s.UserID == localId);

            APIScoreWithPosition? userScore = null;
            if (localScore != null)
            {
                userScore = new APIScoreWithPosition
                {
                    Score = localScore,
                    Position = 1 + allScores.Count(s => s.TotalScore > localScore.TotalScore),
                };
            }

            var collection = new APIScoresCollection
            {
                Scores = allScores.Take(maxDisplayedScores).ToList(),
                ScoresCount = allScores.Count,
                UserScore = userScore,
            };

            // don't cache empty results: they are usually caused by the friends list not being loaded yet,
            // and would otherwise keep showing "no records" for the whole cache duration.
            if (allScores.Count > 0)
                cache[cacheKey] = new CachedResult(collection, DateTimeOffset.UtcNow);

            return collection;
        }

        private static void complete(APIRequest request, APIScoresCollection collection)
        {
            if (request.CompletionState != APIRequestCompletionState.Waiting)
                return;

            try
            {
                // APIRequest<APIScoresCollection>.TriggerSuccess(T) is internal to osu.Game.
                triggerSuccessMethod ??= typeof(APIRequest<APIScoresCollection>)
                    .GetMethod("TriggerSuccess", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(APIScoresCollection) }, null);

                triggerSuccessMethod?.Invoke(request, new object[] { collection });
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"completing request failed: {ex}");
            }
        }

        private static void fail(APIRequest request)
        {
            if (request.CompletionState != APIRequestCompletionState.Waiting)
                return;

            try
            {
                // APIRequest.TriggerFailure(Exception) is internal to osu.Game.
                triggerFailureMethod ??= typeof(APIRequest)
                    .GetMethod("TriggerFailure", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Exception) }, null);

                triggerFailureMethod?.Invoke(request, new object[] { new InvalidOperationException("Friends leaderboard could not be built") });
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"failing request failed: {ex}");
            }
        }

        private sealed record CachedResult(APIScoresCollection Collection, DateTimeOffset FetchedAt)
        {
            public bool IsFresh => DateTimeOffset.UtcNow - FetchedAt < cacheDuration;
        }
    }
}
