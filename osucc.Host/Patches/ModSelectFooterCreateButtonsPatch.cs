using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Mods;
using osucc.Client;
using osucc.Core;
using osucc.UI.Mods;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Appends the <see cref="RandomModsButton"/> to the mod-select footer.
    /// Targets <c>ModSelectFooterContent.CreateButtons()</c>.
    /// </summary>
    public sealed class ModSelectFooterCreateButtonsPatch : OsuCcPatch
    {
        public ModSelectFooterCreateButtonsPatch()
            : base("osu.Game.Overlays.Mods.ModSelectFooterContent", "CreateButtons", MethodType.Postfix)
        {
        }

        public override bool Condition => ClientMods.ShowRandomModsButton;

        public void Postfix(ModSelectFooterContent __instance, ref IEnumerable<ShearedButton> __result)
        {
            var overlay = readOverlay(__instance);
            if (overlay == null)
            {
                LogError("overlay field not found");
                return;
            }

            __result = __result.Append(new RandomModsButton(overlay));
        }

        private static ModSelectOverlay? readOverlay(ModSelectFooterContent instance)
        {
            var field = typeof(ModSelectFooterContent).GetField("overlay", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field?.GetValue(instance) as ModSelectOverlay;
        }
    }
}
