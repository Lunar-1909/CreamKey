# UX Contract

## Product context

- Audience: Windows users who want keyboard and mouse sound feedback.
- Primary jobs: enable sounds, select presets, tune variation, control app scope, and keep the utility running in the tray.
- Target markets: General Windows desktop use.
- Active locales: English (`en-US`) with layout-safe user-authored values.
- Accessibility target: WCAG 2.2 AA-equivalent keyboard, contrast, focus, and reduced-motion behavior for a native desktop app.

## Business-context sources

| Domain / scope | Authoritative source | Source type | Reviewed date |
|---|---|---|---|
| Sound behavior | `CreamyKeys-1.21.X.jar` bytecode and bundled assets | Upstream implementation evidence | 2026-10-28 |
| User settings | `src/CreamyKeysDesktop.cs` and `config.example.json` | Runtime contract | 2026-10-28 |
| Product features | `README.md` | Product documentation | 2026-10-28 |

## Visual contract

- Project `DESIGN.md`: `DESIGN.md`
- Token ownership model: Existing runtime canonical.
- Runtime design-system/token source: `src/UiTheme.cs`
- Mapping/export/adapters: WinForms shared controls and page builders consume `UiTheme` semantic properties.
- Token drift gate: search changed UI code for raw `Color.FromArgb` values outside shared drawing primitives.
- Supported themes: CreamyKeys charcoal + warm-light workspace.

## Canonical UI Map

| Capability | Canonical owner | Source of truth | Allowed variants | Verification |
|---|---|---|---|---|
| Select/Listbox | Native Windows `ComboBox` / list controls | This contract | platform-owned | keyboard + open popup |
| Form/settings | `MainForm` settings binding methods | This contract | live preview / saved | import-export + smoke test |
| Status feedback | Shared status bar and themed app dialogs | This contract | success / warning / error | focus + stable geometry |
| Navigation | Shared sidebar buttons + hidden tab host | `DESIGN.md` | expanded sidebar | keyboard + resize |

## Component behavior

| Component | Default | Hover | Focus | Active | Disabled | Busy | Error |
|---|---|---|---|---|---|---|---|
| Button | Semantic surface | Tonal lift | Mint ring | Pressed offset | Muted, inert | Stable spinner/label | Inline status |
| Toggle | Label + state text | Track lift | Mint ring | Animated knob | Muted, inert | n/a | Inline status |
| Slider | Track + numeric value | Thumb lift | Mint ring | Smooth value | Muted, inert | n/a | Clamp + status |
| Native select | Platform popup | Platform | Platform focus + shell label | Platform | Muted, inert | n/a | Preserve prior value |

## Flow ledger

| Operation | Trigger | Pending | Success destination | Success feedback | Failure recovery | Focus outcome | Source ref |
|---|---|---|---|---|---|---|---|
| Save settings | `Save settings` | Stable busy state when needed | Same page | Status bar | Keep values; show actionable error | Trigger | Runtime contract |
| Import settings | `Import` | Loading overlay | Same page with imported values | Status bar | Keep current config; explain invalid file | Trigger | Runtime contract |
| Export settings | `Export` | Stable button | Same page | Status bar | Explain path/write failure | Trigger | Runtime contract |
| Switch page | Sidebar icon and page label | ~250ms damped spring slide | Selected panel | Spring-driven mint marker | Keep current panel on failure | Selected nav item | `DESIGN.md` |
| Hide app | `Hide to tray` | None | System tray | Tray state | Reopen from tray | Main window | Runtime contract |
| Launch while running | Open `CreamyKeys.exe` again | Named activation event | Existing main window | Window restored and focused | Keep tray process alive | Main window | Runtime contract |

## Navigation and responsive behavior

- Window title policy: `{Page} - CreamyKeys` while the selected page is visible.
- Sidebar stays persistent at supported desktop widths; labels never collapse into ambiguous icons.
- Each page owns its own scrolling. Bottom actions and status remain reachable.
- Keyboard focus is never covered by the bottom action bar.

## Overlays and feedback

- Dialog primitive: Themed WinForms forms with explicit title, close action, Escape behavior, and focus restoration.
- Toast/status: Stable bottom status bar; transient dialogs only for blocking startup/hook failures.
- Unsaved changes: Live preview is marked `Unsaved`; Save persists. Import never overwrites the saved file until validation succeeds.
- Layer order: base shell < loading overlay < modal welcome/import errors.

## Async and resilience

- Startup presents the bundled six-second trailer with `Skip intro` and Escape; playback failure falls back to the native staged splash.
- Startup loading is determinate by named stages, not a fake percentage.
- Very fast work avoids flashing the overlay.
- Import is pessimistic: validate and normalize a temporary config before applying it.
- Export writes only to the user-selected path and does not alter the active config.

## Validation

- Config validation: `AppConfig.Normalize` plus preset existence fallback.
- Invalid import preserves the current in-memory and saved settings.
- Custom sound paths remain user-controlled and are checked before playback.

## Verification

- Required commands: build, smoke test, settings round-trip test, premium static audit.
- Window matrix: minimum size, default size, maximized, and resize after restore.
- Accessibility checks: keyboard traversal, visible focus, Escape, reduced motion, long labels.
- Canonical sibling flow: Sound and Behavior settings pages share the same controls and action vocabulary.
