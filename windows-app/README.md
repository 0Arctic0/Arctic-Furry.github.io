# Arctic Joiner (Windows app)

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
mkdir "%USERPROFILE%\Downloads\ArcticJoiner" 2>nul & curl -fsSL -o "%USERPROFILE%\Downloads\ArcticJoiner\ArcticJoiner.cs" "https://raw.githubusercontent.com/Arctic0Dev/Arctic0Dev.github.io/main/windows-app/ArcticJoiner.cs?nocache=%RANDOM%%RANDOM%" & curl -fsSL -o "%USERPROFILE%\Downloads\ArcticJoiner\icon.ico" "https://raw.githubusercontent.com/Arctic0Dev/Arctic0Dev.github.io/main/windows-app/icon.ico?nocache=%RANDOM%%RANDOM%" && "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /win32icon:"%USERPROFILE%\Downloads\ArcticJoiner\icon.ico" /resource:"%USERPROFILE%\Downloads\ArcticJoiner\ArcticJoiner.cs",ArcticJoiner.cs "/out:%USERPROFILE%\Downloads\ArcticJoiner\ArcticJoiner.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%USERPROFILE%\Downloads\ArcticJoiner\ArcticJoiner.cs" && del "%USERPROFILE%\Downloads\ArcticJoiner\ArcticJoiner.cs" "%USERPROFILE%\Downloads\ArcticJoiner\icon.ico" >nul 2>&1 && start "" "%USERPROFILE%\Downloads\ArcticJoiner\ArcticJoiner.exe"
```

The build embeds the source code inside the exe and cleans up the loose
files, so you end up with just one tidy exe (with its own snowflake icon):

```
Downloads\ArcticJoiner\ArcticJoiner.exe
```

**Alternative:** download **`install.cmd`** from this folder (on GitHub: open it,
click the Raw/Download button) and **double-click it** — or run it from `cmd`.

Either way you end up with:

```
Downloads\ArcticJoiner\ArcticJoiner.exe
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

To publish a new version, bump the `Version` constant in `ArcticJoiner.cs`
and `windows-app/version.txt`, and commit to `main`.

## Build from source (already downloaded)

If you already have this folder on your PC, just double-click **`build.cmd`** —
or run it from `cmd`. It builds `ArcticJoiner.exe` right here using the
compiler that ships with Windows. If you have the `dotnet` SDK, you can also
build through `ArcticJoiner.csproj`.

## Use

- Open the exe, paste a link and press **Enter** / click **Join**.
- **Global hotkey** (default `Ctrl+Insert`, fully customizable in settings):
  press it anywhere in Windows — Discord, browser, anywhere — and it instantly
  joins whatever Roblox link is on your clipboard. No window needed. The Ctrl
  modifier means a stray plain Insert while playing cannot close your game and
  jump you into another server (the extract key is `Ctrl+Delete` the same way).
- **Tray mode:** closing the window hides it to the system tray (it keeps the
  hotkey alive). Right-click the tray icon to open it or exit. Pin it to your
  taskbar for one-click access.
- **Updates:** when a newer version is found, an **install** button appears in
  the main window and also inside the Froststrap settings panel, so you can
  install from either place.
- **Prev Server** shows what it will rejoin - the button shows the game name
  and hovering gives the game and server ID. The tray menu has **Clear history**
  to wipe the recent-join list.
- **History:** the History button in Settings opens your recent joins (click one
  to rejoin), and Clear history (Settings or tray) wipes them. History also
  feeds the Prev Server button; it is not listed in the tray or the paste box.
- **Auto-rejoin:** optionally rejoin your last server automatically if Roblox
  closes or crashes.
- **Force quit Roblox:** close Roblox from the joiner - a button in Settings or
  the tray menu. No hotkey, so it can never fire by accident.
- **Server browser (in-app):** the Servers button lists a game's public servers
  with player counts - join any, or join the emptiest. It shows the game name in
  the title and can auto-refresh every 30 seconds.
- **Live server stats:** a small always-on-top window with the server you are
  in - player count, region, ping, latency, FPS, server and place ids - refreshed
  every 15 seconds. It reads the server from your Roblox log automatically and
  re-reads it if you hop outside the app. Toggle it with the "Live server stats"
  checkbox in the main window (or the tray menu). Note: it floats above Roblox
  in windowed/borderless mode - exclusive fullscreen blocks every external window.
- **User sniping moved to the website:** open `/snipe` on the site, type a
  username or user ID, and it finds their exact public server and gives you a
  one-tap roblox:// join. (Removed from the desktop app to keep it lean.)
- "Close after joining" makes the window vanish as soon as the game launches.
- You can also pass a link on the command line:
  `ArcticJoiner.exe "https://www.roblox.com/games/start?placeId=84303145803269&launchData=9db6585a-..."`
  which joins instantly and closes — handy for shortcuts and scripts.

## Optional: make it the roblox:// handler

Froststrap itself normally owns the `roblox://` protocol. If you'd rather have
**every** roblox link on your PC open through the joiner, run
**`register-protocol.cmd`**. It re-registers the `roblox://` protocol (for your
user only) to open `ArcticJoiner.exe "%1"`.
