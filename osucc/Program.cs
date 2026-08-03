using DotMake.CommandLine;
using osucc.App.Commands;

// Replaces the old deploy\run_custom.ps1. The hook never goes into the osu install dir;
// it is deployed to <osu-cc data root>\hook and activated only for the osu! process this
// tool launches (DOTNET_STARTUP_HOOKS is set on the child, never persisted).
//
//   osucc                  show help (no default action; use: osucc start)
//   osucc build            dotnet build hook + plugins
//   osucc deploy           copy hook + plugin archives into the osu-cc data root
//   osucc run              launch osu with the already-deployed hook (no build/deploy)
//   osucc start            build + deploy + run
//   osucc update           pull the latest hook + plugins (--launcher to update osucc itself)
//   osucc clean            remove hook files from the install dir (legacy) + data root
//   osucc status           print resolved paths and current state

return Cli.Run<RootCliCommand>(args);
