using osu.Framework.Graphics;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osucc.Core;
using System.Collections;
using System.Reflection;

namespace osucc.Client
{
    /// <summary>
    /// Live access to the mod-related Specials settings, plus runtime management of the
    /// mod-select overlay columns so toggling a setting reflects immediately on open overlays.
    /// </summary>
    public static class ClientMods
    {
        private static readonly object lockObject = new();
        private static readonly HashSet<ModSelectOverlay> overlays = new();

        // GetField/GetMethod with NonPublic on a derived type does not see private members of
        // a base class, so resolve from the *declaring* type (ModSelectOverlay). The column
        // flow is a FillFlowContainer<ColumnDimContainer>; the non-generic FillFlowContainer is
        // not a base of it, so everything below the field read is handled reflectively too.
        private static readonly Lazy<Type?> overlayType = new(() =>
            AppDomain.CurrentDomain.GetAssemblies()
                     .FirstOrDefault(a => a.GetName().Name == "osu.Game")
                     ?.GetType("osu.Game.Overlays.Mods.ModSelectOverlay"));

        private static readonly Lazy<MethodInfo?> createLocalModsMethod = new(() =>
            overlayType.Value?.GetMethod("createLocalMods", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

        private static readonly Lazy<FieldInfo?> columnFlowField = new(() =>
            overlayType.Value?.GetField("columnFlow", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

        private static readonly Lazy<MethodInfo?> createModColumnContentMethod = new(() =>
            overlayType.Value?.GetMethod("createModColumnContent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

        public static bool AllowIncompatibleMods => getFlag(SpecialsSetting.AllowIncompatibleMods);

        public static bool ShowSystemMods => getFlag(SpecialsSetting.ShowSystemMods);

        public static bool ShowRandomModsButton => getFlag(SpecialsSetting.ShowRandomModsButton);

        public static bool CelebrateNewRecord => getFlag(SpecialsSetting.CelebrateNewRecord);

        public static bool DisableSoloScoreSubmission => getFlag(SpecialsSetting.DisableSoloScoreSubmission);

        private static bool getFlag(SpecialsSetting setting)
            => ClientApi.Config?.GetBindable<bool>(setting).Value ?? false;

        /// <summary>
        /// Randomly selects a valid set of mods on the given overlay, replacing the current
        /// selection. Assigning <see cref="ModSelectOverlay.SelectedMods"/> drives the game's own
        /// <c>updateFromExternalSelection</c> flow, so columns/presets react.
        /// </summary>
        public static void ApplyRandomMods(ModSelectOverlay overlay)
        {
            try
            {
                var pool = overlay.AvailableMods.Value
                                  .SelectMany(pair => pair.Value)
                                  .Where(state => state.ValidForSelection.Value && state.Mod.HasImplementation && state.Mod.Type != ModType.System)
                                  .Select(state => state.Mod)
                                  .ToArray();

                if (pool.Length == 0)
                {
                    TimingLog.Info("ClientMods.ApplyRandomMods: no valid mods available");
                    return;
                }

                int count = Random.Shared.Next(1, Math.Min(3, pool.Length) + 1);
                var picked = pool.OrderBy(_ => Random.Shared.Next()).Take(count).ToArray();

                overlay.SelectedMods.Value = picked;
                TimingLog.Info($"ClientMods.ApplyRandomMods: selected {count} mod(s): {string.Join(", ", picked.Select(m => m.Acronym))}");
            }
            catch (Exception ex)
            {
                TimingLog.Error($"ClientMods.ApplyRandomMods: {ex}");
            }
        }

        /// <summary>Called from the <c>ModSelectOverlay.LoadComplete</c> postfix.</summary>
        public static void Register(ModSelectOverlay overlay)
        {
            lock (lockObject)
            {
                overlays.RemoveWhere(o => isDisposed(o));
                overlays.Add(overlay);
                TimingLog.Info($"ClientMods: registered overlay {overlay.GetType().Name} ({overlays.Count} live)");
            }
        }

        /// <summary>
        /// Re-applies System-column visibility and mod filtering to every live overlay. Called when
        /// the <see cref="SpecialsSetting.ShowSystemMods"/> toggle changes.
        /// </summary>
        public static void RefreshOverlays()
        {
            ModSelectOverlay[] live;
            lock (lockObject)
            {
                overlays.RemoveWhere(o => isDisposed(o));
                live = overlays.ToArray();
            }

            TimingLog.Info($"ClientMods.RefreshOverlays: {live.Length} live overlay(s), ShowSystemMods={ShowSystemMods}, AllowIncompatibleMods={AllowIncompatibleMods}");

            foreach (var overlay in live)
            {
                try
                {
                    applySystemColumn(overlay, ShowSystemMods);
                    invokeCreateLocalMods(overlay);
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"ClientMods.RefreshOverlays: {ex}");
                }
            }
        }

        /// <summary>
        /// Runs <c>createLocalMods()</c>, re-populating every column (including a just-added System
        /// column) and re-running <c>filterMods()</c>.
        /// </summary>
        private static void invokeCreateLocalMods(ModSelectOverlay overlay)
        {
            var createLocalMods = createLocalModsMethod.Value;
            createLocalMods?.Invoke(overlay, null);
        }

        private static void applySystemColumn(ModSelectOverlay overlay, bool present)
        {
            var columnFlowFieldInfo = columnFlowField.Value;
            var createModColumnContent = createModColumnContentMethod.Value;

            if (columnFlowFieldInfo == null || createModColumnContent == null)
            {
                TimingLog.Error("ClientMods.applySystemColumn: columnFlow/createModColumnContent not found");
                return;
            }

            var columnFlow = columnFlowFieldInfo.GetValue(overlay);
            if (columnFlow == null)
                return;

            var existing = findSystemColumn(columnFlow);

            if (present && existing == null)
            {
                if (createModColumnContent.Invoke(overlay, new object[] { ModType.System }) is not { } container)
                    return;

                var add = columnFlow.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new[] { container.GetType() }, null);
                add?.Invoke(columnFlow, new[] { container });
                TimingLog.Info("ClientMods: System column added");
            }
            else if (!present && existing != null)
            {
                var remove = columnFlow.GetType().GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public, null, new[] { existing.GetType(), typeof(bool) }, null);
                remove?.Invoke(columnFlow, new object[] { existing, true });
                TimingLog.Info("ClientMods: System column removed");
            }
        }

        private static Drawable? findSystemColumn(object columnFlow)
        {
            var children = columnFlow.GetType().GetProperty("Children", BindingFlags.Instance | BindingFlags.Public)?.GetValue(columnFlow) as IEnumerable;
            if (children == null)
                return null;

            foreach (var child in children)
            {
                object? column = child.GetType().GetProperty("Column", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(child);
                object? modType = null;
                if (column != null)
                    modType = column.GetType().GetField("ModType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)?.GetValue(column);

                if (modType is ModType mt && mt == ModType.System)
                    return child as Drawable;
            }

            return null;
        }

        // Drawable.IsDisposed is protected; read it through reflection.
        private static bool isDisposed(ModSelectOverlay overlay)
        {
            try
            {
                var type = overlay.GetType();
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                var prop = type.GetProperty("IsDisposed", flags);
                return (bool?)prop?.GetValue(overlay) ?? false;
            }
            catch
            {
                return false;
            }
        }
    }
}
