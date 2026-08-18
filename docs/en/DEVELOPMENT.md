# osu!cc: development

Notes for people hacking on this repo.

**Languages:** English · [Русский](../ru/DEVELOPMENT.md)

For plugin development documentation, see [PLUGINS.md](PLUGINS.md).

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
osucc/          the launcher CLI (status only; bare osucc launches - never builds, never writes to the install)
plugins/        the built-in plugins (CustomUserGroups, ExamplePlugin, FakeSupporter, FriendsLeaderboard, Oii, osuccDebug, OsuCcUpdater, SubdivideNations, UsernameVisuals)
docs/           screenshots (assets/), per-language docs (en/, ru/)
```

## How the hook works

The runtime calls `StartupHook.Initialize()` before `Main()` runs. It subscribes to `AppDomain.AssemblyLoad`, and when `osu.Game` shows up, `ClientBootstrapper.InstallPatches()` puts all patches in place.

Patch targets are resolved by assembly/type/method **name** at runtime, so a patch survives osu! version bumps. The UI/API code, on the other hand, compiles against the `ppy.osu.Game` NuGet reference. The production `osu.Game.dll` is usually newer than that reference, so never reference osu internals directly. Find them by name. Reflection helpers live in `osucc.Host/Core/Reflection.cs`.

One trap: `GetField`/`GetMethod` with `FlattenHierarchy` does **not** see private instance members of base classes. Read those from the declaring type or walk `BaseType`.

## Build & run

```shell
dotnet build osucc.build.proj -c Debug        # hook + plugins (+ local NuGet feed)
dotnet build osucc.build.proj -t:PackBootstrapBundle -c Release   # artifacts/runtime/osucc-runtime-<ver>.zip
dotnet osucc/bin/Debug/net8.0/osucc.dll status # where everything is / would go
dotnet osucc/bin/Debug/net8.0/osucc.dll        # launch osu! with the deployed hook (default action)
```

`osucc.build.proj` is the repo's single MSBuild entry point: it packs `osucc.Host` / `osucc.Build` / `osucc` (the dotnet tool) / `osucc.Shared` / `osucc.Templates` into the repo-local feed (`artifacts/nuget`), clears their stale global-cache copies, then builds the hook and all `plugins/*/*.csproj` in one parallel MSBuild process. Each distributable package is versioned independently (`OsuCcHostVersion`/`OsuCcBuildVersion`/`OsuCcSharedVersion`/`OsuCcLauncherVersion`/`OsuCcTemplatesVersion`), centrally managed in `Directory.Packages.props`, so a bump only touches the component that actually changed.

`PackBootstrapBundle` collects the deployable output into one zip: `osucc.dll`, `0Harmony.dll`, `SharpCompress.dll` and `osucc.Shared.dll` under `hook/`, plus every plugin archive under `plugins/`. The NuGet-restored `osu.*` copies in `bin` are deliberately **not** included, since they would overwrite the production assemblies. Deploying is unpacking this bundle into the data folder (`hook/` + `plugins/`), which is exactly what the update manager plugin stages and what a fresh install does manually.

The launcher (`osucc` / `osucc status`) does none of that: it locates the osu install and the data root, applies a staged update if one is waiting, and launches the game. If the hook is missing it fails with a pointer to the runtime bundle - it never builds or writes to the install, so it works without a checkout and cannot corrupt anything. Path resolution lives in `osucc/OsuCcPaths.cs` and the shared `OsuCcDataRootResolver`; shared layout names live in `osucc.Shared/OsuCcLayout.cs`.

### Updating from inside the game

Updating happens in-game through the **Update Manager** plugin, not through the launcher:

- **GitHub bundle (default):** the plugin queries the repo's latest GitHub release for the `osucc-runtime-<version>.zip` asset and downloads it into a temp file.
- **Local build:** it clones (or fetches) the official repo into `<data>/osu-cc/src/osu-cc`, checks out the newest version tag and runs `dotnet build osucc.build.proj -t:PackBootstrapBundle -c Release`, producing the same bundle. Needs the .NET SDK and git on the machine.

Either way the bundle is unpacked into `<data>/osu-cc/staging/` (only the top-level `hook/` and `plugins/` entries, with a zip-slip guard) and an `update.json` marker is written with the version, source and timestamp. The **next** launch of osu! applies the staged files over `hook/` and `plugins/` and deletes `staging/` - the running game locks the hook files on Windows, so an in-place swap is not possible. `osucc status` shows a waiting staged update, and the update manager's settings subsection and toolbar button show the current / latest / staged versions. Auto-check runs on startup and is throttled to once per six hours; it notifies but never stages automatically.

### Standalone executables

The launcher can be published as a self-contained single file for Linux and Windows (no .NET runtime needed on the target machine; cross-publishing works from either OS since the app is fully managed):

```shell
dotnet publish osucc/osucc.csproj -p:PublishProfile=linux-x64   # artifacts/publish/linux-x64/osucc
dotnet publish osucc/osucc.csproj -p:PublishProfile=win-x64     # artifacts/publish/win-x64/osucc.exe
```

`PublishTrimmed` stays off (the path resolver relies on `AppContext.BaseDirectory`, which is empty under trimming-unsafe reflection patterns). A standalone binary without a checkout can still launch an already-deployed hook (bare `osucc`) or `osucc status`.

### Publishing to NuGet

Everything the distribution needs is produced by `dotnet build osucc.build.proj` into `artifacts/nuget`:

- `osucc.Host` - the plugin API (and the runtime hook assembly);
- `osucc.Build` - shared MSBuild props/targets for plugins;
- `osucc` - the launcher as a [dotnet tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) (`osucc` sets `PackAsTool`), so bare `osucc` and `osucc status` work without a checkout or a build;
- `osucc.Shared` - the shared layout/version logic, pulled in by plugins from NuGet (its code lives in the `osucc.Shared` project, namespace `osucc.Common`);
- `osucc.Templates` - `dotnet new osucc-plugin`, instantiating a standalone plugin repo identical to the monorepo plugins.

Test a local install from the feed:

```shell
dotnet tool install osucc --tool-path /tmp/osucc-tool --add-source artifacts/nuget
dotnet new install artifacts/nuget/osucc.Templates.1.0.0.nupkg
dotnet new osucc-plugin -n MyPlugin -o /tmp/MyPlugin && dotnet build /tmp/MyPlugin
```

The generated project references `osucc.Host` from NuGet, so it builds without any osucc source; drop the resulting `my-plugin.zip` into the game's `osu-cc/plugins` folder.

### CI and releases

`.github/workflows/ci.yml` runs on every push and PR, and on `v*` tags:

- **build** and **build-windows** build the hook, the plugins and the NuGet packages (`dotnet build osucc.build.proj` in Release), gate on `dotnet format --verify-no-changes`, publish the standalone launchers, and run `PackBootstrapBundle` to produce the single runtime zip. Everything is attached as CI artifacts: the `.nupkg` files, the plugin `.zip` archives, the `linux-x64` / `win-x64` binaries and the `osucc-runtime-*.zip` bundle. Only the packages whose components changed against the base ref are staged for publishing (`.github/scripts/changed-packages.sh`: on a PR the base branch, on a `v*` tag the previous tag, otherwise the previous commit; changes to `osucc.Shared` also republish `osucc.Host` and `osucc`, its dependents).
- **publish** runs only on `v*` tags: it pushes the changed packages to nuget.org via trusted publishing (OIDC - the job gets `id-token: write` and exchanges the GitHub token for a short-lived API key through `NuGet/login@v1`, no repository secrets involved) and creates a GitHub release with all artifacts attached, including the runtime bundle the update manager plugin pulls. The release body lists the package version bumps detected by the build job. The trust policy is set up once on nuget.org (`account/trustedpublishing`, owner `rus07tam`, repo `osu-cc`).

To release: bump only the component versions in `Directory.Packages.props` that actually changed (and the matching template defaults in `templates/osucc.Templates/.../template.json`), then tag `vX.Y.Z`. The CI publishes exactly those packages and the update manager ships the bundle named after the host version. The launcher's NuGet form is the `osucc` dotnet tool; the standalone binaries are release assets, not packages.

Formatting: `dotnet format osucc.sln` and `dotnet format osucc.sln --verify-no-changes`. Style rules live in `.editorconfig` and the root `Directory.Build.props`. Only the built-in .NET analysers are used, and they warn, never error.

## Debugging

- Hook log: `<data>/osu-cc/logs/<unix timestamp>.osu-cc.log`: one file per session, with a line per patch and its timing.
- If the hook log is clean but something doesn't render or crashes, check the game's own log: `<data>/logs/<timestamp>.runtime.log`.

`<data>` is the osu! data folder: `%APPDATA%\osu` on Windows, `~/.local/share/osu` on Linux.
