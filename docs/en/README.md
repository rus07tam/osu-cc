# osu!cc

**Docs:** English · [Русский](../ru/README.md) · [Development (Core)](DEVELOPMENT.md) · [Development (Plugins)](PLUGINS.md) · [Security](SECURITY.md) · [Contributing](CONTRIBUTING.md) · [Code of Conduct](../../CODE_OF_CONDUCT.md)

osu!cc is an add-on for osu!lazer. It is not an official client. Think of it as
a set of optional improvements on top of the game you already have.

It never touches the osu! installation itself: nothing is copied into or changed
in the game folder, and the auth x-token signing works exactly like on a stock
client. The only thing it leaves behind is its own `osu-cc` folder in the game's
data directory, where it keeps settings, plugins and logs. Removing the client is
just deleting that folder.

> **Warning.** osu!cc is not an official osu! client. Using it breaks the osu!
> Terms of Service, and your account is your responsibility. We are not liable
> for anything that happens to it. And to be clear from the start: this project
> will never contain cheats or anything of that kind. Read
> [SECURITY.md](SECURITY.md) before using it.

## What it does

Most features are toggles in a dedicated **Specials** section in the game settings.

### Core features

- **Plugin manager**: plugins can add toolbar buttons, settings sections,
  overlays and their own notifications. Plugins support lifecycle hooks (they run
  when a plugin is installed, updated or uninstalled) and versioned data
  migrations, so they stay in sync across updates. Toggle them from the plugins
  overlay.
- **Skip mid-map breaks**: skip break times inside beatmaps with a button during gameplay.
- **Allow incompatible mods**: pick and play mods that normally clash with
  each other.
- **Random mods button**: a button in the mod select footer that picks a
  random set of valid mods.
- **System mods column**: the usually-hidden `System` mod column (score v2,
  touch device, ...) shows up in the mod selector.
- **New personal best**: a full-screen particle show when you set a new
  personal best.
- **Disable score submission**: block solo scores from being submitted to the
  osu! servers; local scores are still saved, and a reminder shows when a play
  starts.
- **Fake osu! supporter**: a local-only supporter tag with a custom level
  (1 to 10 hearts) on the current player, everywhere the profile appears. Nothing
  is sent to the servers. Ships as the **Fake Supporter** plugin, which also adds
  per-user supporter overrides and a public rule API for other plugins.
- **Favourite map highlight**: favourited beatmaps get a pink pulsing outline
  in the song select carousel.
- **Download all favourites**: a button in a user profile's Beatmaps →
  Favourites section that downloads every favourited beatmap.
- **Branding**: the window title becomes "osu!cc".
- **Startup toast + first-run disclaimer**: a notification that the client
  loaded, and a one-time warning dialog.

### Bundled plugins

These ship with the client and can be disabled from the plugins overlay:

- **Custom user groups**: locally create and manage user groups, overriding user roles, badges, flags, and names on profile cards and leaderboards.
- **Username visuals**: paints every username with a horizontal gradient
  palette (your own name and everyone else's get separate palettes). It covers
  scoreboards, chat, profiles, multiplayer and toolbars. Your own username can
  also be replaced with a custom text or hidden behind a white block.
- **Subdivide Nations**: shows each user's sub-national region on profiles and
  user cards.
- **oii**: shows the improvement indicator next to the total play time on user
  profiles.
- **Friends leaderboard**: shows the friend leaderboard without an osu!supporter
  tag, built client-side from each friend's best score on the beatmap.
- **Debug overlay**: test panels for the notification and personal-best systems.
- **ExamplePlugin**: a reference implementation that demonstrates the plugin
  API. It is meant for developers; delete it if you do not need it.
- **Update Manager**: keeps the hook and the shipped plugins up to date without
  touching the install. It downloads the latest runtime bundle from GitHub
  releases, or builds it locally from the official repository (needs the .NET
  SDK and git), stages it and applies it on the next launch. It runs from a
  settings subsection, a toolbar button and a toggleable auto-check on startup.
  Removing it does not remove the hook - it only stops automatic updates.

The **Update Manager** is the recommended way to update from now on.


Planned:

- webview browser
- plugin browser: find plugins and their metadata on GitHub, install and enable
  them from the plugins overlay.
- more username-colour conditionals: per-player manual overrides, osu!supporter,
  has badge X, is a friend
- username font (also conditional, like the colours)
- game-wide font replacement
- local override for banners and badges
- custom banners and badges

## How it works

osu!lazer is a .NET app, so osu!cc loads through a .NET startup hook: the game
runs our code before it even reaches `Main()`. That is earlier than the usual
ruleset-injection trick and lets us change things other approaches cannot. The
proprietary `osu.Game.dll`, `osu.Game.Auth.dll` and `AuthNative.dll` are never
modified.

The hook only activates when osu! is launched through `osucc`. Starting it any
other way runs a vanilla osu!.

## Installation

### From source (recommended)

You need the .NET SDK 8.0 (download it from the
[official website](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)) and
osu!lazer installed.

```shell
git clone https://github.com/rus07tam/osu-cc.git
cd osu-cc
dotnet build osucc.build.proj -t:PackBootstrapBundle -c Release
```

This builds the hook and the plugins and produces a single runtime bundle,
`artifacts/runtime/osucc-runtime-<version>.zip`. Deploying is just unpacking it
into the game's `osu-cc` data folder (it contains `hook/` and `plugins/`):

```shell
# Windows: %APPDATA%\osu\osu-cc ; Linux/macOS: ~/.local/share/osu/osu-cc
unzip artifacts/runtime/osucc-runtime-1.0.0.zip -d <data>/osu-cc
```

Then launch the game with the hook loaded:

```shell
dotnet osucc/bin/Release/net8.0/osucc.dll
```

The first run also gives you the **Update Manager** plugin, which handles all
future updates in-game — rebuild the bundle (`dotnet build osucc.build.proj
-t:PackBootstrapBundle -c Release`), or let it pull the latest from GitHub
releases.

If you point the hook's location with `--osu-dir`, remember it must be the
osu! install that the data folder belongs to; the launcher only ever reads, it
never writes into the install.

### Binaries (standalone, no build)

No checkout or .NET SDK needed. Available once the first public release
(`v1.0.0`) is out.

1. Download `osucc` (Linux) or `osucc.exe` (Windows) **and**
   `osucc-runtime-<version>.zip` from the latest
   [GitHub release](https://github.com/rus07tam/osu-cc/releases); put the
   binary somewhere on your `PATH` (`chmod +x osucc` on Linux).
2. Deploy the runtime bundle by unpacking it into the game's `osu-cc` data
   folder (it creates `hook/` and `plugins/`):

   ```shell
   # Windows: %APPDATA%\osu\osu-cc ; Linux/macOS: ~/.local/share/osu/osu-cc
   unzip osucc-runtime-1.0.0.zip -d <data>/osu-cc
   ```

3. Start the game:

   ```shell
   osucc
   ```

The in-game **Update Manager** keeps everything up to date from then on
(rebuild locally or pull the newest release). If there is no hook deployed yet,
`osucc` refuses to start and points you at the release —
the launcher never installs anything on its own.

## Commands

Bare `osucc` launches the game (the default action); `osucc status` inspects the
installation:

| Command            | What it does                                |
| ---                | ---                                         |
| `osucc`            | launch osu! with the deployed hook, applying a staged update first if one is waiting |
| `osucc status`     | show the osu install, data root, hook version, plugins and any staged update |

Options: `--osu-dir <path>`, `--verbose|-v`.

The launcher is deliberately minimal: it **never builds** and **never writes**
to the install. If the hook is missing it fails with a message pointing at the
runtime bundle. Keep the hook and plugins current from inside the game with the
**Update Manager** plugin instead.

## Development

See [DEVELOPMENT.md](DEVELOPMENT.md) for the repo layout, how to write a
plugin, and how to debug. Want to help? See [CONTRIBUTING.md](CONTRIBUTING.md).

## Contributors

- [rus07tam](https://t.me/rus07tam_vf): project creator and maintainer (<rus07tam@ruject.fun>).
- [Meyblow](https://t.me/Meyblow): dedicated Windows 11 tester; came up with ideas behind several features.
- [tryhxo](https://t.me/tryhxo): Windows 11 testing.

## Special thanks

- [ppy/osu](https://github.com/ppy/osu) and
  [ppy/osu-framework](https://github.com/ppy/osu-framework): the game and the
  framework everything else builds on.
- [LazerAuthlibInjection](https://github.com/MingxuanGame/LazerAuthlibInjection):
  where the hooking approach comes from.
- [oii](https://github.com/ferryhmm/oii): the improvement indicator concept behind
  the bundled `oii` plugin.
- [osu-subdivide-nations](https://github.com/Cavitedev/osu-subdivide-nations): the
  regional flags that inspired the bundled `Subdivide Nations` plugin.
- [Harmony](https://github.com/pardeike/Harmony): the patching library used to
  change the game at runtime.
- [SharpCompress](https://github.com/adamhathcock/sharpcompress): used for
  extracting plugin archives.

## License

See the [LICENSE](../../LICENSE).
