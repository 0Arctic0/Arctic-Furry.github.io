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

## Build

Run PowerShell in this folder:

```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

This uses the C# compiler that already ships with Windows (`.NET Framework
csc.exe`), so nothing needs to be installed. If you have the `dotnet` SDK, it
builds through `FroststrapJoiner.csproj` instead. Either way you end up with
`FroststrapJoiner.exe` in this folder.

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
**every** roblox link on your PC open through the joiner, run:

```
powershell -ExecutionPolicy Bypass -File .\register-protocol.ps1
```

It re-registers the `roblox://` protocol (for your user only) to open
`FroststrapJoiner.exe "%1"`.
