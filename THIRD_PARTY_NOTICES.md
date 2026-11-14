# Third-party components

The MIT licence at the repository root applies to original EvoBridge code. It does not replace the following licences.

## VIIPER 0.9.0 — GPL-3.0

Upstream: https://github.com/Alia5/VIIPER/tree/v0.9.0

EvoBridge communicates over loopback with this separate, unmodified executable. The official Windows amd64 binary has SHA256 `142c144f6abee8b8a5f6a1abbc7018683444de71d2e45102cde6aaab9b2223e6`.

The portable release includes `backend/LICENSE.txt` and the corresponding upstream source at `backend/source/VIIPER-v0.9.0.tar.gz`, with SHA256 `5cddd9907fc68e069a17afe6254f18148bb72f8713fba558aff433f4e321b7ac`. The archive retains upstream copyright notices, build instructions and dependency manifests. Refer to its README and Go build files to rebuild VIIPER; no EvoBridge-specific backend patches are applied. Preserve the licence and corresponding source when redistributing the included backend.

The licence is also kept in [licenses/VIIPER/LICENSE.txt](licenses/VIIPER/LICENSE.txt). Source URL: https://github.com/Alia5/VIIPER/archive/refs/tags/v0.9.0.tar.gz

## SDL 3.4.20 — zlib licence

Upstream: https://github.com/libsdl-org/SDL/releases/tag/release-3.4.20

Copyright (C) 1997–2026 Sam Lantinga and contributors. The complete notice is in [licenses/SDL3-LICENSE.txt](licenses/SDL3-LICENSE.txt). The DLL is used unmodified. Its SHA256 is `66844f10251cc1c10619ed5d8e4e3224f564900ee6d69ba87d63de782addae40`.

## Separately installed drivers

[usbip-win2](https://github.com/vadimgrn/usbip-win2) and [HidHide](https://github.com/nefarius/HidHide) are external prerequisites. Their installers and drivers are not distributed in the EvoBridge portable package. See each upstream project for its licence, source and installation terms.

## Artwork

The controller illustration and screen design references were generated using GPT Image for EvoBridge. Prompts are documented in `design/prompts.md`. Reference images illustrate the intended design and are not captured screenshots. Windows fonts are used from the operating system and are not redistributed.
