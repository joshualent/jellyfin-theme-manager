# Theming the TV and mobile apps (experimental)

Branch `experimental/native-app-theming`. This page records what each Jellyfin app takes from the server and how far a server plugin can theme it. The findings come from the apps' source code (cloned 27 November 2026) and were tested on a Jellyfin 12.4.0 server.

## The short version

| App | Type | What Theme Manager can theme |
|---|---|---|
| Jellyfin Web (browser) | Web | Everything: each user's own theme, banners, effects, player colours |
| **Jellyfin Android** (phone) | WebView around the server's Jellyfin Web | **Everything, like a browser** (needs File Transformation). The native video player is not themed. |
| Jellyfin iOS, Jellyfin Media Player (desktop) | Web-based | Same as above (not tested separately) |
| **Android TV** | Native | Library tiles, the full-screen background of a focused library, splash and login screens |
| **Swiftfin** (iOS / tvOS) | Native | Library tiles (Primary, Thumb in some views), sign-in and user-select background |
| **Roku** | Native | Library tiles, the background inside a library, and the app-wide background if the user picks "Splashscreen Image" |
| Findroid and other native apps | Native | Library tiles (Primary) |

Native apps draw their own screens. They never load Jellyfin Web's CSS or JavaScript, so **colours, fonts and per-user themes cannot reach them from the server**. Everything a native app shows of the server's look comes through **images**, which are the same for every user. Theme Manager therefore uses the **server default** theme for native apps.

## What the apps read from the server

### Android TV ([jellyfin-androidtv](https://github.com/jellyfin/jellyfin-androidtv))
- Library tiles use the library's **Primary** image (`ui/itemhandling/BaseItemDtoBaseRowItem.kt`, `USER_VIEW -> PRIMARY`).
- When a home row item is focused, the app shows that item's **Backdrop** full-screen (`HomeRowsFragment` → `BackgroundService.setBackground(item)`). For a library tile, that is the **library backdrop**.
- The **splash screen** (`/Branding/Splashscreen`) is the background of the start and login screens, but only when Branding has *Enable splash screen* switched on (`BackgroundService.setBackground(server)`, `ServerRepository` reads `branding.splashscreenEnabled`).

### Swiftfin ([Swiftfin](https://github.com/jellyfin/Swiftfin))
- Library tiles are landscape **Primary** images. Views that prefer thumbs use the **Thumb** (`BaseItemDto+Poster.swift`).
- Backdrop contexts use the item **Backdrop**.
- The sign-in and user-select screens use the server **splash screen** as their background (`UserSignInView`, `SelectUserView`; it can be switched off in the app's own settings).

### Roku ([jellyfin-roku](https://github.com/jellyfin/jellyfin-roku))
- Library tiles use **Primary** (`components/data/FolderData.bs`).
- Inside a library, the background is the **library's backdrop** (`OtherLibrary.bs`, `AudioBookLibraryView.bs`: `SetBackground(parentItem.backdropUrl)`).
- *Settings → User Interface → General → Channel Background Image → Splashscreen Image* shows the **server splash screen behind every screen** (`Main.bs: setBackgroundImage`). Roku requests it with `tag=splash` and caches it for a long time, so a new theme may take a while to appear.
- Roku has many colour settings (`colorBackground`, `colorCursor`, `colorText` ...), but they are stored on the Roku device, not on the server. A server plugin cannot set them.

### Jellyfin Android (phone) ([jellyfin-android](https://github.com/jellyfin/jellyfin-android))
- `WebViewFragment` loads the server's own address. `JellyfinWebViewClient` only swaps the main JavaScript bundle and serves its `native/*.js` shims; `index.html` comes from the server unchanged. File Transformation's injection and Theme Manager's client therefore load as in a browser. Tested in a phone-sized touch browser (412 px, Android user agent): themed home, banners, Themes button and picker all work.

### Server (Jellyfin 12.4)
- `GET /Branding/Splashscreen` is anonymous once *SplashscreenEnabled* is on. It serves `BrandingOptions.SplashscreenLocation` if that file exists, otherwise Jellyfin's generated `splashscreen.png`.
- Items inherit a parent's backdrop only in narrow cases (`DtoService.AddInheritedImages`), so a library backdrop shows for the library itself, not on every movie without one.

## What this branch adds

Under **TV and mobile apps** on the settings page, below *Set library images for TV and mobile apps*:

1. **Also library backgrounds and thumbnails** (on by default with the main switch). Sets each library's **Thumb** to its banner and its **Backdrop** to the theme background. Libraries set to *keep own image* are left alone.
2. **Use the theme background as the server splash screen** (off by default; it changes Branding). Points Branding's splash screen at the theme background and switches the splash screen on.

Both follow the calendar with the existing nightly task, renamed to *Set images for apps*. Every image is backed up before it is first replaced. Switching an option off, switching the main setting off, choosing *Original Jellyfin* or **Restore original images** puts back exactly what was there:
- a library's own image, or no image if it had none
- the splash file the administrator uploaded, and whether the splash screen was on

If an administrator uploads a new splash screen while the option is on, that upload becomes the "original" and is restored later.

Tested on Jellyfin 12.4.0:
- one run set Primary, Thumb and Backdrop on both libraries, plus the splash screen
- a second run changed nothing
- each option restored independently
- a full restore returned a 64 px custom Primary image, removed images from a library that had none, and put back a custom splash upload with its *disabled* state
- the images were fetched exactly as Roku (`/branding/splashscreen?format=jpg&...`), Android TV and Swiftfin request them

## Not possible from the server, and what would be

- **Colours, fonts and personal themes in native apps.** This would need changes in each app, for example a fork of Android TV or Swiftfin that reads `/ThemeManager/Effective` and applies the palette. That is a separate client project per platform.
- **Themed content.** A possible next step on the server side: a seasonal collection ("Christmas picks", from genres or tags), which every app shows as a normal row.
- **Logo and Art images on libraries.** None of the three native apps reads them for libraries today.
