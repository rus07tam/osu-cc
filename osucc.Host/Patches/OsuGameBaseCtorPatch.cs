using osucc.Client;
using osucc.Core;
using System;

namespace osucc.Patches
{
    /// <summary>
    /// Branding patch: postfixes the <c>OsuGameBase</c> parameterless ctor and overwrites its
    /// <c>Name</c> with the client branding (the window title reads it in
    /// <c>updateWindowTitle()</c>).
    /// </summary>
    public sealed class OsuGameBaseCtorPatch : OsuCcPatch
    {
        public OsuGameBaseCtorPatch()
            : base("osu.Game.OsuGameBase", Type.EmptyTypes)
        {
        }

        public void Postfix(object __instance)
        {
            ClientHostTasks.CaptureOriginalGameName(Reflection.GetName(__instance as osu.Framework.Graphics.Drawable));
            Reflection.SetName(__instance as osu.Framework.Graphics.Drawable, ClientApi.BrandingName);
            LogInfo($"Name set to \"{ClientApi.BrandingName}\" on {__instance.GetType().FullName}");
        }
    }
}
