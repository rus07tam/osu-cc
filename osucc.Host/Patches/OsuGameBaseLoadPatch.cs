using osu.Game;
using osucc.Client;
using osucc.Core;

namespace osucc.Patches
{
    /// <summary>
    /// Wires the client into the game once it is constructed and loading. Targets the private
    /// <c>[BackgroundDependencyLoader]</c> <c>load(...)</c> on <c>OsuGameBase</c> — the first
    /// point where the game instance, storage and dependency injection are all available. The
    /// postfix hands the instance to <see cref="ClientBootstrap"/>.
    /// </summary>
    public sealed class OsuGameBaseLoadPatch : OsuCcPatch
    {
        public OsuGameBaseLoadPatch()
            : base("osu.Game.OsuGameBase", "load", m => m.GetParameters().Length == 2, MethodType.Postfix)
        {
        }

        public static void Postfix(OsuGameBase __instance)
        {
            ClientBootstrap.AttachToGame(__instance);
        }
    }
}
