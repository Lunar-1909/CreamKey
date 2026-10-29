---
version: alpha
name: "CreamyKeys"
description: "A tactile Windows sound utility inspired by mechanical keyboards and the CreamyKeys Minecraft mod."
colors:
  canvas: "#111412"
  surface: "#1B201D"
  surface-elevated: "#242A26"
  border: "#3A443E"
  content: "#EEF3F0"
  text-primary: "#F7F9F8"
  text-muted: "#AAB6AF"
  action: "#50D5A4"
  action-active: "#35B98A"
  warning: "#F6C85F"
  danger: "#FF6F61"
typography:
  display:
    fontFamily: "Segoe UI Variable Display, Segoe UI, sans-serif"
  sans:
    fontFamily: "Segoe UI Variable Text, Segoe UI, sans-serif"
  mono:
    fontFamily: "Cascadia Mono, Consolas, monospace"
rounded:
  DEFAULT: "0.5rem"
  sm: "0.375rem"
  md: "0.5rem"
  lg: "0.75rem"
spacing:
  control-height: "2.5rem"
  page-gutter: "1.25rem"
  section-gap: "1rem"
components:
  button: {}
  card: {}
  dialog: {}
  sidebar: {}
  slider: {}
  virtual-device: {}
---

# CreamyKeys Design System

## Overview

### Creative North Star

CreamyKeys should feel like a compact audio workstation built around a mechanical keyboard: dark neutral hardware, warm keycap surfaces, precise spacing, and one responsive signal that proves the app is alive.

### Product context and register

- **Audience and primary job:** Windows users, gamers, and keyboard enthusiasts who want tactile keyboard and mouse sounds without opening a game.
- **Target markets and evidence:** General Windows desktop use; repository copy and existing controls are English-first.
- **Locales and language policy:** English UI with flexible control widths. User-authored process names and paths must never be truncated without a way to inspect them.
- **Usage scene:** Always-on tray utility opened briefly to choose a preset, tune sound, or inspect input activity.
- **Register:** Product UI. Task clarity and fast scanning lead; brand expression stays concentrated in the intro and live signal.
- **Memorable signature:** A mint sound-pulse waveform that reacts to key and mouse activity.
- **Restraint:** Settings, lists, and device controls remain quiet, compact, and predictable.
- **Anti-references:** No glassmorphism, oversized marketing cards, rainbow gradients, fake macOS traffic lights, or duplicate navigation rails.
- **Token ownership/runtime mapping:** `src/UiTheme.cs` is the runtime source of truth. This file mirrors its accepted semantic values and explains intent. Shared controls consume `UiTheme`; screen-local raw colors are treated as drift.

## Colors

Charcoal canvas and surfaces evoke hardware without becoming blue-gray. Warm off-white content panels keep dense controls readable. Mint is the only routine action/focus color; coral is reserved for destructive/error states and yellow for warnings. Text always uses `text-primary` or `text-muted` against a documented surface.

## Typography

Segoe UI Variable is preferred because CreamyKeys is a Windows utility; Segoe UI is the stable fallback. Display weights are limited to page titles and preset names. Body copy uses regular weight, while technical values may use Cascadia Mono or Consolas. Letter spacing remains zero.

## Layout

The shell uses a compact identity rail, a 220px navigation column, and one flexible workspace. Dashboard content owns one scroll region. Cards use a 16px rhythm and responsive docking rather than manual viewport offsets. The virtual keyboard and mouse are centered from measured bounds at every window size; arbitrary post-layout nudges are forbidden.

## Elevation & Depth

Hierarchy comes primarily from tonal surfaces and 1px borders. Only the app shell, dialogs, and elevated tray menu receive a soft shadow. Static settings sections do not stack cards inside cards.

## Shapes

Controls and cards use 6-12px radii. Buttons are rounded rectangles, not pills. The virtual keys use a smaller radius to preserve their physical-keycap character. Toggle switches and range sliders may use restrained vertical material shading to communicate physical depth; decorative page gradients remain out of scope.

## Components

### Foundational visual states

Every interactive control has default, hover, keyboard focus, pressed, disabled, and busy treatment. Focus uses a visible mint ring plus shape change where possible. Loading reserves stable geometry. Reduced-motion mode removes translation and switches pages immediately.

### Buttons and actions

Primary actions use mint with dark text. Neutral actions use elevated charcoal or warm off-white depending on surface. Danger uses coral and a clear destructive verb. Button dimensions remain stable during busy states.

### Navigation and data display

The current page has a mint leading marker and stronger text, not color alone. Navigation labels never abbreviate. Status is summarized in the shell and expanded on the dashboard.

### Forms and overlays

Native Windows combo boxes and file dialogs are accepted platform-owned controls. Numeric entry accompanies sliders where precision matters. Welcome/import/export dialogs stay within the working area and restore focus to their trigger.

### Iconography

Use Lucide line icons from the icon families supported by Morphicons, paired with text labels for navigation and unfamiliar actions. Native PNG masks are recolored at runtime for each surface, so icons remain legible without bringing a browser runtime into the desktop app. Icons are never the only accessible name.

### Motion

Hover feedback runs at 120-180ms. Toggles, sliders, the navigation marker, and page transitions use velocity-aware damped spring motion adapted from Animate UI's Motion patterns. Page transitions run for about 250ms and remain in a form-hosted overlay so they never participate in layout. The startup experience uses a six-second video cut with a visible skip action and Escape support; a compact native splash is the playback fallback. All routine animations remain interruptible and can be disabled with the app's reduced-motion setting.

Morphicons itself is a web SVG morph runtime, so CreamyKeys uses compatible Lucide shapes as native assets rather than embedding a web view. Icons support labels rather than replacing them, and the Lucide ISC license is bundled beside the sources.

### Content and data visualization

Copy is direct and action-led: `Save settings`, `Import settings`, `Export settings`. The waveform indicates recent activity only and does not pretend to measure audio volume precisely.

## Do's and Don'ts

- **Do:** Keep one obvious primary action in each decision area.
- **Do:** Center virtual devices from their actual rendered bounds after every resize.
- **Don't:** Reintroduce manual nudge constants or one-off color literals in page builders.
- **Don't:** Use decorative gradients, fake window controls, or animation that delays routine settings work.
