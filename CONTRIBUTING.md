# Contributing to osu!cc

Thanks for considering a contribution. osu!cc is a small project, so a good issue
report or a well-scoped PR goes a long way.

Please also read:

- [docs/en/DEVELOPMENT.md](docs/en/DEVELOPMENT.md): repo layout, plugin authoring, debugging
- [docs/en/SECURITY.md](docs/en/SECURITY.md): account safety and detection-footprint policy
- [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)

## What we welcome

- **Bug reports**: issues with steps to reproduce, expected vs. actual behaviour,
  and any relevant log snippets.
- **Feature requests**: and the PRs that implement them.
- **Plugin requests and plugin PRs**: especially tools that integrate an existing
  osu! tool or workflow (like `plugins/SubdivideNations`, inspired by
  `osu-subdivide-nations`).
- **API extensions**: PRs that grow the plugin API (`IOsuCcPluginHost`) so plugins
  can do more without touching harmony patches.
- **Core PRs**: anything else that improves the client.

## What we won't accept

osu!cc is explicitly a **non-cheat** client (see the warning in the README). We
won't merge anything that gives an explicit advantage, including:

- gameplay automation (auto-play / auto-aim / relax and similar);
- inflating, faking or manipulating submitted scores or rank progression;
- altering what is sent to the osu! servers to gain an edge (on-the-wire changes);
- reading or exposing other users' private data.

Local-only changes to your own experience are the whole point of the project and
are welcome. If you're not sure whether an idea crosses the line, open an issue
and ask before writing code.

## Ground rules

- **Discuss first.** Open an issue before large PRs, especially ones touching the
  core bootstrapper (`osucc/Core/`) or the Harmony patches (`osucc/Patches/`).
- **No binaries in PRs.** Never commit osu! assemblies or NuGet blobs
  (`osu.Game.dll`, `osu.Game.Resources.dll`, `osuTK`, ...). The proprietary blobs
  are never modified, and the NuGet copies would overwrite production assemblies
  if deployed.
- **Never write into the osu! folder.** The hook lives in the osu! data folder
  (`<data>/osu-cc/hook/`), not in the install directory.
- **Keep the build clean.** `dotnet build osucc.App/osucc.App.csproj` should have
  zero warnings, and `dotnet format osucc.sln --verify-no-changes` should pass.
- **Update the docs.** Feature changes come with updates to the English and Russian
  docs (README, the feature list, DEVELOPMENT where relevant).
- **Licensing.** Contributions are released under the [Unlicense](LICENSE) (public
  domain), the same as the rest of the project.

## Plugins

A plugin is a classlib with a type marked `[OsuCcPlugin]`. New plugins live in
`plugins/<Name>/` and ship as a zip archive with an `icon.png`. Prefer the host
API (`IOsuCcPluginHost`) over your own Harmony patches whenever possible.
Everything you need is in [docs/en/DEVELOPMENT.md](docs/en/DEVELOPMENT.md).

## Getting started

```shell
dotnet build osucc.App/osucc.App.csproj -c Debug
dotnet osucc.App/bin/Debug/net8.0/osucc.dll start   # build + deploy + run
```

See the [README](README.md) for all launcher commands and options.
