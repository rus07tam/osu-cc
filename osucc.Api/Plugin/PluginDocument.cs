namespace osucc.Plugin
{
    /// <summary>
    /// Represents a declared Markdown documentation file (e.g. README or CHANGELOG) associated with a plugin.
    /// </summary>
    public class PluginDocument
    {
        /// <summary>Display title shown on the document's tab (e.g. "README", "Changelog").</summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>Relative file path from the plugin folder root (e.g. "res/README.md").</summary>
        public string Path { get; init; } = string.Empty;

        /// <summary>Optional FontAwesome glyph name for the tab icon (e.g. "Book", "History").</summary>
        public string? IconGlyph { get; init; }
    }
}
