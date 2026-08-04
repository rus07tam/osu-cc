# osu!cc: development

Notes for people hacking on this repo.

**Languages:** English · [Русский](../ru/DEVELOPMENT.md)

## Layout

```plaintext
osucc.Host/     the startup hook DLL (classlib, net8.0), also the osucc.Host NuGet package
  StartupHook.cs     entry point the runtime calls
  Core/              bootstrapper, reflection helpers, logging
  Client/            public API + client state
  Patches/           the Harmony patches
  UI/                overlays, settings section, mod UI
  Plugin/            plugin manager and the host API
osucc/          the launcher CLI (build / deploy / run / start / clean / status)
plugins/        the built-in plugins (ExamplePlugin, FriendsLeaderboard, Oii, osuccDebug, SubdivideNations, UsernameVisuals)
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

A plugin is a classlib with a type marked `[OsuCcPlugin]`; see
`plugins/ExamplePlugin` for a working example. Through `IOsuCcPluginHost` a
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

Dependencies are declared on `[OsuCcPlugin]` with
`DependsOn = new[] { "plugin-id" }`. The dependency resolver guarantees a
dependency loads (and attaches) before the dependent plugin; when no dependency
forces an order, the `Priority` order is preserved exactly, so the priority
system keeps working (the overlay's display order stays purely priority-based,
reordering arrows are unaffected). Dependencies are **soft**: a missing or
disabled dependency only logs a warning, and the plugin still loads — `GetApi`
returns `null`, which the consumer must handle as before. `ExamplePlugin`
declares a dependency on `username-visuals` as the reference (see its attribute).

Plugins are shipped as zip archives. The launcher drops them into the osu-cc
data folder (`plugins/`), where the manager extracts each one into a folder
named after the plugin `Id`. Disabled plugins stay listed in the overlay but
aren't loaded.

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
`[OsuCcPlugin]` `Version` attribute, so bump it on every release that changes
behaviour or data, otherwise `OnUpdate` will not fire.

## Build & run

```shell
dotnet build osucc/osucc.csproj -c Debug
dotnet osucc/bin/Debug/net8.0/osucc.dll start       # build + deploy + run
```

`osucc build` delegates to the repo's single MSBuild entry point,
`osucc.build.proj`: it packs `osucc.Host` / `osucc.Build` / `osucc` (the dotnet
tool) / `osucc.Templates` into the repo-local feed (`artifacts/nuget`), clears
their stale global-cache copies, then builds the hook and all `plugins/*/*.csproj`
in one parallel MSBuild process. All four distributable packages share a single
version (`OsuCcVersion`), centrally managed in `Directory.Packages.props` (CPM),
so a version bump is a single edit.

`osucc deploy` copies `osucc.dll`, `0Harmony.dll` and `SharpCompress.dll` into
`<osu-cc data>/hook/` plus the plugin archives into `plugins/`. The
NuGet-restored `osu.*` copies in `bin` are deliberately **not** deployed, since they
would overwrite the production assemblies.

Building requires a local checkout: the launcher locates the repo by walking up
from its own location until it finds `osucc.sln` (`--repo` overrides). Commands
that do not compile (`run`, `status`, `clean`) work without one; `osucc run`
launches the already-deployed hook and never touches the repo.

### Updating without a build

`osucc update` keeps the hook and plugins current without a checkout, pulling
prebuilt artifacts straight from the public feeds:

- the hook: the latest `osucc.Host` package from nuget.org — the flat-container
  API returns the newest stable version, then the nupkg is unpacked to
  `<osu-cc data>/hook/` together with its runtime dependencies (`Lib.Harmony` →
  `0Harmony.dll`, `SharpCompress`), versions read from the package's own nuspec;
- the plugins: the zip assets of the latest GitHub release (the CI attaches them)
  are downloaded into `<osu-cc data>/plugins/`, where the in-game
  `PluginPackageStore` unpacks them on the next launch;
- the launcher (`--launcher`): a global dotnet tool runs `dotnet tool update`,
  a standalone binary is swapped for the release build of the same OS (Windows
  defers the replacement to a detached script because the running exe is locked).

`osucc update` skips the hook when the deployed `osucc.dll` already carries the
latest version, and always fetches the plugin archives from the latest GitHub
release (they are small and idempotent). It errors out if the latest release
ships no plugin archives, so a broken release is never a silent no-op.

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
without a checkout can still `osucc run` an already-deployed hook.

### Publishing to NuGet

Everything the distribution needs is produced by `osucc build` into
`artifacts/nuget`:

- `osucc.Host` — the plugin API (and the runtime hook assembly);
- `osucc.Build` — shared MSBuild props/targets for plugins;
- `osucc` — the launcher as a [dotnet tool](https://learn.microsoft.com/dotnet/core/tools/global-tools)
  (`osucc` sets `PackAsTool`), so `osucc status` / `run` work without a
  checkout or a build (`start`/`deploy` still need the repo);
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

- **build** and **build-windows** build the hook, the plugins and the four NuGet
  packages (`osucc build` in Release), gate on `dotnet format --verify-no-changes`,
  and publish the standalone launchers. Everything is attached as CI artifacts:
  the `.nupkg` files, the plugin `.zip` archives and the `linux-x64` / `win-x64`
  binaries.
- **publish** runs only on `v*` tags: it pushes the four packages to nuget.org
  via trusted publishing (OIDC — the job gets `id-token: write` and exchanges the
  GitHub token for a short-lived API key through `NuGet/login@v1`, no repository
  secrets involved) and creates a GitHub release with all artifacts attached.
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
