# Pinned ServerSync for Valheim 1.0.7

- Baseline ID: `valheim-1.0.7-r1`; vendored as `Libs/ServerSync.dll`, merged into the mod by ILRepack.
- SHA-256: `b4dd786997f4e90d770f09ef3e9d64154754fe7e8edfb4841795751895b35846`.
- Replaced input SHA-256: `166956302a294e224474b26f4c7d58409084ad3f48bd0af1feb7551f229c8f60` (matches the common baseline; no independent fork detected).
- Upstream: https://github.com/blaxxun-boop/ServerSync, commit `c57c2aa54e07cdcc7630d6068699ea781622323e`, MIT-0; see `ServerSync.LICENSE.txt`.
- Assembly identity remains 1.0.0.0; file version 1.0.0.1.
- Fixes the constant `ZRoutedRpc.Everybody` references, uses `ZNet.IsAdmin`, and buffers peer initialization messages in FIFO order.
- Reproducible source, original input DLLs, build and verification records:
  `C:\Users\blizz\.codex\references\valheim\integrations\serversync\versions\valheim-1.0.7-r1`.
- The baseline's static/isolated checks do not establish real client/host/dedicated/crossplay compatibility. This port also checks the final merged DLL's game member references; it does not install a standalone ServerSync plugin.
