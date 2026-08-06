namespace osucc.Plugin
{
    /// <summary>
    /// Carries a plugin's metadata. Declared in the plugin's project file (see
    /// <c>PluginId</c>/<c>PluginName</c>/… properties and <c>PluginDependency</c> items in the
    /// csproj); the build emits an assembly-level instance from those properties, so plugin
    /// sources never write the attribute directly. The class-level target is retained only so
    /// archives built before the csproj declaration are still discovered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public class OsuCcPluginAttribute : Attribute
    {
        /// <summary>
        /// Version of the plugin API this client supports. Bump on any breaking change to
        /// <see cref="IOsuCcPlugin"/> / <see cref="IOsuCcPluginHost"/>; plugins declaring a
        /// different <see cref="ApiVersion"/> are skipped with a warning.
        /// </summary>
        public const int CurrentApiVersion = 2;

        /// <summary>Stable, unique identifier (also used as the plugin's storage folder name).</summary>
        public string Id { get; }

        /// <summary>Display name shown in the plugins overlay.</summary>
        public string Name { get; }

        /// <summary>
        /// Author as a single display string. Retained for backward compatibility: archives built
        /// before the multi-author declaration only carry this. New builds use
        /// <see cref="AuthorNames"/> / <see cref="AuthorOsuIds"/> instead.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Display label per author: the nickname, or the username for a profile-linked author.
        /// Parallel to <see cref="AuthorOsuIds"/> (index-for-index).
        /// </summary>
        public string[] AuthorNames { get; set; } = Array.Empty<string>();

        /// <summary>
        /// osu! profile id per author, parallel to <see cref="AuthorNames"/>; <c>-1</c> (or a shorter
        /// array) means that author is a plain nickname without a profile link.
        /// </summary>
        public int[]? AuthorOsuIds { get; set; }

        /// <summary>
        /// Display categories/tags for the plugin (e.g. <c>"osu"</c>, <c>"theme"</c>, <c>"api"</c>),
        /// shown as clickable chips in the plugins UI. Filtering the search box by a tag, name,
        /// author or id selects the matching plugins in the list overlay.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        public string? Description { get; set; }

        public string Version { get; set; } = "1.0.0";

        /// <summary>Plugin API version the plugin was built against.</summary>
        public int ApiVersion { get; }

        /// <summary>Load order. Lower numbers load first.</summary>
        public int Priority { get; }

        /// <summary>Optional name of an embedded assembly resource used as the plugin icon.</summary>
        public string? IconResource { get; set; }

        /// <summary>
        /// Optional FontAwesome glyph name (e.g. <c>"FillDrip"</c>) used as the plugin icon.
        /// Declared so the icon is available even when the plugin is not loaded (disabled/errored),
        /// unlike the runtime <see cref="osucc.Plugin.IOsuCcIconProvider"/> which requires a live
        /// instance.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Optional image file icon, relative to the plugin payload folder (e.g. <c>"icon.png"</c>
        /// or <c>"Assets/icon.webp"</c>). Declared from the <c>PluginIcon</c> project property;
        /// preferred over <see cref="IconResource"/> at display time.
        /// </summary>
        public string? IconPath { get; set; }

        /// <summary>
        /// Stable ids of plugins this plugin depends on. Dependencies always load before the
        /// dependent plugin; the persisted/attribute priority only breaks the tie when no
        /// dependency forces an order. Missing or disabled dependencies are soft — the plugin
        /// still loads (its <c>GetApi&lt;T&gt;</c> returns <c>null</c>), only a warning is logged.
        /// </summary>
        public string[] DependsOn { get; set; } = Array.Empty<string>();

        public OsuCcPluginAttribute(string id, string name, int priority = 0, int apiVersion = CurrentApiVersion)
        {
            Id = id;
            Name = name;
            Priority = priority;
            ApiVersion = apiVersion;
        }
    }
}
