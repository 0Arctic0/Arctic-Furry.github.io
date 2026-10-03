# Roblox Joiner Pages

Static GitHub Pages join helper for Roblox public servers and private server links.

## What it does

- Public server join links using:
  - `placeId`
  - optional `gameInstanceId`
  - optional `launchData` (from `games/start` links)
- `games/start` deep-launch links like `https://www.roblox.com/games/start?placeId=...&launchData=...`
- Private server join links using:
  - Roblox share links like `https://www.roblox.com/share?code=...&type=Server`
  - Roblox game URLs with `privateServerLinkCode=...`
- Froststrap-style invite parsing from pasted links
- Simple menu page for generating join links
- Dedicated private servers page
- Changelogs page
- Windows desktop app (`windows-app/`) that pastes a link and instantly launches Froststrap with it, plus pinned games for one-click joins without a link

## Routes

- `/menu` - manual input page
- `/invite` - opens a generated join page
- `/ps` - private servers and community submissions
- `/changelogs` - website update history
- `/snipe` - user sniper: find and join a specific user's public server
- `/servers` - server browser: see every public server of a game and join one directly

Examples:

- Public place only:
  - `/invite?placeId=606849621`
- `/invite?placeId=606849621&launchData=9db6585a-9598-4de7-981d-a82cab6bdec2`
- Public exact server:
  - `/invite?placeId=606849621&gameInstanceId=b7b55207-ab2f-4b62-acd7-7f5962b13a26`
- Private server:
  - `/invite?privateCode=c3a6b94b1cb67c41bd28313b491424ce`

## Supported private link formats

- `https://www.roblox.com/share?code=...&type=Server`
- `https://www.roblox.com/games/1962086868/Tower-of-Hell?privateServerLinkCode=00589493508464507079428306544385`

## Notes

- Public joins can open Roblox directly using a `roblox://` deep link.
- Private joins can open Roblox directly using:
  - `roblox://navigation/share_links?code=SHARE_CODE&type=Server`
- In private mode, opening a normal non-forcing game page is only possible if a public `placeId` is known separately.

## Tech

- Plain HTML
- Plain CSS
- Plain JavaScript
- GitHub Pages static hosting

## Made by

- Built for **Arctic0Dev**
- Implemented with help from **Cline**

## Publishing

This repo is designed for GitHub Pages from the `main` branch root.