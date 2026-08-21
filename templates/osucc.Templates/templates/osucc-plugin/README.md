# MyPlugin

An [osu-cc](https://github.com/osucc/osucc) plugin generated with `dotnet new osucc-plugin`.

## Build

```shell
dotnet build -c Release
```

This produces `bin/Release/net8.0/my-plugin.zip` containing only the plugin's dll (and your
`PluginIcon` image if you declare one in the csproj). `osu.*` / `osucc` / `0Harmony` resolve
from the host process, so they are never packaged.

## Deploy

Drop `my-plugin.zip` into the game's plugin folder:

- Windows: `%APPDATA%\osu\osu-cc\plugins\`
- Linux: `~/.local/share/osu/osu-cc/plugins/`

The manager extracts the archive into a folder named after the plugin `Id` declared in the
project file (`<PackageId>` — here `my-plugin`) on the next launch, and the plugin shows up in
the Plugins overlay.

## Plugin metadata

Metadata lives in the project file, not in source:

- `<PackageId>` / `<Title>` / `<Description>` / `<Version>` / `<RepositoryUrl>`
- `<Author>` items (or `<Authors>` property) — each a plain nickname or, with an
  `OsuProfileId` metadata, an osu! profile-linked username (clickable in the UI):
  `<Author Include="peppy" OsuProfileId="1013" />`
- `<Priority>` — load/display order (lower first)
- `<PackageIcon>Assets/icon.webp</PackageIcon>` — an image file icon (any name/path/format);
  `<IconGlyph>FillDrip</IconGlyph>` — a FontAwesome glyph icon name
- `<PluginDependency Include="other-plugin-id" />` items — plugin dependencies
- `<Tag Include="category" />` items or `<PackageTags>` property — display tags: shown as clickable chips in the
  Plugins overlay; clicking a chip (or typing a name/author/id/tag in the overlay's search box)
  filters the list to the matching plugins.

## Plugin lifecycle

Bump `<Version>` in the project file to trigger `OnUpdate` on upgrade; raise
`SchemaVersion` and add `IPluginMigration` steps to migrate persisted settings. See the
[plugin docs](https://github.com/osucc/osucc) for the full host API (`IOsuCcPluginHost`).
