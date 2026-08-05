# osu!cc: development

Notes for people hacking on this repo.

**Languages:** English · [Русский](../ru/DEVELOPMENT.md)

## Layout

```plaintext
osucc.Shared/    shared layout/version/staging logic (namespace osucc.Common), the single
                 source of truth for the launcher, the hook and the update manager plugin
osucc.Host/     the startup hook DLL (classlib, net8.0), also the osucc.Host NuGet package
  StartupHook.cs     entry point the runtime calls
  Core/              bootstrapper, reflection helpers, logging
  Client/            public API + client state
  Patches/           the Harmony patches
  UI/                overlays, settings section, mod UI
  Plugin/            plugin manager and the host API
osucc/          the launcher CLI (run / start / status only — never builds, never writes to the install)
plugins/        the built-in plugins (ExamplePlugin, FakeSupporter, FriendsLeaderboard, Oii, osuccDebug, OsuCcUpdater, SubdivideNations, UsernameVisuals)
docs/           screenshots (assets/), per-language docs (en/, ru/)
```

## How the hook works

The runtime calls `StartupHook.Initialize()` before `Main()` runs. It
subscribes to `AppDomain.AssemblyLoad`, and when `osu.Game` shows up,
`ClientBootstrapper.InstallPatches()` puts all patches in place.

Patch targets are resolved by assembly/type/method **name** at runtime, so a
patch survives osu! version bumps. The UI/API code, on the other hand, compiles
against the `ppy.osu.Game` NuGet reference. The production `osu.Game.dll` is
usually newer than that reference, so never reference osu internals directly.
Find them by name. Reflection helpers live in `osucc.Host/Core/Reflection.cs`.

One trap: `GetField`/`GetMethod` with `FlattenHierarchy` does **not** see
private instance members of base classes. Read those from the declaring type or
walk `BaseType`.

## Plugins

A plugin is a classlib that implements `IOsuCcPlugin` (or extends `OsuCcPluginBase`);
see `plugins/ExamplePlugin` for a working example. Plugin metadata is declared in the
**project file**, never in source: the build turns the `PluginId` / `PluginName` /
`PluginAuthor` / `PluginDescription` / `PluginVersion` / `PluginPriority` properties,
the `PluginIcon` (image file), `PluginIconGlyph` (FontAwesome name) and
`PluginIconResource` (embedded resource) values, and the `PluginDependency` items into an
assembly-level `[OsuCcPlugin]` manifest (generated into `obj/PluginMetadata.g.cs`), which
the manager reads at discovery. Legacy archives whose attribute lives on the plugin class
are still discovered, with a deprecation log. Through `IOsuCcPluginHost` a
plugin can:

- add toolbar buttons (`AddToolbarButton(factory, placement, layoutPosition)`)
- add settings subsections
- register full-screen overlays
- send notifications (`host.Notify`)
- play celebrations
- install its own Harmony patches via `host.AddPatch(...)` or the
  `PatchHelper.AttachPrefix/AttachPostfix/AttachConstructorPostfix/AttachMethodPostfix`
  wrappers, which return a disposable patch handle the host can revoke
- persist config (`GetSettings` / `GetStorage`)

Plugins can also expose a **public API** to each other. The plugin system only
provides the transport — contract types live where the exporting plugin puts
them. To export, call `host.ExportApi(api)` in `Load`; consumers fetch it by the
exporting plugin's id with `host.GetApi<T>(pluginId)` (returns `null` when the
plugin is missing or exported nothing assignable to `T`). Because the host's
`AssemblyLoadContext.Default.Resolving` handler binds every `osucc` reference to
the deployed hook, contract types declared in `osucc.Host` unify across plugins;
a contract declared inside the exporting plugin's own assembly requires the
consumer to reference that assembly (a `ProjectReference` in the monorepo). To
cast across assemblies the contract must be shared, so exported interface types
should live in `osucc.Host` or a common package. Example: the built-in
`UsernameVisuals` plugin exports `IUsernameVisualsApi` (own/others palettes,
display overrides and per-user overrides as prioritized conditionals) under the
`username-visuals` id. Its own-username display settings (hide, replace) always
win over plugin-registered rules; the others-palette fallback uses the lowest
priority, so plugins can still style other users. The built-in `ExamplePlugin` consumes it in
`AttachToGame` via `host.GetApi<IUsernameVisualsApi>("username-visuals")` (see
`ExampleUsernameVisualsApiConsumer`); because the contract lives in the
`UsernameVisuals` assembly it adds a `ProjectReference` to it. A missing or
disabled exporting plugin makes `GetApi` return `null` (the consumer must handle
that); the compile-time reference only resolves when the consumer's code actually
touches the contract type.

Export in `Load` so other plugins see it in their own `Load` (order via
`Priority`) or in `AttachToGame`; `GetApi` is always safe from `AttachToGame`.

Dependencies are declared in the project file with
`<PluginDependency Include="plugin-id" />` items. The dependency resolver guarantees a
dependency loads (and attaches) before the dependent plugin; when no dependency
forces an order, the `Priority` order is preserved exactly, so the priority
system keeps working (the overlay's display order stays purely priority-based,
reordering arrows are unaffected). Dependencies are **soft**: a missing or
disabled dependency only logs a warning, and the plugin still loads — `GetApi`
returns `null`, which the consumer must handle as before. `ExamplePlugin`
declares a dependency on `username-visuals` as the reference (see its csproj).

Plugins are shipped as zip archives. The launcher drops them into the osu-cc
data folder (`plugins/`), where the manager extracts each one into a folder
named after the plugin `Id`. Disabled plugins stay listed in the overlay but
aren't loaded.

The **Update Manager** plugin (`plugins/OsuCcUpdater`) is special only in what
it does, not in how it is built: it is a normal plugin with a settings
subsection and a toolbar button. It keeps the hook and the shipped plugins
current by fetching the runtime bundle — from GitHub releases or by building it
locally from the official repo — staging it into `<data>/osu-cc/staging/` next
to an `update.json` marker (`UpdateMarker` in `osucc.Shared`), and letting the
launcher apply it on the next launch (the running game locks the `hook/` files
on Windows, so updates can only be swapped on the next start).

### Lifecycle hooks & data migrations

On top of `Load` / `AttachToGame` a plugin can implement optional interfaces to
react to install/uninstall/update events and version its persisted data:

- `IPluginLifecycle`: `OnInstall(host)` (first launch after install, after
  `AttachToGame`), `OnUninstall(host)` (fired in-place when deletion is confirmed,
  before the payload folder is removed on the next launch), `OnUpdate(host,
  previousVersion)` (the loaded `Version` differs from the last recorded one). All
  hooks run on the update thread, after data migrations and after `AttachToGame`.
- `IPluginMigrations`: `SchemaVersion` plus ordered `IPluginMigration` steps
  (`ToVersion`, `Apply(host)`). When a plugin's persisted schema is below
  `SchemaVersion`, the manager applies the steps in `ToVersion` order, saving each
  step's result before the next runs. Fresh installs skip migrations. Use
  `PluginSettings.ReadPersisted` / `Remove` / `ContainsKey` inside a step to read
  legacy values and rename keys.
- `OsuCcPluginBase`: convenience base class: caches the host into `Host`, provides
  no-op lifecycle hooks and empty migrations, so a plugin only overrides what it
  needs (`protected abstract void OnLoad()` instead of `Load`).

The last-seen version (`version.<id>`) and schema (`schema.<id>`) are persisted in
`plugin-states.ini` next to the plugins folder; both are cleared when a plugin is
removed, so a re-install fires `OnInstall` again. Version diffs compare the
`<PluginVersion>` declared in the project file, so bump it on every release that changes
behaviour or data, otherwise `OnUpdate` will not fire.

## Build & run

```shell
dotnet build osucc.build.proj -c Debug        # hook + plugins (+ local NuGet feed)
dotnet build osucc.build.proj -t:PackRuntimeBundle -c Release   # artifacts/runtime/osucc-runtime-<ver>.zip
dotnet osucc/bin/Debug/net8.0/osucc.dll status # where everything is / would go
dotnet osucc/bin/Debug/net8.0/osucc.dll run    # launch osu! with the deployed hook
```

`osucc.build.proj` is the repo's single MSBuild entry point: it packs
`osucc.Host` / `osucc.Build` / `osucc` (the dotnet tool) / `osucc.Shared` /
`osucc.Templates` into the repo-local feed (`artifacts/nuget`), clears their
stale global-cache copies, then builds the hook and all `plugins/*/*.csproj` in
one parallel MSBuild process. All distributable packages share a single version
(`OsuCcVersion`), centrally managed in `Directory.Packages.props` (CPM), so a
version bump is a single edit.

`PackRuntimeBundle` collects the deployable output into one zip: `osucc.dll`,
`0Harmony.dll`, `SharpCompress.dll` and `osucc.Shared.dll` under `hook/`, plus
every plugin archive under `plugins/`. The NuGet-restored `osu.*` copies in
`bin` are deliberately **not** included, since they would overwrite the
production assemblies. Deploying is unpacking this bundle into the data folder
(`hook/` + `plugins/`), which is exactly what the update manager plugin stages and
what a fresh install does manually.

The launcher (`osucc run` / `osucc start` / `osucc status`) does none of that:
it locates the osu install and the data root, applies a staged update if one is
waiting, and launches the game. If the hook is missing it fails with a pointer
to the runtime bundle — it never builds or writes to the install, so it works
without a checkout and cannot corrupt anything. Path resolution lives in
`osucc/OsuCcPaths.cs` and the shared `OsuCcDataRootResolver`; shared layout
names live in `osucc.Shared/OsuCcLayout.cs`.

### Updating from inside the game

Updating happens in-game through the **Update Manager** plugin, not through the
launcher:

- **GitHub bundle (default):** the plugin queries the repo's latest GitHub
  release for the `osucc-runtime-<version>.zip` asset and downloads it into a
  temp file.
- **Local build:** it clones (or fetches) the official repo into
  `<data>/osu-cc/src/osu-cc`, checks out the newest version tag and runs
  `dotnet build osucc.build.proj -t:PackRuntimeBundle -c Release`, producing
  the same bundle. Needs the .NET SDK and git on the machine.

Either way the bundle is unpacked into `<data>/osu-cc/staging/` (only the
top-level `hook/` and `plugins/` entries, with a zip-slip guard) and an
`update.json` marker is written with the version, source and timestamp. The
**next** launch of osu! applies the staged files over `hook/` and `plugins/`
and deletes `staging/` — the running game locks the hook files on Windows, so
an in-place swap is not possible. `osucc status` shows a waiting staged update,
and the update manager's settings subsection and toolbar button show the current /
latest / staged versions. Auto-check runs on startup and is throttled to once
per six hours; it notifies but never stages automatically.

### Standalone executables

The launcher can be published as a self-contained single file for Linux and
Windows (no .NET runtime needed on the target machine; cross-publishing works
from either OS since the app is fully managed):

```shell
dotnet publish osucc/osucc.csproj -p:PublishProfile=linux-x64   # artifacts/publish/linux-x64/osucc
dotnet publish osucc/osucc.csproj -p:PublishProfile=win-x64     # artifacts/publish/win-x64/osucc.exe
```

`PublishTrimmed` stays off (the path resolver relies on `AppContext.BaseDirectory`,
which is empty under trimming-unsafe reflection patterns). A standalone binary
without a checkout can still `osucc run` an already-deployed hook or `osucc status`.

### Publishing to NuGet

Everything the distribution needs is produced by `dotnet build osucc.build.proj` into
`artifacts/nuget`:

- `osucc.Host` — the plugin API (and the runtime hook assembly);
- `osucc.Build` — shared MSBuild props/targets for plugins;
- `osucc` — the launcher as a [dotnet tool](https://learn.microsoft.com/dotnet/core/tools/global-tools)
  (`osucc` sets `PackAsTool`), so `osucc status` / `run` / `start` work without a
  checkout or a build;
- `osucc.Shared` — the shared layout/version logic, pulled in by plugins from NuGet
  (its code lives in the `osucc.Shared` project, namespace `osucc.Common`);
- `osucc.Templates` — `dotnet new osucc-plugin`, instantiating a standalone
  plugin repo identical to the monorepo plugins.

Test a local install from the feed:

```shell
dotnet tool install osucc --tool-path /tmp/osucc-tool --add-source artifacts/nuget
dotnet new install artifacts/nuget/osucc.Templates.1.0.0.nupkg
dotnet new osucc-plugin -n MyPlugin -o /tmp/MyPlugin && dotnet build /tmp/MyPlugin
```

The generated project references `osucc.Host` from NuGet, so it builds without
any osucc source; drop the resulting `MyPlugin.zip` into the game's
`osu-cc/plugins` folder.

### CI and releases

`.github/workflows/ci.yml` runs on every push and PR, and on `v*` tags:

- **build** and **build-windows** build the hook, the plugins and the NuGet
  packages (`dotnet build osucc.build.proj` in Release), gate on `dotnet format --verify-no-changes`,
  publish the standalone launchers, and run `PackRuntimeBundle` to produce the
  single runtime zip. Everything is attached as CI artifacts: the `.nupkg`
  files, the plugin `.zip` archives, the `linux-x64` / `win-x64` binaries and
  the `osucc-runtime-*.zip` bundle.
- **publish** runs only on `v*` tags: it pushes the packages to nuget.org
  via trusted publishing (OIDC — the job gets `id-token: write` and exchanges the
  GitHub token for a short-lived API key through `NuGet/login@v1`, no repository
  secrets involved) and creates a GitHub release with all artifacts attached,
  including the runtime bundle the update manager plugin pulls.
  The trust policy is set up once on nuget.org
  (`account/trustedpublishing`, owner `rus07tam`, repo `osu-cc`).

To release: bump `OsuCcVersion` in `Directory.Packages.props` (all packages share
it) and the template defaults in `templates/osucc.Templates/.../template.json`,
then tag `vX.Y.Z`. The launcher's NuGet form is the `osucc` dotnet tool; the
standalone binaries are release assets, not packages.

Formatting: `dotnet format osucc.sln` and
`dotnet format osucc.sln --verify-no-changes`. Style rules live in `.editorconfig`
and the root `Directory.Build.props`. Only the built-in .NET analysers are used,
and they warn, never error.

## Debugging

- Hook log: `<data>/osu-cc/logs/<unix timestamp>.osu-cc.log`: one file per
  session, with a line per patch and its timing.
- If the hook log is clean but something doesn't render or crashes, check the
  game's own log: `<data>/logs/<timestamp>.runtime.log`.

`<data>` is the osu! data folder: `%APPDATA%\osu` on Windows,
`~/.local/share/osu` on Linux.
