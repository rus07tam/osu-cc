using HarmonyLib;
using osu.Game.Rulesets.Mods;
using osucc.Client;
using osucc.Core;
using System.Collections;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Adds the ModType.System column to the mod selector. Targets the private
    /// <c>ModSelectOverlay.createColumns()</c>. When the flag is on, the postfix appends a System
    /// column (via the private <c>createModColumnContent</c>). Live toggling on open overlays is
    /// handled by <see cref="ClientMods.RefreshOverlays"/>.
    /// </summary>
    public static class ModSelectCreateColumnsPatch
    {
        // Resolved from the *declaring* type: createModColumnContent is private to
        // ModSelectOverlay and would not be found when querying a derived type.
        private static Type? overlayType;

        public static bool Install()
        {
            overlayType = Reflection.GetGameType("osu.Game.Overlays.Mods.ModSelectOverlay");
            var method = overlayType?.GetMethod("createColumns", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (method == null)
            {
                TimingLog.Error("ModSelectCreateColumnsPatch: createColumns method not found");
                return false;
            }

            HookDependencies.Create("dev.osucc.mods.columns").Patch(method, postfix: Reflection.HarmonyMethod(typeof(ModSelectCreateColumnsPatch), nameof(Postfix)));
            TimingLog.Info("ModSelectOverlay.createColumns patched (postfix)");
            return true;
        }

        private static void Postfix(object __instance, ref object __result)
        {
            if (!ClientMods.ShowSystemMods)
                return;

            try
            {
                // __result is IEnumerable<ColumnDimContainer>; the type is a private nested
                // type, so rebuild the enumerable reflectively with the System column appended.
                var existing = ((IEnumerable)__result).Cast<object>().ToList();

                var createModColumnContent = overlayType?.GetMethod("createModColumnContent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (createModColumnContent == null)
                {
                    TimingLog.Error("ModSelectCreateColumnsPatch: createModColumnContent not found");
                    return;
                }

                existing.Add(createModColumnContent.Invoke(__instance, new object[] { ModType.System }) ?? throw new InvalidOperationException("createModColumnContent returned null"));

                var containerType = existing[0].GetType();
                var listType = typeof(List<>).MakeGenericType(containerType);
                var list = (IList)Activator.CreateInstance(listType)!;

                foreach (var item in existing)
                    list.Add(item);

                __result = list;
                TimingLog.Info("ModSelectOverlay System column appended");
            }
            catch (Exception ex)
            {
                TimingLog.Error($"ModSelectCreateColumnsPatch.Postfix: {ex}");
            }
        }
    }
}
