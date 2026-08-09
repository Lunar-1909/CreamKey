# CreamyKeys

CreamyKeys brings CreamyKeys-style keyboard and mouse ASMR sounds from Minecraft to normal Windows desktop apps and games.

## Download

Download the latest portable build from the GitHub Releases page:

- `CreamyKeys-portable.zip` - recommended portable package
- `CreamyKeys.exe` - standalone app executable

Extract the zip, then run `CreamyKeys.exe`.

## Quick Start

1. Run `CreamyKeys.exe`.
2. Open the **Sound library** page and make sure **App enabled**, **Keyboard sound**, and **Mouse sound** are enabled.
3. Pick a sound preset, adjust **Output volume** and **Key boost**, then press **Test**.
4. Open **Dashboard** to see the virtual keyboard and mouse.
5. Press **Save** after changing settings.
6. Use **Hide** to keep the app running in the tray.

If a game runs as administrator, run CreamyKeys as administrator too.

## Features

- Global keyboard sound for desktop apps and many games.
- Global mouse click sound, including left, right, middle, X1, and X2 buttons.
- CreamyKeys sound presets extracted from the Minecraft mod assets.
- Tray menu with settings, enable/disable, test sound, startup, admin, and exit actions.
- Virtual keyboard that lights up when you press physical keys.
- Virtual mouse that lights up when you click physical mouse buttons.
- Keyboard layouts: laptop, 60%, tenkeyless, and full size.
- Mouse styles: office, gaming, and compact.
- Auto-detect option to choose a matching keyboard and mouse style from Windows device names.
- Per-key and per-button customization in edit mode or by right-clicking a virtual button.
- Custom label, icon text, position offset, and custom `.wav` sound for each virtual key/button.
- Output volume, key boost, volume jitter, pitch jitter, cooldown, and max voice controls.
- Behavior settings for held key repeat, modifier keys, injected input, and startup.
- App allow list: checked apps are allowed to play CreamyKeys sounds, unchecked apps are blocked.
- Adjustable virtual device shadow: enable/disable, depth, and X/Y direction.

## Pages

### Dashboard

The Dashboard is the main view. It shows the virtual keyboard and, when enabled, the virtual mouse. You can select the keyboard layout and mouse style from the compact controls at the top.

Use **Auto detect** to let CreamyKeys choose the closest layout automatically.

### Sound Library

Sound settings live here:

- **App enabled** turns the whole app on or off.
- **Keyboard sound** enables key sounds.
- **Mouse sound** enables mouse sounds.
- **Show virtual mouse** shows or hides the virtual mouse.
- **Preset** selects a keyboard sound pack.
- **Output volume** controls app volume.
- **Key boost** makes CreamyKeys louder relative to other apps.
- **Volume jitter** and **Pitch jitter** add small random variation.

Most sliders also have a percentage box beside them, so you can type the exact value you want.

### Behavior

Use this page to tune how input is handled:

- **Held key repeat** plays sounds while holding a key.
- **Shift / Ctrl / Alt** allows modifier keys to make sound.
- **Ignore injected input** avoids playing sounds for synthetic input.
- **Run at startup** starts CreamyKeys with Windows.
- **Cooldown** limits how quickly repeated sounds can fire.
- **Max voices** limits overlapping sounds.

### Allowed Apps

Use this page to control where CreamyKeys works.

- Checked app: CreamyKeys can play sounds in that app.
- Unchecked app: CreamyKeys will stay silent in that app.

This is useful for excluding voice chat, recording apps, or apps where you do not want keyboard sounds.

## Tray Menu

CreamyKeys keeps running in the system tray when hidden. Right-click the tray icon for quick controls:

- Open settings
- Enable or disable
- Test sound
- Change preset, output volume, and key boost
- Toggle startup, key repeat, modifier keys, and injected input filtering
- Open config folder
- Run as administrator
- Exit

Double-click the tray icon to show the main window again.

## Custom Sounds

You can add or replace sounds in the `assets` folder.

Keyboard presets are stored in:

```text
assets\keyboards\<preset-name>\
```

Mouse sounds are stored in:

```text
assets\mouse\
```

For per-button custom sounds, enable edit mode or right-click a virtual key/button, then choose a `.wav` file.

## Config

User settings are saved here:

```text
%APPDATA%\CreamyKeysDesktop\config.json
```

A sample config is included as:

```text
config.example.json
```

## Build From Source

Requirements:

- Windows
- PowerShell
- .NET Framework C# compiler at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
- Python, only needed when extracting assets from the Minecraft mod jar

Build:

```powershell
.\build.ps1
```

Build without re-extracting assets:

```powershell
.\build.ps1 -SkipAssets
```

Use a custom CreamyKeys mod jar:

```powershell
.\build.ps1 -CreamyKeysJar "C:\path\to\CreamyKeys.jar"
```

Smoke test:

```powershell
.\dist\CreamyKeys.exe --smoke-test
```

Create the portable zip:

```powershell
Compress-Archive -Path "dist\*" -DestinationPath "CreamyKeys-portable.zip" -Force
```

## Troubleshooting

- **No sound:** check App enabled, Keyboard sound, Mouse sound, Windows volume, and Output volume.
- **Sound too quiet:** increase Output volume and Key boost. Windows master volume still affects the final loudness.
- **Game does not trigger sounds:** run CreamyKeys as administrator if the game is elevated.
- **Some games block hooks:** anti-cheat or exclusive input modes may block global keyboard/mouse hooks.
- **Mouse is hidden:** enable Mouse sound and Show virtual mouse in the Sound Library page.
- **Settings are wrong:** open the config folder from the app or tray menu and remove `config.json` to reset.

## Credits

Inspired by the CreamyKeys Minecraft mod.

Author: NTGH  
Idea: CreamyKeys mod - Minecraft
