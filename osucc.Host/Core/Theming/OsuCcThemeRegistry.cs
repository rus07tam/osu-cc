using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace osucc.Core
{
    /// <summary>
    /// Registry of available themes.
    /// Built-in themes are loaded from embedded JSON files on first access.
    /// </summary>
    public static class OsuCcThemeRegistry
    {
        public const string DefaultId = "Default";

        private static readonly object lockObject = new();
        private static osu.Framework.Bindables.BindableList<OsuCcThemeDefinition>? themes;
        private static FileSystemWatcher? themeWatcher;
        private static bool isInitialized;

        /// <summary>All registered themes.</summary>
        public static osu.Framework.Bindables.IBindableList<OsuCcThemeDefinition> RegisteredThemes
        {
            get
            {
                lock (lockObject)
                {
                    ensureBuiltIns();
                    return themes!;
                }
            }
        }

        /// <summary>Registers a new theme definition.</summary>
        public static void Register(OsuCcThemeDefinition definition)
        {
            lock (lockObject)
            {
                ensureBuiltIns();
                if (themes!.Any(t => t.Id == definition.Id))
                    return;

                themes!.Add(definition);
            }
        }

        public static OsuCcThemeDefinition Get(string id)
            => TryGet(id, out var definition) ? definition : throw new ArgumentException($"Unknown theme id: {id}", nameof(id));

        public static bool TryGet(string id, out OsuCcThemeDefinition definition)
        {
            lock (lockObject)
            {
                ensureBuiltIns();

                definition = themes!.FirstOrDefault(t => t.Id == id)!;
                return definition != null;
            }
        }

        private static void ensureBuiltIns()
        {
            if (isInitialized)
                return;

            var storage = Client.ClientHostTasks.StorageManager?.GetStorage("core", typeof(OsuCcThemeRegistry).Assembly);
            if (storage == null)
                return; // Not ready yet

            isInitialized = true;
            themes = new osu.Framework.Bindables.BindableList<OsuCcThemeDefinition>();

            var files = storage.GetFiles("Themes", "*.json");
            foreach (var file in files)
            {
                var theme = storage.ReadJson<OsuCcThemeDefinition>($"Themes/{file}");
                if (theme != null)
                {
                    themes.Add(theme);
                }
            }

            ensureDefaultFallback();

            var fullPath = storage.GetFullPath("Themes");
            if (!string.IsNullOrEmpty(fullPath))
            {
                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);

                themeWatcher = new FileSystemWatcher(fullPath, "*.json")
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };
                themeWatcher.Changed += onThemeFileChanged;
                themeWatcher.Created += onThemeFileChanged;
                themeWatcher.Deleted += onThemeFileDeleted;
                themeWatcher.Renamed += onThemeFileDeleted; // A rename triggers delete of old, create of new (handled separately or by renaming event, we just trigger delete and the new one triggers created)
                themeWatcher.EnableRaisingEvents = true;
            }
        }

        private static void ensureDefaultFallback()
        {
            if (!themes!.Any(t => t.Id == DefaultId))
            {
                themes!.Add(new OsuCcThemeDefinition
                {
                    Id = DefaultId,
                    Name = "Default",
                    IsVanilla = true,
                    AccentTransform = new AccentTransformDefinition { Kind = AccentTransformKind.Identity },
                    Chrome = new ChromeRampDefinition
                    {
                        TextLightnessThreshold = 0.9f,
                        Text = new HslSpec(0, 0, 0.95f),
                        AccentSaturationThreshold = 0.4f,
                        Accent = new AccentBandDefinition
                        {
                            HueDegrees = 0,
                            Saturation = SaturationMode.Keep,
                            LightnessMin = 0.3f,
                            LightnessMax = 0.85f
                        },
                        Surface = new SurfaceBandDefinition
                        {
                            HueDegrees = 0,
                            Saturation = SaturationMode.Keep,
                            Lightness = new LightnessCurve
                            {
                                Interpolation = LightnessCurveInterpolation.Linear,
                                ControlPoints = new[] { new osuTK.Vector2(0, 0), new osuTK.Vector2(1, 1) }
                            }
                        }
                    }
                });
            }
        }

        private static readonly JsonSerializerOptions jsonOptions = new() { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        private static void onThemeFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // FileWatcher may fire while the file is still being written to.
                System.Threading.Thread.Sleep(100);

                var json = File.ReadAllText(e.FullPath);
                var theme = JsonSerializer.Deserialize<OsuCcThemeDefinition>(json, jsonOptions);
                if (theme == null) return;

                var scheduler = Reflection.GetScheduler(Client.ClientApi.Game);
                scheduler?.Add(() =>
                {
                    var existing = themes!.FirstOrDefault(t => t.Id == theme.Id);
                    if (existing != null)
                    {
                        var index = themes!.IndexOf(existing);
                        themes[index] = theme;
                    }
                    else
                    {
                        themes!.Add(theme);
                    }

                    if (OsuCcThemeManager.ActiveId == theme.Id)
                        OsuCcThemeManager.IsActiveThemeDirty.Value = true;
                });
            }
            catch
            {
                // Ignore parse errors or lock errors for now.
            }
        }

        private static void onThemeFileDeleted(object sender, FileSystemEventArgs e)
        {
            var scheduler = Reflection.GetScheduler(Client.ClientApi.Game);
            scheduler?.Add(() =>
            {
                // We don't have the ID from the file content anymore, so we guess from filename.
                var possibleId = Path.GetFileNameWithoutExtension(e.Name);
                var existing = themes!.FirstOrDefault(t => t.Id == possibleId || string.Equals(t.Name, possibleId, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    themes!.Remove(existing);
                    if (OsuCcThemeManager.ActiveId == existing.Id)
                        OsuCcThemeManager.IsActiveThemeDirty.Value = true;
                }
            });
        }
    }
}
