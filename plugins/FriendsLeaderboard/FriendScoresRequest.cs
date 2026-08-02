using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using System.Collections.Generic;

namespace FriendsLeaderboard
{
    /// <summary>
    /// Fetches all of a single user's scores on a beatmap through the public endpoint
    /// <c>beatmaps/{beatmap}/scores/users/{user}/all</c>. Unlike the friend-scoped leaderboard
    /// endpoint this one does not require an osu!supporter tag, and the response carries the
    /// modern score schema, so it deserializes straight into <see cref="SoloScoreInfo"/>.
    /// </summary>
    internal sealed class FriendScoresRequest : APIRequest<FriendScoresResponse>
    {
        private readonly long beatmapId;
        private readonly int userId;
        private readonly string rulesetShortName;

        public FriendScoresRequest(long beatmapId, int userId, string rulesetShortName)
        {
            this.beatmapId = beatmapId;
            this.userId = userId;
            this.rulesetShortName = rulesetShortName;
        }

        protected override string Target => $"beatmaps/{beatmapId}/scores/users/{userId}/all";

        protected override WebRequest CreateWebRequest()
        {
            var request = base.CreateWebRequest();
            request.AddParameter("mode", rulesetShortName);
            return request;
        }
    }

    /// <summary>The response envelope of <see cref="FriendScoresRequest"/>.</summary>
    internal sealed class FriendScoresResponse
    {
        [JsonProperty("scores")]
        public List<SoloScoreInfo> Scores { get; set; } = new List<SoloScoreInfo>();
    }
}
