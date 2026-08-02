# Security

**Read this in:** [Русский](../ru/SECURITY.md) · English

## The short version

This client does **not** guarantee the safety of your account. Using any
third-party client is against the osu! Terms of Service, and being flagged,
restricted or banned is a real risk. We do not claim that osu!cc is
undetectable or indistinguishable from a stock client. It simply isn't, and
we won't pretend otherwise.

## osu.Game.Auth is a black box

The proprietary blobs (`osu.Game.dll`, `osu.Game.Auth.dll`, `AuthNative.dll`)
are never modified or copied by osu!cc, and the auth x-token signing chain works
exactly like on a stock client. But that's all we can say: **nobody has audited
those binaries**, and it has **not** been verified that they don't perform
runtime analysis (scanning loaded modules, checksumming, behaviour heuristics)
that could detect a non-stock client. A future osu! update could add such checks
at any time, without warning.

## What we do on our side

The client does reduce its own footprint where it can:

- **Sentry error reporting is off by default.** osu's own error logger gets
  disabled via its kill-switch (`OSU_DISABLE_ERROR_REPORTING=1`). Left enabled,
  crash reports and stack traces could contain osu!cc-specific patterns
  (method names, assembly paths, plugin names) that the server could
  heuristically use to fingerprint the client. The toggle lives in the Specials
  settings; the default is off, and it takes effect from the next launch.
- **Nothing is written to the osu! install.** No files are copied into or
  modified in the install dir, so there is no persistent artifact on disk. The
  hook is loaded only when osu! is started through `osucc`; any other launch is
  a stock client.
- **Score submission can be switched off.** The "Disable score submission"
  toggle stops solo scores from being sent to the servers entirely; no submit
  request ever leaves the client. Local scores are unaffected.
- **The fake supporter tag is local-only.** The "Fake osu! supporter" toggle
  only rewrites the current player's supporter fields on *deserialized* responses,
  in memory: profile, leaderboards, scores, chat.
  No request, header or payload is altered: the server still sees the real
  account, and nothing about the client changes on the wire.

## Potential detection vectors

None of these are confirmed, and none are refuted. They are possible signals a
server-side check *could* use, and nothing more:

- **Allow incompatible mods.** The patches only lift the game's own mod
  validation during selection and before gameplay; score submission is **not**
  patched. A play with an incompatible mod set still goes through the normal
  submission flow, so the server may receive a submit request with an invalid
  mod combination, a request a stock client would never produce. Whether the
  server actually logs or checks this is unknown.
- **Fake supporter.** Enabling the toggle unlocks supporter-gated UI locally.
  If that UI leads to requests a non-supporter wouldn't make, the server could
  notice the difference. The feature itself never sends anything.

## Reporting

Found something that makes osu!cc easier to detect, or another security
concern? Open an issue or reach out to the maintainers; we can't promise a
perfect answer, but honest reports are always welcome.
