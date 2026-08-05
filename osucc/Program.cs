using DotMake.CommandLine;
using osucc.App.Commands;

// Pure launcher: osu! is launched with DOTNET_STARTUP_HOOKS set only on the child process, so the
// hook is active solely when started through osucc. The hook never goes into the osu install dir;
// it lives in <osu-cc data root>/hook and is installed/updated by the in-game updater plugin.
//
//   osucc                  show help (no default action; use: osucc start)
//   osucc start|run        apply any staged update, then launch osu with the hook
//   osucc status           print resolved paths, deployed hook/plugins and staged update
//
// The launcher never builds, never deploys and never updates anything itself: those duties belong
// to the in-game osu-cc updater plugin (pulls a osucc-runtime bundle from GitHub releases or builds
// locally from the official repo, stages it, and the launcher applies it on the next launch).

return Cli.Run<RootCliCommand>(args);
