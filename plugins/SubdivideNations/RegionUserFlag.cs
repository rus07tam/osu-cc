using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;
using osu.Game.Users.Drawables;
using osuTK;

namespace SubdivideNations
{
    /// <summary>
    /// User-panel flag replacement for <c>UserPanel.CreateFlag()</c>: keeps the country flag and
    /// overlays a compact region flag badge (with a region-name tooltip) once the region resolves.
    /// Deriving from <see cref="UpdateableFlag"/> keeps the return type of the patched method, so
    /// it slots into every panel layout untouched. The badge joins via <see cref="CompositeDrawable.AddInternal"/>
    /// because <see cref="ModelBackedDrawable{T}"/> (the flag's base) has no public child accessor.
    /// </summary>
    public partial class RegionUserFlag : UpdateableFlag, IHasTooltip
    {
        private readonly int userId;
        private bool regionApplied;

        public LocalisableString TooltipText { get; set; }

        public RegionUserFlag(APIUser user, UpdateableFlag source)
        {
            userId = user.OnlineID;
            CountryCode = source.CountryCode;
            Action = source.Action;
            Size = source.Size;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            _ = loadRegionAsync();
        }

        private async Task loadRegionAsync()
        {
            var region = await RegionService.GetRegionAsync(userId).ConfigureAwait(false);
            if (region == null || !IsAlive)
                return;

            var resolved = region.Value;
            Scheduler.Add(() => applyRegion(resolved));
        }

        private void applyRegion(RegionInfo region)
        {
            if (!IsAlive || regionApplied)
                return;

            regionApplied = true;
            TooltipText = region.RegionName;

            if (RegionFlagStore.ShowFlags && region.FlagUrl != null)
                _ = loadFlagAsync(region);
        }

        private async Task loadFlagAsync(RegionInfo region)
        {
            byte[]? bytes = await RegionFlagStore.GetFlagPngAsync(region.RegionCode, region.FlagUrl!).ConfigureAwait(false);
            if (bytes == null || !IsAlive)
                return;

            Scheduler.Add(() =>
            {
                if (!IsAlive)
                    return;

                var texture = RegionFlagStore.CreateTexture(bytes);
                if (texture == null)
                    return;

                var badgeSize = new Vector2(16, 16 / RegionFlagSprite.AspectRatio);
                AddInternal(new RegionFlagSprite(texture)
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Depth = -1,
                    Size = badgeSize,
                });
            });
        }
    }
}
