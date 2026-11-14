# Contributing

Build with `./build.ps1` and run `./scripts/Test.ps1` before submitting a change. Keep input processing separate from the UI, retain exact-device hiding checks, and preserve Stop/exit restoration. Do not weaken dependency hash checks or bind the backend to a public interface.

For hardware reports, include the controller model, connection type, mode, Windows version and whether buttons, both sticks, independent triggers, rumble and double-input prevention worked. Distinguish physical confirmation from an API accepting a command. Use synthetic identities in tests; redact serial numbers and device paths from reports.

Profiles, hiding journals, PSN credentials and raw private diagnostic reports do not belong in commits or issues. Do not redistribute proprietary mapper code or vendor drivers. Original code contributions are submitted under this repository's MIT licence; third-party files retain their own licences.
