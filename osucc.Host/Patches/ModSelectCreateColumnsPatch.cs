using osu.Game.Rulesets.Mods;
using osucc.Client;
using osucc.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Adds the ModType.System column to the mod selector. Targets the private
    /// <c>ModSelectOverlay.createColumns()</c>.
    /// </summary>
    public sealed class ModSelectCreateColumnsPatch : OsuCcPatch
    {
        public ModSelectCreateColumnsPatch()
            : base("osu.Game.Overlays.Mods.ModSelectOverlay", "createColumns", MethodType.Postfix)
        {
        }

        public override bool Condition => ClientMods.ShowSystemMods;

        public void Postfix(object __instance, ref object __result)
        {
            var overlayType = Reflection.GetGameType("osu.Game.Overlays.Mods.ModSelectOverlay");
            var existing = ((IEnumerable)__result).Cast<object>().ToList();

            var createModColumnContent = overlayType?.GetMethod("createModColumnContent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (createModColumnContent == null)
            {
                LogError("createModColumnContent not found");
                return;
            }

            existing.Add(createModColumnContent.Invoke(__instance, new object[] { ModType.System }) ?? throw new InvalidOperationException("createModColumnContent returned null"));

            var containerType = existing[0].GetType();
            var listType = typeof(List<>).MakeGenericType(containerType);
            var list = (IList)Activator.CreateInstance(listType)!;

            foreach (var item in existing)
                list.Add(item);

            __result = list;
            LogInfo("ModSelectOverlay System column appended");
        }
    }
}
