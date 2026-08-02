using HarmonyLib;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays.Profile;
using osu.Game.Overlays.Profile.Header;
using osu.Game.Users.Drawables;
using osucc.Core;
using osuTK;
using System.Runtime.CompilerServices;

namespace SubdivideNations
{
    /// <summary>
    /// Shows the region on the profile header (<c>TopHeaderContainer</c>): appends the region name
    /// to the country text ("Spain / Catalonia") and shows the region flag right after the country
    /// flag. The country/region fields are read reflectively because they are private to the
    /// container. A per-instance <see cref="HeaderState"/> tracks a single region flag and the
    /// region text is re-applied on every <c>updateUser</c>, so re-fetches never lose the suffix
    /// and switching users replaces the flag instead of stacking copies.
    /// </summary>
    internal static class TopHeaderContainerPatch
    {
        private static readonly ConditionalWeakTable<TopHeaderContainer, HeaderState> states = new();

        public static bool Install(Harmony harmony)
            => PatchHelper.AttachPostfix(harmony, "osu.Game.Overlays.Profile.Header.TopHeaderContainer", "updateUser", typeof(TopHeaderContainerPatch), nameof(Postfix));

        private static void Postfix(TopHeaderContainer __instance, UserProfileData? data)
        {
            var user = data?.User;
            if (user == null)
                return;

            int userId = user.Id;
            var state = states.GetValue(__instance, static _ => new HeaderState());

            if (state.LastUserId != userId)
            {
                state.ClearRegionFlag();
                state.LastUserId = userId;
            }

            var task = RegionService.GetRegionAsync(userId);
            if (task.IsCompletedSuccessfully && task.Result is { } region)
                applyRegion(__instance, state, userId, region);
            else
                _ = resolveAndApplyAsync(__instance, state, userId, task);
        }

        private static async Task resolveAndApplyAsync(TopHeaderContainer instance, HeaderState state, int userId, Task<RegionInfo?> task)
        {
            var region = await task.ConfigureAwait(false);
            if (region == null)
                return;

            var scheduler = Reflection.GetScheduler(instance);
            if (scheduler == null)
                return;

            scheduler.Add(() => applyRegion(instance, state, userId, region.Value));
        }

        private static void applyRegion(TopHeaderContainer instance, HeaderState state, int userId, RegionInfo region)
        {
            if (!instance.IsAlive || instance.User.Value?.User?.Id != userId)
                return;

            var countryText = Reflection.FindField(typeof(TopHeaderContainer), "userCountryText")?.GetValue(instance) as OsuSpriteText;
            if (countryText != null && !countryText.Text.ToString().Contains(region.RegionName, StringComparison.Ordinal))
                countryText.Text = $"{countryText.Text} / {region.RegionName}";

            if (RegionFlagStore.ShowFlags && region.FlagUrl != null)
                _ = applyFlagAsync(instance, state, userId, region);
        }

        private static async Task applyFlagAsync(TopHeaderContainer instance, HeaderState state, int userId, RegionInfo region)
        {
            byte[]? bytes = await RegionFlagStore.GetFlagPngAsync(region.RegionCode, region.FlagUrl!).ConfigureAwait(false);
            if (bytes == null)
                return;

            var scheduler = Reflection.GetScheduler(instance);
            if (scheduler == null)
                return;

            scheduler.Add(() =>
            {
                if (!instance.IsAlive || instance.User.Value?.User?.Id != userId)
                    return;

                var texture = RegionFlagStore.CreateTexture(bytes);
                if (texture == null)
                    return;

                state.EnsureFlagContainer(instance);
                state.SetRegionFlag(new RegionFlagSprite(texture, region.RegionName)
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Size = new Vector2(28, 20),
                });
            });
        }

        /// <summary>
        /// Per-header state: wraps the country flag once so the region flag can sit directly after
        /// it, and holds the single currently-displayed region flag. Drawables are kept by weak
        /// reference — the header's tree owns them, and the state must not outlive its header.
        /// </summary>
        private sealed class HeaderState
        {
            public int LastUserId;

            private WeakReference<FillFlowContainer>? flagWrap;
            private WeakReference<RegionFlagSprite>? regionFlag;

            /// <summary>
            /// Replaces the header's <c>userFlag</c> with a horizontal flow holding it, so a region
            /// flag added to that flow is structurally the element right after the country flag.
            /// Runs once per header; no-op afterwards.
            /// </summary>
            public void EnsureFlagContainer(TopHeaderContainer instance)
            {
                if (flagWrap?.TryGetTarget(out _) == true)
                    return;

                var userFlagField = Reflection.FindField(typeof(TopHeaderContainer), "userFlag");
                if (userFlagField?.GetValue(instance) is not UpdateableFlag userFlag || userFlag.Parent is not FillFlowContainer flow)
                    return;

                float flagPosition = flow.GetLayoutPosition(userFlag);
                int index = flow.IndexOf(userFlag);
                if (index < 0)
                    return;

                var wrap = new FillFlowContainer
                {
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(4, 0),
                    AutoSizeAxes = Axes.Both,
                };

                flow.Remove(userFlag, false);
                flow.Insert(index, wrap);
                wrap.Add(userFlag);
                flow.SetLayoutPosition(wrap, flagPosition - 1f);
                flagWrap = new WeakReference<FillFlowContainer>(wrap);
            }

            /// <summary>Replaces the currently displayed region flag (if any).</summary>
            public void SetRegionFlag(RegionFlagSprite sprite)
            {
                ClearRegionFlag();

                if (flagWrap == null || !flagWrap.TryGetTarget(out var wrap))
                    return;

                wrap.Add(sprite);
                regionFlag = new WeakReference<RegionFlagSprite>(sprite);
            }

            /// <summary>Removes the currently displayed region flag, if any.</summary>
            public void ClearRegionFlag()
            {
                if (regionFlag?.TryGetTarget(out var sprite) == true
                    && flagWrap?.TryGetTarget(out var wrap) == true
                    && wrap.IsAlive)
                {
                    wrap.Remove(sprite, true);
                }

                regionFlag = null;
            }
        }
    }
}
