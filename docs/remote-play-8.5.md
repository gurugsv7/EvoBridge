# Remote Play 8.5: update-check workaround

This documents the local workaround used when newer PS Remote Play versions crashed before sign-in. It keeps the **8.5.0.08070** program and changes its file and product version metadata to **9.5.0.08040**, the version used in that setup. It does not install 9.5 functionality or fix every cause of crashes. Future Sony updates or server checks may make it ineffective.

**The patch changes Sony's signed executable, so its Authenticode signature will no longer validate.** Keep the original and restore it to undo the change. Do not disable Windows security settings to run the patch. No Sony executable or installer is distributed by EvoBridge; the script works on your own installed copy. This workaround is separate from controller mapping and does not prove Remote Play accepts EvoBridge's virtual controller.

## 1. Install and back up 8.5

1. Close PS Remote Play completely. If a newer version is installed, uninstall it using Windows Settings. Preserve any settings you want to keep before uninstalling.
2. Open [Uptodown's Windows version history](https://ps5-remote-play.en.uptodown.com/windows/versions) and select **8.5.0.08070**. This is a third-party download page. Check the downloaded installer's Digital Signatures tab for Sony Interactive Entertainment before installing. If a download bootstrapper installs a newer version, stop: this patch is for the original 8.5 executable only.
3. Install 8.5, but do not open it or accept an update yet. The usual executable path is `C:\Program Files (x86)\Sony\PS Remote Play\RemotePlay.exe`.
4. Create a writable working folder, such as `Documents\RemotePlay85-fix`. Copy the installed `RemotePlay.exe` into that folder and rename the copy **RemotePlay-8.5-original.exe**. Keep this backup outside the installation directory.

In the original file's Properties → Details, both file and product version should be **8.5.0.08070**. In Digital Signatures, verify the Sony signature. The included patcher requires the exact tested original SHA256:

```text
ACD3C15A35206B287BA1B1FD372994B2488A6FF3C675339A695EA1F0EEA46BC5
```

A different hash is not automatically malicious, but it is outside this patcher's tested input. Do not remove its validation to force a different file through.

## 2. Create a patched copy

Download the EvoBridge repository source ZIP from **Code → Download ZIP**, extract it, and open Windows PowerShell in that extracted repository folder. The portable app ZIP does not contain the development scripts.

Run this command in ordinary, non-administrator PowerShell. Change the working-folder paths if you chose another location:

```powershell
./scripts/Patch-RemotePlay85.ps1 `
    -SourcePath "$env:USERPROFILE\Documents\RemotePlay85-fix\RemotePlay-8.5-original.exe" `
    -OutputPath "$env:USERPROFILE\Documents\RemotePlay85-fix\RemotePlay-patched.exe"
```

If a local execution policy blocks the script, review it before using a process-scoped execution policy suitable for your machine. No global policy change is required. If Windows blocks the modified application, restore the original rather than disabling security protections.

The script verifies the original hash and Sony signature, checks the expected version records, and changes only the `.rsrc` section. It writes a new output file and refuses to overwrite an existing output. It does not launch Remote Play, replace the installation or alter your system settings. Expected output:

- FileVersion and ProductVersion: **9.5.0.08040**
- VersionStringsUpdated: **2**
- ChangesRestrictedToResourceSection: **true**
- SHA256: `C15447E29DB2E3545781DA04E3CE7B50FD1FB5C1838777AE7B06BA9E0BED7D6C`

These are the fixed and text version fields described by Microsoft's [VERSIONINFO resource documentation](https://learn.microsoft.com/en-us/windows/win32/menurc/versioninfo-resource). The underlying application is still 8.5; its displayed version is intentionally changed.

## 3. Replace the installed executable

1. Confirm Remote Play is closed and the original backup is safe.
2. In your working folder, make another copy of **RemotePlay-patched.exe** and name that copy **RemotePlay.exe**. Keep the original and patch output too.
3. Copy that new `RemotePlay.exe` into the existing PS Remote Play installation directory, replacing its `RemotePlay.exe`. Approve Windows' administrator permission prompt for that file replacement. Do not remove the other installed files.
4. Open Remote Play normally and test sign-in. Its Properties → Details will report 9.5.0.08040 even though the program is still 8.5.

The local setup stopped closing before sign-in after this workaround. It was not a live PS5/EvoBridge compatibility test. If the app still crashes, or Sony demands a newer client, restore the original; do not repeatedly change the version to arbitrary values.

## Undo or update

Close Remote Play. Copy **RemotePlay-8.5-original.exe** from your backup into the installation folder as **RemotePlay.exe**, replacing the patched file and approving the file-copy permission prompt. Its file/product version should return to 8.5.0.08070 and its Sony signature should validate again. Keep the backup until restoration is confirmed.

Alternatively, uninstall the modified Remote Play installation and install Sony's current release from the [official Remote Play page](https://remoteplay.dl.playstation.net/remoteplay/lang/en/1100001.html). A normal official update replaces this workaround. Old clients may lack later security and compatibility fixes; use the current client when it works for your setup.
