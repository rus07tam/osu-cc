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
project file (`<PluginId>` — here `my-plugin`) on the next launch, and the plugin shows up in
the Plugins overlay.

## Plugin metadata

Metadata lives in the project file, not in source:

- `<PluginId>` / `<PluginName>` / `<PluginAuthor>` / `<PluginDescription>` / `<PluginVersion>`
- `<PluginPriority>` — load/display order (lower first)
- `<PluginIcon>Assets/icon.webp</PluginIcon>` — an image file icon (any name/path/format);
  `<PluginIconGlyph>FillDrip</PluginIconGlyph>` — a FontAwesome glyph icon;
  `<PluginIconResource>` — an embedded resource
- `<PluginDependency Include="other-plugin-id" />` items — plugin dependencies

## Plugin lifecycle

Bump `<PluginVersion>` in the project file to trigger `OnUpdate` on upgrade; raise
`SchemaVersion` and add `IPluginMigration` steps to migrate persisted settings. See the
[plugin docs](https://github.com/osucc/osucc) for the full host API (`IOsuCcPluginHost`).
