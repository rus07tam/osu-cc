using osu.Framework.Extensions.Color4Extensions;
using osuTK.Graphics;

namespace osucc.Core
{
    /// <summary>
    /// Shared colour palette for osu!cc surfaces, mirroring the game's
    /// <see cref="osu.Game.Graphics.OsuColour"/> values so toasts and status text match the stock styling.
    /// </summary>
    public static class OsuCcColours
    {
        public static readonly Color4 Success = Color4Extensions.FromHex("88b300"); // OsuColour.Green
        public static readonly Color4 Error = Color4Extensions.FromHex("ed1121"); // OsuColour.Red
        public static readonly Color4 Info = Color4Extensions.FromHex("05f4fd"); // OsuColour.Cyan
        public static readonly Color4 Disabled = Color4Extensions.FromHex("d3d3d3"); // neutral grey
        public static readonly Color4 Pink = Color4Extensions.FromHex("ff66aa"); // OsuColour.Pink
    }
}
