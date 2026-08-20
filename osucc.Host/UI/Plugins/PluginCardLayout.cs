using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Users;
using osucc.Core;
using osucc.Localisation;
using osucc.Plugin;
using osuTK;
using osuTK.Graphics;
using System.Collections.Concurrent;
using System.Reflection;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// Shared layout helpers for rendering plugin metadata (icon, dependency and "used by"
    /// values). Used by <see cref="PluginDetailsOverlay"/>.
    /// </summary>
    internal static class PluginCardLayout
    {
        /// <summary>
        /// Builds the plugin icon. Precedence: plugin-provided FontAwesome icon, FontAwesome icon
        /// declared in the plugin attribute, image file icon, embedded IconResource, generic
        /// puzzle-piece fallback.
        /// </summary>
        public static Drawable CreateIcon(PluginEntry entry, float size, out SpriteIcon? fallbackIcon)
        {
            fallbackIcon = null;

            if (entry.Plugin?.Icon is { } usage)
                return createIcon(usage, size);

            if (ResolveFontAwesomeIcon(entry.Icon) is { } declaredIcon)
                return createIcon(declaredIcon, size);

            if (!string.IsNullOrEmpty(entry.IconPath))
            {
                var texture = entry.Host?.LoadTextureFromFile(entry.IconPath);

                if (texture != null)
                    return createTextureIcon(texture, size);
            }

            if (!string.IsNullOrEmpty(entry.IconResource))
            {
                var texture = entry.Host?.LoadTexture(entry.IconResource);

                if (texture != null)
                    return createTextureIcon(texture, size);
            }

            return fallbackIcon = createIcon(FontAwesome.Solid.PuzzlePiece, size);
        }

        private static SpriteIcon createIcon(IconUsage usage, float size) => new()
        {
            Size = new Vector2(size),
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Icon = usage,
            Colour = Color4.White,
        };

        private static readonly ConcurrentDictionary<string, IconUsage?> fontAwesomeIconLookup = new();

        /// <summary>
        /// Resolves a FontAwesome glyph by name (e.g. <c>"FillDrip"</c>, <c>"solid/fill-drip"</c>,
        /// <c>"fa-solid-fill-drip"</c>) across the solid, regular and brands families. Cached via
        /// reflection on the static properties of <see cref="FontAwesome"/>.
        /// </summary>
        public static IconUsage? ResolveFontAwesomeIcon(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            string key = name.Trim().Replace(" ", "-", StringComparison.Ordinal);
            if (fontAwesomeIconLookup.TryGetValue(key, out var cached))
                return cached;

            var resolved = resolveFontAwesomeIcon(key);
            fontAwesomeIconLookup.TryAdd(key, resolved);
            return resolved;
        }

        private static IconUsage? resolveFontAwesomeIcon(string key)
        {
            foreach (var family in new[] { typeof(FontAwesome.Solid), typeof(FontAwesome.Regular), typeof(FontAwesome.Brands) })
            {
                foreach (var property in family.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    if (property.PropertyType != typeof(IconUsage) || property.GetValue(null) is not IconUsage usage)
                        continue;

                    string familyName = family.Name;
                    string propertyName = property.Name;

                    if (string.Equals(propertyName, key, StringComparison.OrdinalIgnoreCase)
                        || string.Equals($"{familyName}/{propertyName}", key, StringComparison.OrdinalIgnoreCase)
                        || string.Equals($"{familyName}-{propertyName}", key, StringComparison.OrdinalIgnoreCase)
                        || string.Equals($"fa-{familyName}-{propertyName}", key, StringComparison.OrdinalIgnoreCase))
                        return usage;
                }
            }

            return null;
        }

        private static Sprite createTextureIcon(Texture texture, float size)
        {
            // Size must be set before Texture: Sprite's Texture setter auto-sizes when Size is
            // zero, which combined with RelativeSizeAxes would blow the icon up.
            return new Sprite
            {
                Size = new Vector2(size),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                FillMode = FillMode.Fit,
                Texture = texture,
            };
        }

        /// <summary>Localised plugin name from the <c>&lt;id&gt;:name</c> key, falling back to the attribute value.</summary>
        public static LocalisableString LocalisedName(PluginEntry entry) => LocalisedName(entry.Id, entry.Name);

        /// <summary>Localised plugin name from the <c>&lt;id&gt;:name</c> key, falling back to the given fallback.</summary>
        public static LocalisableString LocalisedName(string id, string name) => OsuCcLocalisation.Get($"{id}:name", name);

        /// <summary>Localised description text from the <c>&lt;id&gt;:description</c> key, falling back to the attribute value.</summary>
        public static LocalisableString Description(PluginEntry entry) => OsuCcLocalisation.Get($"{entry.Id}:description", entry.Description ?? string.Empty);

        /// <summary>Human-readable status text for the given plugin entry.</summary>
        public static LocalisableString StatusText(PluginEntry entry)
        {
            return entry.Status switch
            {
                PluginStatus.Active => PluginsOverlayStrings.StatusActive,
                PluginStatus.PendingEnable => PluginsOverlayStrings.StatusPendingEnable,
                PluginStatus.PendingDisable => PluginsOverlayStrings.StatusPendingDisable,
                PluginStatus.PendingDelete => PluginsOverlayStrings.StatusPendingDelete,
                PluginStatus.Disabled => PluginsOverlayStrings.StatusDisabled,
                PluginStatus.Error => entry.LoadError == null ? PluginsOverlayStrings.StatusFailed : PluginsOverlayStrings.StatusFailedWithError(entry.LoadError.Message),
                _ => string.Empty,
            };
        }

        /// <summary>Colour used to render the given plugin status.</summary>
        public static Color4 StatusColour(PluginStatus status)
        {
            return status switch
            {
                PluginStatus.Active => OsuCcColours.Success,
                PluginStatus.PendingEnable or PluginStatus.PendingDisable => OsuCcColours.Info,
                PluginStatus.PendingDelete or PluginStatus.Disabled => OsuCcColours.Disabled,
                PluginStatus.Error => OsuCcColours.Error,
                _ => OsuCcColours.Info,
            };
        }

        /// <summary>A plain white icon button with a tooltip.</summary>
        public static IconButton CreateActionButton(IconUsage icon, LocalisableString tooltip, Action action) => new()
        {
            Icon = icon,
            TooltipText = tooltip,
            Action = action,
            IconColour = Color4.White,
        };

        /// <summary>A dimmed ", " separator for joining text segments.</summary>
        private static OsuSpriteText createSeparator(float fontSize) => new()
        {
            Text = ", ",
            Font = OsuFont.Default.With(size: fontSize),
            Colour = Color4.White.Opacity(0.55f),
        };

        /// <summary>
        /// Renders the "Depends on" value: one segment per dependency, joined by commas.
        /// Dependencies that are missing (not discovered) or disabled are shown with a suffix and
        /// highlighted.
        /// </summary>
        public static FillFlowContainer CreateDependenciesValue(
            IReadOnlyList<string> dependencyIds,
            IReadOnlyDictionary<string, LocalisableString> nameById,
            IReadOnlySet<string> unavailableIds,
            float fontSize = 13)
        {
            var flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(0, 0),
            };

            for (int i = 0; i < dependencyIds.Count; i++)
            {
                string depId = dependencyIds[i];
                bool missing = !nameById.ContainsKey(depId);
                bool unavailable = missing || unavailableIds.Contains(depId);

                var segment = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(0, 0),
                    Children = new Drawable[]
                    {
                        unavailable
                            ? new OsuSpriteText
                            {
                                Text = missing ? depId : nameById[depId],
                                Font = OsuFont.Default.With(size: fontSize),
                                Colour = OsuCcColours.Warning,
                            }
                            : new PluginNameLink(depId, nameById[depId], fontSize: fontSize),
                    },
                };

                if (unavailable)
                {
                    segment.Add(new OsuSpriteText
                    {
                        Text = LocalisableString.Format("({0})", missing ? PluginsOverlayStrings.DependencyMissing : PluginsOverlayStrings.DependencyDisabled),
                        Font = OsuFont.Default.With(size: fontSize),
                        Colour = OsuCcColours.Warning,
                    });
                }

                if (i > 0)
                    flow.Add(createSeparator(fontSize));

                flow.Add(segment);
            }

            return flow;
        }

        /// <summary>Renders the "Used by" value: one clickable link per dependent plugin.</summary>
        public static FillFlowContainer CreateUsedByValue(
            IReadOnlyList<string> dependentIds,
            IReadOnlyDictionary<string, LocalisableString> nameById,
            float fontSize = 13)
        {
            var flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(0, 0),
            };

            for (int i = 0; i < dependentIds.Count; i++)
            {
                string id = dependentIds[i];

                if (i > 0)
                    flow.Add(createSeparator(fontSize));

                if (nameById.TryGetValue(id, out var name))
                    flow.Add(new PluginNameLink(id, name, fontSize: fontSize));
                else
                {
                    flow.Add(new OsuSpriteText
                    {
                        Text = id,
                        Font = OsuFont.Default.With(size: fontSize),
                        Colour = Color4.White.Opacity(0.55f),
                    });
                }
            }

            return flow;
        }

        /// <summary>
        /// Renders the "Author" value: one segment per author, joined by commas. Profile-linked
        /// authors (<see cref="PluginAuthor.OsuesId"/> set) render as clickable usernames that open
        /// the in-game osu! profile (routed through <see cref="LinkFlowContainer.AddUserLink"/> so
        /// they also inherit the Username Visuals gradient styling); plain nicknames render as
        /// text. Falls back to the "unknown author" placeholder when there are no authors.
        /// </summary>
        public static FillFlowContainer CreateAuthorValue(PluginEntry entry, float fontSize = 13)
        {
            var flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(0, 0),
            };

            for (int i = 0; i < entry.Authors.Count; i++)
            {
                if (i > 0)
                    flow.Add(createSeparator(fontSize));

                PluginAuthor author = entry.Authors[i];

                if (author.OsuesId is int profileId)
                {
                    var linkFlow = new LinkFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                    };

                    linkFlow.AddUserLink(new PluginUser { OnlineID = profileId, Username = author.Name },
                        s => s.Font = OsuFont.GetFont(size: fontSize, weight: FontWeight.Medium));

                    flow.Add(linkFlow);
                }
                else
                {
                    flow.Add(new OsuSpriteText
                    {
                        Text = author.Name,
                        Font = OsuFont.Default.With(size: fontSize),
                        Colour = Color4.White,
                    });
                }
            }

            return flow;
        }

        /// <summary>Joined author names as plain text (no links), for string-only surfaces.</summary>
        public static string FormatAuthorNames(PluginEntry entry)
            => string.Join(", ", entry.Authors.Select(a => a.Name));

        /// <summary>Renders the plugin's tags as non-clickable pills, capped at <paramref name="maxShown"/> with a "+N" counter for the rest.</summary>
        public static FillFlowContainer CreateTagsValue(
            PluginEntry entry,
            float fontSize = 12,
            int maxShown = int.MaxValue)
        {
            var flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(6, 0),
            };

            int shown = 0;

            foreach (string tag in entry.Tags)
            {
                if (shown >= maxShown)
                    break;

                flow.Add(new TagChip(tag, fontSize));

                shown++;
            }

            int hidden = entry.Tags.Count - shown;

            if (hidden > 0)
                flow.Add(new TagChip($"+{hidden}", fontSize, more: true));

            return flow;
        }

        /// <summary>
        /// Minimal <see cref="IUser"/> carrying just what the profile link needs: the osu! id and
        /// the username to display. The profile overlay fetches the full profile from the API by id.
        /// </summary>
        private sealed class PluginUser : IUser
        {
            public int OnlineID { get; set; }

            public string Username { get; set; } = string.Empty;

            public CountryCode CountryCode { get; set; }

            public bool IsBot { get; set; }

            public bool Equals(IUser? other) => other is PluginUser p && p.OnlineID == OnlineID;
        }
    }
}
