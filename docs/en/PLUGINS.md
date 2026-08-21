# osu!cc: plugin development

Notes for developers writing plugins for osu!cc.

**Languages:** English · [Русский](../ru/PLUGINS.md)

## Plugins

A plugin is a classlib that extends `OsuCcPlugin`; see `plugins/ExamplePlugin` for a working example. Plugin metadata is declared in the **project file** using standard .NET/MSBuild properties: `<PackageId>`, `<Title>`, `<Description>`, `<Version>`, `<RepositoryUrl>`, `<PackageTags>`, `<PackageIcon>` (image file), `<IconGlyph>` (FontAwesome icon name), and `<Priority>`. The build turns these properties into an assembly-level `[OsuCcPlugin]` manifest (generated into `obj/PluginMetadata.g.cs`), which the manager reads at discovery.

Authors are declared using `<Author>` items (or `<Authors>` string). Each item is a plain nickname or, with an `OsuProfileId`, an osu! profile-linked username that the UI renders as a clickable username opening the profile **in-game**:

```xml
<ItemGroup>
  <Author Include="osu-cc" />
  <Author Include="peppy" OsuProfileId="1013" />
</ItemGroup>
```

Display tags can be declared via the `<PackageTags>` property (e.g. `<PackageTags>profile;library</PackageTags>`) or as `<Tag>` items:

```xml
<ItemGroup>
  <Tag Include="profile" />
  <Tag Include="library" />
</ItemGroup>
```

Tags are free-form lowercase strings. The recommended vocabulary below keeps tags consistent across plugins and makes the search predictable:

- **Classifiers** (apply when true):
  - `library` - the plugin exposes a public API to other plugins via `host.ExportApi(...)` (consumed with `host.GetApi<T>(...)`)
  - `integration` - the plugin integrates with a third-party service/external API
- **Scope** (where in the game the plugin surfaces): `profile`, `menu`, `playfield`, `settings`, ... - other scopes (e.g. `song-select`, `leaderboard`, `chat`) are welcome
- **Minor / descriptive**: `tools`, `fun`, `dev`, `ui`, `visual`, `audio`

A plugin usually carries at least one scope tag, plus a classifier and/or a descriptive tag when relevant.

### Resources & Documentation

Plugins can bundle arbitrary resource files in a `./res/` directory (or by declaring `<PluginResource Include="..." />` items in the `.csproj`). All files in `./res/` are automatically packaged into the plugin archive and staged into the plugin directory at runtime.

To provide in-game Markdown documentation (such as a `README.md` or `CHANGELOG.md`), declare `<PluginDocument>` items referencing markdown files in `./res/`:

```xml
<ItemGroup>
  <PluginDocument Include="res/README.md" Title="README" IconGlyph="Book" />
  <PluginDocument Include="res/CHANGELOG.md" Title="Changelog" IconGlyph="History" />
</ItemGroup>
```

Declared documents appear as interactive tabs on the right side of the plugin's details view alongside the settings tab, rendered using osu!'s Markdown engine.

Through `IOsuCcPluginHost` a plugin can:

- add toolbar buttons (`AddToolbarButton(factory, placement, layoutPosition)`)
- add settings subsections
- register full-screen overlays
- send notifications (`host.Notify`)
- play celebrations
- declare strongly-typed Harmony patches via classes inheriting from `PluginPatch<TPlugin>` in the `Patches` collection, automatically wrapped with runtime `Condition` gating (e.g. `Plugin.Enabled`), automatic `try/catch` protection, and error logging
- persist config (`GetSettings` / `GetStorage`)

Plugins can also expose a **public API** to each other. The plugin system only provides the transport - contract types live where the exporting plugin puts them. To export, call `host.ExportApi(api)` in `Load`; consumers fetch it by the exporting plugin's id with `host.GetApi<T>(pluginId)` (returns `null` when the plugin is missing or exported nothing assignable to `T`). Because the host's `AssemblyLoadContext.Default.Resolving` handler binds every `osucc` reference to the deployed hook, contract types declared in `osucc.Host` unify across plugins; a contract declared inside the exporting plugin's own assembly requires the consumer to reference that assembly (a `ProjectReference` in the monorepo). To cast across assemblies the contract must be shared, so exported interface types should live in `osucc.Host` or a common package. Example: the built-in `UsernameVisuals` plugin exports `IUsernameVisualsApi` (own/others palettes, display overrides and per-user overrides as prioritized conditionals) under the `username-visuals` id. Its own-username display settings (hide, replace) always win over plugin-registered rules; the others-palette fallback uses the lowest priority, so plugins can still style other users. The built-in `ExamplePlugin` consumes it in `AttachToGame` via `host.GetApi<IUsernameVisualsApi>("username-visuals")` (see `ExampleUsernameVisualsApiConsumer`); because the contract lives in the `UsernameVisuals` assembly it adds a `ProjectReference` to it. A missing or disabled exporting plugin makes `GetApi` return `null` (the consumer must handle that); the compile-time reference only resolves when the consumer's code actually touches the contract type.

Export in `Load` so other plugins see it in their own `Load` (order via `Priority`) or in `AttachToGame`; `GetApi` is always safe from `AttachToGame`.

Dependencies are declared in the project file with `<PluginDependency Include="plugin-id" />` items. The dependency resolver guarantees a dependency loads (and attaches) before the dependent plugin; when no dependency forces an order, the `Priority` order is preserved exactly, so the priority system keeps working (the overlay's display order stays purely priority-based, reordering arrows are unaffected). Dependencies are **soft**: a missing or disabled dependency only logs a warning, and the plugin still loads - `GetApi` returns `null`, which the consumer must handle as before. `ExamplePlugin` declares a dependency on `username-visuals` as the reference (see its csproj).

Plugins are shipped as zip archives. The launcher drops them into the osu-cc data folder (`plugins/`), where the manager extracts each one into a folder named after the plugin `Id`. Disabled plugins stay listed in the overlay but aren't loaded.

The **Update Manager** plugin (`plugins/OsuCcUpdater`) is special only in what it does, not in how it is built: it is a normal plugin with a settings subsection and a toolbar button. It keeps the hook and the shipped plugins current by fetching the runtime bundle - from GitHub releases or by building it locally from the official repo - staging it into `<data>/osu-cc/staging/` next to an `update.json` marker (`UpdateMarker` in `osucc.Shared`), and letting the launcher apply it on the next launch (the running game locks the `hook/` files on Windows, so updates can only be swapped on the next start).

### Lifecycle hooks & data migrations

On top of `Load` / `AttachToGame` a plugin can implement optional interfaces to react to install/uninstall/update events and version its persisted data:

- `IPluginLifecycle`: `OnInstall(host)` (first launch after install, after `AttachToGame`), `OnUninstall(host)` (fired in-place when deletion is confirmed, before the payload folder is removed on the next launch), `OnUpdate(host, previousVersion)` (the loaded `Version` differs from the last recorded one). All hooks run on the update thread, after data migrations and after `AttachToGame`.
- `IPluginMigrations`: `SchemaVersion` plus ordered `IPluginMigration` steps (`ToVersion`, `Apply(host)`). When a plugin's persisted schema is below `SchemaVersion`, the manager applies the steps in `ToVersion` order, saving each step's result before the next runs. Fresh installs skip migrations. Use `PluginSettings.ReadPersisted` / `Remove` / `ContainsKey` inside a step to read legacy values and rename keys.
- `OsuCcPluginBase`: convenience base class: caches the host into `Host`, provides no-op lifecycle hooks and empty migrations, so a plugin only overrides what it needs (`protected abstract void OnLoad()` instead of `Load`).

The last-seen version (`version.<id>`) and schema (`schema.<id>`) are persisted in `plugin-states.ini` next to the plugins folder; both are cleared when a plugin is removed, so a re-install fires `OnInstall` again. Version diffs compare the `<PluginVersion>` declared in the project file, so bump it on every release that changes behaviour or data, otherwise `OnUpdate` will not fire.

### Full-screen overlays

A plugin can render its own full-screen UI on top of the game with one of the two style bases shipped in `osucc.Api` (`osucc.UI.Overlays`):

- `OsuCcShearedOverlay` — the sheared (slanted) look used across osu!cc (the debug overlay itself is one).
- `OsuCcWaveOverlay` — wave style like the game's online overlays (beatmap listing, changelog, wiki): four coloured wave bands sweep over the dimmed background while the page quickly fades in on top. The header (`OsuCcOverlayHeader`) is a stock-style title bar with icon/title/description plus a content row for tabs/filters, and it scrolls together with the content.

Both derive from the shared `OsuCcOverlayBase` and bring the same guarantees: mutual exclusion ("last opened wins" - opening one hides the other osu!cc overlays), depth so the overlay renders above the game's own overlays, shared dimming of the screen content, close/back handling that restores the previously visible overlay, and a `MainAreaContent` `PopoverContainer` for popovers.

To use one, subclass it, pick an `OverlayColourScheme`, set the header title/description/icon, optionally add tabs to `Header.ContentRow` and content into `MainAreaContent`, then register it with the game's overlay manager through `host.RegisterBlockingOverlay(overlay)`:

```csharp
public class MyOverlay : OsuCcWaveOverlay
{
    public MyOverlay() : base(OverlayColourScheme.Blue) { }

    [BackgroundDependencyLoader]
    private void load()
    {
        Header.TitleText = "My overlay";
        Header.DescriptionText = "Wave style";
        Header.HeaderIcon = OsuIcon.Online;

        // Optional: row of tabs under the title bar (stock header look).
        Header.ContentRow.Add(new OsuTabControl<MySection> { ... });

        MainAreaContent.Add(/* your content (no extra scroll: the overlay already provides one) */);
    }
}
```

Register it in `AttachToGame`:

```csharp
public override void AttachToGame()
{
    overlay = new MyOverlay();
    Host.RegisterBlockingOverlay(overlay);
}
```

The debug plugin's **Overlays** panel demonstrates this: "Show wave overlay" opens a `DebugWaveOverlay` on top of the debug overlay and back.

### Best Practices

- **Dynamic Settings**: Settings must always apply dynamically at runtime without requiring a game restart (except for heavy architectural changes like UI theming/skinning).
- **Reversibility**: Disabling a feature or plugin must restore the game's stock behavior immediately. When a toggle is switched off, all patches/hooks should either disable themselves or yield to the default behavior.

### Building & deploying a plugin

A plugin is a classlib whose `osucc.Build` targets pack the plugin's own dll (+ optional icon) into a single `<PluginId>.zip` archive. There are two ways to get one.

**Inside the monorepo** - the development loop for the built-in plugins:

```shell
dotnet build osucc.build.proj -c Debug   # packs the local feed, then builds hook + all plugins
```

Every plugin's `PackagePluginArchive` target writes its archive into its own output folder:

```
plugins/MyPlugin/bin/Debug/net8.0/my-plugin.zip
```

To rebuild just one plugin after the feed exists, the per-project build is much faster:

```shell
dotnet build plugins/MyPlugin/MyPlugin.csproj -c Debug
```

**Standalone** (no osu-cc checkout) - via the `dotnet new` template. First build once so the packed packages exist in `artifacts/nuget`, or pull `osucc.Host` / `osucc.Build` from nuget.org:

```shell
dotnet new install artifacts/nuget/osucc.Templates.1.0.0.nupkg
dotnet new osucc-plugin -n MyPlugin -o MyPlugin
dotnet build MyPlugin -c Debug
```

The generated project references `osucc.Host` / `osucc.Build` from NuGet and produces the same `my-plugin.zip` in `bin/Debug/net8.0/`.

**Deploying** in both cases is dropping the zip into the game's `osu-cc/plugins` folder; the manager extracts it into a folder named after the plugin `Id` and lists it in the overlay:

```
# Linux:  ~/.local/share/osu/osu-cc/plugins
# Windows: %APPDATA%\osu\osu-cc\plugins
cp plugins/MyPlugin/bin/Debug/net8.0/MyPlugin.zip ~/.local/share/osu/osu-cc/plugins/
```

Restart the game and toggle the plugin in the plugin overlay. Bump `PluginVersion` in the project file on every release that changes behaviour or data so `OnUpdate` fires for users who already have the plugin installed.
