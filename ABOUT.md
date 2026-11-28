# About Theme Manager

Theme Manager is a Jellyfin 12.4 server plugin that adds themes to **Jellyfin Web**: colours, a background image and themed library banners. Each user picks their own theme. Custom CSS is never changed. Library images and the splash screen change only if you switch that on.

## What you get

- **29 built-in themes**: holidays (New Year, Valentine's Day, Easter, Halloween, Christmas and more), one theme per month, and 10 permanent colour themes.
- **A theme per user.** Click the **Themes** button in Jellyfin Web, preview a theme, and save it to your account. Other users keep theirs.
- **Automatic Seasonal mode.** Uses the month's theme, and holiday themes take over on their dates. Administrators can turn off holidays they don't celebrate.
- **Calendars.** Administrators can build shared theme calendars, and each user can have a personal one.
- **Library banners.** Library cards on the home screen get themed artwork. Libraries without a matching banner get a generic one with their name.
- **Seasonal effects** such as snow, falling leaves, hearts and confetti. They are off by default; an administrator can turn them on, and users can still turn them off. They never run during playback.
- **Themed video player.** Sliders, buttons and menus use the theme's colours, with nothing drawn over the video.
- **Your own themes.** Import a ZIP containing a `theme.json` and images. Packs contain data only, never scripts. You can hide, export, replace or delete themes from the settings page.

## TV and mobile apps

- **Jellyfin's Android and iOS phone apps** show Jellyfin Web, so they get each user's own theme like a browser (with File Transformation).
- **Native apps** (Android TV, Swiftfin, Roku and others) draw their own screens, so they can't show colours or personal themes. Turn on **Set library images for TV and mobile apps** and Theme Manager themes what these apps take from the server, from the **server default** theme, updated nightly:
  - **Library tiles**: the themed banner.
  - **Library backgrounds and thumbnails**: the full-screen background Android TV shows for a focused library and Roku shows inside a library.
  - **Splash screen** (optional): the Android TV start and login screens, the Swiftfin sign-in screen, and Roku's "Splashscreen Image" background.

  Originals are backed up first and restored exactly. Details per app are in [docs/NATIVE-APPS.md](docs/NATIVE-APPS.md).

## How it shows up

- **With the File Transformation plugin:** themes appear at the normal Jellyfin address. No Jellyfin files are changed on disk.
- **Without it:** themes are available at `<your server>/web/theme-manager.html`.

## Safety

- All API calls require a Jellyfin login, and admin settings require an administrator.
- A broken theme pack or plugin error never stops Jellyfin; the reason is written to the server log.
- Uploaded packs can't contain scripts or CSS.

## Install

Add this repository in **Dashboard → Plugins → Repositories**:

```
https://raw.githubusercontent.com/joshualent/jellyfin-theme-manager/main/repository/manifest.json
```

Then install **Theme Manager** and restart Jellyfin. The [README](README.md) has step-by-step instructions, troubleshooting and build notes.

This plugin is a fork of [KsH1/jellyfin-theme-manager](https://github.com/KsH1/jellyfin-theme-manager), maintained separately. Code is GPL-3.0-or-later; artwork is CC0.
