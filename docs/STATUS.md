# Development status

Updated 26 November 2026 for version 0.4.0.0 (preview), the first release of this fork.

This repository (`joshualent/jellyfin-theme-manager`) is a separately managed fork of [KsH1/jellyfin-theme-manager](https://github.com/KsH1/jellyfin-theme-manager) at 0.3.4.0. It has its own plugin GUID (`267f4d8e-f04c-49ab-9373-f721912cee86`) and its own repository manifest, so it installs and updates independently of the original plugin. History below 0.4.0 is the original project's. User documentation is in the [README](../README.md) and [CUSTOM-THEMES.md](CUSTOM-THEMES.md).

## Supported

- Jellyfin 12.4 (`targetAbi` 12.4.0.0), .NET 10. The target version is `DefaultJellyfinVersion` in `Directory.Build.props`; more versions can be added to `JELLYFIN_VERSIONS` in the release workflow.
- Upstream releases up to 0.3.4.0 targeted 12.0, 12.1 and 12.2.
- Installed from the plugin repository `repository/manifest.json` on GitHub. Plugin ZIPs are GitHub release assets. The fork's manifest starts empty and lists only this fork's releases.
- Jellyfin 10.x is not supported.

## Built

| Area | What |
|---|---|
| Themes | 29 standard packs (19 calendar themes with artwork, 10 colour themes), seeded once on first start. Data-only packs: `theme.json` plus PNG/JPEG/WebP. |
| Choice | Per user: follow server default, Automatic Seasonal, fixed theme, calendar, or original Jellyfin. Saved per Jellyfin account. |
| Calendars | Shared calendars (administrator) and one personal calendar per user. Holidays can be left out of Automatic Seasonal; Fastelavn and Midsummer start left out outside Denmark. |
| Web client | Background, palette and library banners in Jellyfin Web; theme picker with tabs, thumbnails, search and preview. At the normal address with File Transformation, otherwise at `/web/theme-manager.html`. |
| Library banners | Themed banner (Movies, TV Shows, Collections, Live TV) for one library per type, generic banner with the library name for the rest. Choice per library: any theme banner, generic, or the library's own image. |
| Apps | Optional library images for TV and mobile apps from the server default, with backup and exact restore. Off by default. |
| Player | The page theme is off over the video; the accent colour stays on sliders and buttons, menus get the theme surface, and the fades behind the title and controls take the theme colours. Tested with a generated video on a test server. |
| Effects | Snow, leaves, hearts, confetti. Off by default; users can switch them off; never during playback, on admin pages or with reduced motion. |
| Theme management | Import, replace (old version kept), hide from the picker, export as ZIP, delete (moved to `theme-manager/deleted-packs`). |
| Status | Settings page shows web-client state, versions, and when a newer installed version needs a full restart. |
| Release | GitHub Actions workflow on tag `v*`: tests, builds for all three Jellyfin versions, creates the release and commits the manifest. |

## Verified on Jellyfin 12.4 (26 November 2026)

On a clean Jellyfin 12.4.0 server (Debian 13, Linux tarball): `dotnet test` 103 passed and client tests 33 passed against the 12.4.0 packages; the 0.4.0.0 package installed from a repository manifest via the catalog, loaded as *Active* after a restart, installed all 29 themes, and its API, settings page and asset previews answered. File Transformation 3.0.3.0 (12.4 build) registered the web injection; in headless Chromium the home screen showed the Christmas theme and banners, the theme picker opened, and there were no console errors.

## Verified (upstream, Jellyfin 12.2)

Every test below starts its own Jellyfin with empty data on a loopback port; none touches a running server.

| Test | Result (19 October 2026) |
|---|---|
| Unit tests (`dotnet test`) | 103 passed |
| Client tests (`node --test tests/client/core.test.mjs`) | 33 passed |
| `Test-IsolatedServer.ps1` (API, access rules, import, theme management, previews, holidays, effects default) | 67 checks passed on 12.2 |
| `Test-ServerImages.ps1` (app images, per-library banners, restore) | 22 checks passed on 12.2 |
| `Test-WebInjection.ps1` (File Transformation, stale browser cache) | 25 checks passed on 12.2 |
| `Test-DefaultPacks.ps1` | 9 checks passed |
| `Test-CatalogInstall.ps1` (install from the published repository) | 0.3.0.0 from the GitHub release: 7 checks passed on 12.2. Earlier versions also passed on 12.0; 12.1 confirmed on a real 12.1 server |

Jellyfin Web's new layout (MUI app bar, menus, dialogs) was checked page by page in a real browser on 19 October 2026: home, library, item details, search, favourites, settings, Live TV, queue, the item menu and the user menu. The surfaces found are listed in a client test.

Not verified: Linux and Docker hosts, Firefox and Safari, two users in parallel browsers, keyboard navigation and contrast across the whole picker, native apps beyond library images.

## Open

1. Artwork for the 10 colour themes (needs an image tool).
2. Jellyfin 10.11 support, only if there is demand.
