# MyPlugin

An [osu-cc](https://github.com/osucc/osucc) plugin generated with `dotnet new osucc-plugin`.

## Build

```shell
dotnet build -c Release
```

This produces `bin/Release/net8.0/MyPlugin.zip` containing only the plugin's dll (and an
`icon.png` if you add one next to the csproj). `osu.*` / `osucc` / `0Harmony` resolve from the
host process, so they are never packaged.

## Deploy

Drop `MyPlugin.zip` into the game's plugin folder:

- Windows: `%APPDATA%\osu\osu-cc\plugins\`
- Linux: `~/.local/share/osu/osu-cc/plugins/`

The manager extracts the archive into a folder named after the plugin `Id` from `[OsuCcPlugin]`
(here `my-plugin`) on the next launch, and the plugin shows up in the Plugins overlay.

## Plugin lifecycle

Bump the `Version` in the `[OsuCcPlugin]` attribute to trigger `OnUpdate` on upgrade; raise
`SchemaVersion` and add `IPluginMigration` steps to migrate persisted settings. See the
[plugin docs](https://github.com/osucc/osucc) for the full host API (`IOsuCcPluginHost`).
