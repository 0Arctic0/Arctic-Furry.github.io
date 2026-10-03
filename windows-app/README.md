# Froststrap Joiner (Windows app)

A tiny Windows app for **instant joins**. Paste a Roblox link, and it launches
**Froststrap** directly with the right join info — no browser, no web page, no
extra clicks.

## How it joins instantly

1. It parses whatever link you paste and builds the matching `roblox://` deep link.
2. If you set the **Froststrap.exe location** in its settings, it launches
   `Froststrap.exe "<deeplink>"` directly.
3. If that box is empty, it opens the `roblox://` link the normal way — and since
   Froststrap registers itself as the roblox protocol handler, Froststrap is the
   app that opens.

## Supported links

- `https://www.roblox.com/games/start?placeId=...&launchData=...`
- `https://www.roblox.com/games/12345/Game-Name?gameInstanceId=...`
- `https://www.roblox.com/games/12345/Game-Name?privateServerLinkCode=...`
- `https://www.roblox.com/share?code=...&type=Server`
- raw `roblox://` deep links (passed straight through)
- a bare place ID (digits) or a bare share code
- links pasted without the `https://` prefix

## Install (fetches the latest from GitHub)

**Easiest — one command, no downloads:** open `cmd` (Windows key, type `cmd`,
Enter), then paste this single line and press Enter. It creates the folder in
your Downloads, downloads the latest source, builds the app, and opens it:

```
mkdir "%USERPROFILE%\Downloads\FroststrapJoiner" 2>nul & curl -fsSL -o "%USERPROFILE%\Downloads\FroststrapJoiner\FroststrapJoiner.cs" "https://raw.githubusercontent.com/Arctic-Furry/Arctic-Furry.github.io/main/windows-app/FroststrapJoiner.cs?nocache=%RANDOM%%RANDOM%" && "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ "/out:%USERPROFILE%\Downloads\FroststrapJoiner\FroststrapJoiner.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%USERPROFILE%\Downloads\FroststrapJoiner\FroststrapJoiner.cs" && start "" "%USERPROFILE%\Downloads\FroststrapJoiner\FroststrapJoiner.exe"
```

If your Windows is 32-bit, use this compiler path instead:
`%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe`

**Alternative:** download **`install.cmd`** from this folder (on GitHub: open it,
click the Raw/Download button) and **double-click it** — or run it from `cmd`.

Either way you end up with:

```
Downloads\FroststrapJoiner\FroststrapJoiner.exe
```

Nothing else is needed — no installers, no admin rights, no downloads from
anywhere except this GitHub repo.

## Auto-update from GitHub

The app **fetches updates from this GitHub repo automatically**:

- On every launch it checks `windows-app/version.txt` on `main` in the background.
- If a newer version is out, an **"Update available"** button appears — one click
  downloads the latest source, rebuilds itself with the built-in compiler, swaps
  the exe, and restarts. No reinstall, no download page.
- Keep the exe somewhere writable (e.g. your Desktop or a folder in your user
  profile — the default install location is fine), not Program Files.

To publish a new version, bump the `Version` constant in `FroststrapJoiner.cs`
and `windows-app/version.txt`, and commit to `main`.

## Build from source (already downloaded)

If you already have this folder on your PC, just double-click **`build.cmd`** —
or run it from `cmd`. It builds `FroststrapJoiner.exe` right here using the
compiler that ships with Windows. If you have the `dotnet` SDK, you can also
build through `FroststrapJoiner.csproj`.

## Use

- Open the exe, paste a link — it joins automatically the moment the link
  appears in the box (turn that off with the "Join instantly on paste" checkbox).
- Or paste and press **Enter** / click **Join**.
- "Close after joining" makes the window vanish as soon as the game launches.
- You can also pass a link on the command line:
  `FroststrapJoiner.exe "https://www.roblox.com/games/start?placeId=84303145803269&launchData=9db6585a-..."`
  which joins instantly and closes — handy for shortcuts and scripts.

## Optional: make it the roblox:// handler

Froststrap itself normally owns the `roblox://` protocol. If you'd rather have
**every** roblox link on your PC open through the joiner, run
**`register-protocol.cmd`**. It re-registers the `roblox://` protocol (for your
user only) to open `FroststrapJoiner.exe "%1"`.
