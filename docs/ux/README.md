# Handoff: TestPackage Configurator — "Storyboard" UX revamp

## Overview
A redesign of the TestPackage Configurator (WPF, .NET 8). The 7-tab TabControl is replaced by a **storyboard IA**: five frames laid left-to-right that narrate the life of the generated installer — 01 Identity → 02 Wizard → 03 Install actions → 04 First run → 05 Uninstall — with a persistent **Receipt** rail showing the live footprint of the configured installer. Design goals it solves:

- "What will this actually do?" — the Receipt rail + per-frame summaries give a static, always-visible answer.
- Host vs. fake-app identity confusion — a red-tinted "The fake app" persona card and copy ("who the fake app claims to be", "the fake installer you generate") keep the two identities separate from the host chrome.
- Mixed tab grouping — frames group by what the user is thinking about, in install-chronology order.
- Composite-field data entry — pipe-string rows are replaced by a master-list + detail-form editor.
- Presets — a "Start from" row (Blank / Typical desktop app / Service + firewall / Enterprise) applies behavior sets in one click.

## About the Design Files
The files in this bundle are **design references created in HTML** — a clickable prototype showing intended look and behavior, not production code. The task is to **recreate this design in the existing WPF/.NET 8 codebase** (RWK-Systems/test-package, `src/TestPackage.Configurator`) using its established styles (Card, FieldTextBox, FeatureToggle, SectionLabel, PrimaryButton, SmallButton, RemoveButton — extended as needed). The underlying data model (`src/TestPackage.Core/ConfigModel.cs`) is unchanged; this is a re-projection of the same model onto a new IA.

Note on visual style: the prototype uses the "Modernist" design system (Archivo, red #EC3013 accent, 0 corner radius, 2px rules) as a placeholder aesthetic. Adopt it or keep the app's existing theme — the **IA, layout, interactions and copy are the deliverable**; the color/typography layer is swappable.

## Fidelity
**High-fidelity for structure, interaction and copy; medium-fidelity for visual skin.** Recreate layout, hierarchy, states and behavior faithfully; map colors/typography onto whichever theme you keep.

## Screens / Views

### Main window (single view, ~1560×960 min)
Vertical stack, top to bottom, all separated by 2px rules:

1. **Header bar** (~56px): app brand "TESTPACKAGE / Installer generator" left (host identity — never editable); "Start from" preset buttons (Blank, Typical desktop app, Service + firewall, Enterprise); right-aligned: Load, Save, Preview wizard (secondary), **Generate installer** (primary, accent fill).
2. **Persona band** (~72px, two cells):
   - Left cell (300px, accent-tinted background — e.g. accent ramp 100): kicker "THE FAKE APP" (10px caps, accent-800), app name (20px, 800 weight), "v2.5.1 · Your Mom" (12px, 70% opacity). Live-bound to Identity fields.
   - Right cell: one-line explainer: "This storyboard is the **life of your generated installer**, left to right. Click any frame to edit it; click a chip to turn that behavior on or off. Output: `YourSimulatedSetup.exe` → installs `YourSimulatedApp.exe` + audit viewer."
3. **Storyboard strip** (5 frames, grid columns 1fr / 1fr / 1.25fr / 1fr / 1fr, min-height 190px). Each frame:
   - Number badge ("01"–"05", heading font 13px; selected = white on accent block, unselected = accent text)
   - Title (15px, 800) + one-line subtitle (11px, 55% opacity)
   - **Chip cloud**: one chip per item in that frame (wizard pages, all 15 install behaviors, uninstall behaviors). Chip = 11px label, 3px/8px padding, 1px border. ON = ink background, background-color text, weight 600. OFF = divider border, 55% opacity. **Clicking a chip toggles it directly** (stops propagation — does not select the frame).
   - Summary line pinned to bottom (11px, 600, accent-800): e.g. "9 pages shown", "5 of 15 behaviors on", "2 intentional leftovers" / "Clean removal".
   - Affordance line under summary: selected frame = "EDITING BELOW ↓" in accent; others = "CLICK TO EDIT ↓" at 45% opacity (10px caps, 600).
   - Selected frame: page background + inset 4px accent underline. Unselected: surface background. Hover: 4% ink tint.
4. **Editor area** (fills rest; two columns — editor 1fr, Receipt rail 372px):

#### Frame editors (left column, scrollable, 24px padding)
Every editor opens with: kicker "FRAME 0N" (10px caps accent), title (24px), one-paragraph description (13px, 65% opacity), 2px rule.

- **01 Identity** — 2-col field grid (max 700px): Application name, Publisher, Version, URL, Product GUID + "New GUID" button (generates fresh GUID), Install context segmented control (Per-user / Per-machine (UAC)), Payload size slider (0–100 non-linear cubic; label shows computed "245 MB"/"1.1 GB"; helper text "Fine MB control left, GB right. Pads the .exe for large-payload tests."), Install directory (derived live from `<root>\<Publisher>\<App Name>` where root = `C:\Program Files` per-machine or `%LocalAppData%\Programs` per-user; typing overrides, clearing re-derives), Installer EXE name, Fake app EXE name, Output folder + browse. Monospace font for all path/EXE/GUID values.
- **02 Wizard** — ordered page list (rows, 1px rule between): position number (accent, "01"…, or "—" when hidden), page label, optional "pre-checked" checkbox segment (only for Desktop shortcut / Start Menu pin / Reboot / Active Setup pages — sets the wizard default), on/off toggle switch. Hidden rows drop to 45% opacity and show "hidden". Numbers renumber live. Below the rule: EULA multiline textbox; then "Wizard appearance": Banner color + Accent color (swatch + hex box), Install delay per step.
- **03 Install actions** — 15 rows: toggle switch, label (14px 600), live detail line in monospace 60% opacity (e.g. "3 values → 2 HKCU, 1 HKLM", "TCP 19876 in, 19877 out"), and — for behaviors with row data (Files, Registry, Env vars, File associations, Firewall, Protocols, Context menu) — an accent "Edit data →" ghost button visible only when enabled. Disabled rows at 50% opacity.
- **04 First run** — "Companion audit app" card (1px ink border + 3px offset hard shadow): explains the installed EXE is an audit viewer listing everything the installer did. Reboot segmented control (None / Prompt / Force; reflected as a chip on frame 04). Silent-install reference line: `YourSimulatedSetup.exe /S /dir="C:\Apps" /context=machine /components=CoreFiles` in a monospace surface block.
- **05 Uninstall** — rows: label + description, tag (STANDARD / CLEAN neutral; TEST TRAP accent-tinted), toggle. Rows: Add/Remove Programs entry, Clean files, Clean registry, Clean shortcuts, Leave files behind, Leave registry behind.

#### Receipt rail (right, 372px, surface background, 2px left rule)
- Header: kicker "RECEIPT" + "Footprint on the test machine".
- Key/value rows (12px, 1px rules): Wizard pages, Files written, Registry values, Shortcuts, Services / tasks, Firewall rules, File types / protocols, Env variables, Other registrations, Reboot, ARP entry, Intentional leftovers (this row turns accent-800 bold when > 0). All values recompute live from state.
- Pinned footer: Installer size (from slider) and Elevation ("UAC prompt" / "None (per-user)").

### Data editor overlay (master list + detail)
Opened by "Edit data →". Right-anchored panel 860px over a 35% scrim (click scrim to close). Header: kicker "INSTALL ACTION" + behavior name + "Close ✕". Two columns:
- **Master list** (1fr): one row per entry — primary field in monospace 12px 600, remaining fields joined with " · " beneath at 55% opacity. Selected row: accent-100 background + 3px accent left border. "+ Add entry" ghost button appends a blank entry and selects it.
- **Detail form** (340px, surface): "EDIT ENTRY N", one labeled textbox per field (monospace), "Remove entry" button.

Field schemas per behavior:
- Files: Path, Description
- Registry: Key path, Value name, Type, Data
- Env vars: Variable, Value
- File associations: Extension, ProgID, Description, Icon path
- Firewall: Rule name, Direction, Action, Protocol, Port
- Protocols: Scheme, Description
- Context menu: Menu label, Applies to

### Generate confirmation dialog
Modal (560px, 2px ink border): title "Ready to generate"; body summarizes: setup EXE name + computed size → output folder; "shows N wizard pages, performs N install actions, installs `<app exe>`, and registers "<App>" by <Publisher> in Add/Remove Programs." Actions: Back (secondary), Generate (primary).

## Interactions & Behavior
- Frame click selects it; editor swaps below. Chip click toggles the underlying item without changing selection.
- All derived text updates immediately: persona card, frame summaries, chip states, action detail lines, receipt counts, dialog copy.
- Presets replace the install-action set: Blank = none; Typical desktop app = files, registry, shortcuts, file associations, App Paths; Service + firewall = files, registry, shortcuts, service, firewall; Enterprise = files, registry, shortcuts, service, task, Active Setup, App Paths, env vars.
- Derived install dir: override by typing, re-derive when cleared (existing v2.5 behavior — keep).
- Payload slider: value = cubic curve, `MB = round((pct/100)^3 × 102400)`; display GB ≥ 1024 MB.
- Toggle switch: 38×20px, 1px ink border, square; ON = accent fill, knob right (background-color); OFF = background fill, ink knob left.
- Hover states: frames/list rows get a 4–7% ink tint; chips get an ink border.

## State Management
No new model state — everything maps to the existing `ConfigModel`. New view-model state only: `SelectedFrameIndex`, `OpenDataEditorKey` + `SelectedEntryIndex`, `GenerateDialogOpen`. Receipt values, frame summaries and chip states are computed projections of `ConfigModel` (round-trip via ConfigWriter/ConfigParser unchanged).

## Design Tokens (prototype skin — swappable)
- Background #F3F2F2, surface #EAE9E9, ink #201E1D, accent #EC3013, accent-100 ≈ #FBD9D2 (tint), accent-800 ≈ #7A2113 (text on tint), divider = ink at 40%.
- Type: Archivo; headings weight 800; body 13–15px; labels 10–12px letter-spaced caps; paths/EXE/GUID in monospace.
- Radius 0 everywhere; rules 2px (major) / 1px (rows); "card" emphasis = 1px ink border + 3px offset hard shadow.
- Spacing scale: 4 / 8 / 12 / 16 / 24 / 32.

## Assets
None — no imagery or icons beyond text glyphs (↓, →, ✕, ▪). Lucide icons are the sanctioned set if icons are added.

## Files
- `Storyboard.dc.html` — the clickable prototype (open in a browser; template markup + a logic class holding all interaction state).
- `styles.css` — the Modernist token sheet + component classes the prototype references.

## Composite-field editor — add/edit flow (spec)
See `screenshots/04-data-editor-registry.png` (row selected) and `05-data-editor-add-entry.png` (just after "+ Add entry"). The exact flow to implement, using Registry entries as the reference case (identical for files, env vars, file associations, firewall, protocols, context menu — only the field schema changes):

1. **Open**: "Edit data →" on an enabled behavior row opens the overlay (860px, right-anchored, 35% scrim). First entry is pre-selected.
2. **Select**: clicking a master-list row selects it (accent-100 fill + 3px accent left border) and loads its fields into the detail form on the right. The master row shows field 1 (monospace, bold) with the remaining fields joined by " · " underneath.
3. **Edit**: typing in any detail-form textbox writes through to the selected entry on commit (LostFocus / Enter — WPF UpdateSourceTrigger=LostFocus is fine). The master-list row text updates immediately; so do the Frame 03 detail line and the Receipt counts.
4. **Add**: "+ Add entry" appends a blank entry, selects it, and focuses the first detail field. A blank entry renders in the master list as an empty highlighted row (see 05) until fields are filled.
5. **Remove**: "Remove entry" deletes the selected entry and selects the previous one (or none if the list is empty).
6. **Close**: "Close ✕" or clicking the scrim dismisses the overlay. No explicit Save — edits are live against the view-model (same as the rest of the app; Save Config persists to .ini as today).
7. **Validation** (recommended, not in prototype): per-field inline hints — Key path must start with HKCU\ / HKLM\ / HKCR\; Type is a dropdown (REG_SZ, REG_DWORD, REG_EXPAND_SZ, REG_MULTI_SZ); Port numeric; Extension must start with ".". Never expose the pipe format in the UI.

## Presets (definitions)
Shipped as one .ini per preset in `presets/` — written against indicative key names; map them onto the real ConfigModel/ConfigWriter keys. Summary:

| Preset | Behaviors ON | Sample data | Context |
| --- | --- | --- | --- |
| Blank | none | — | unchanged |
| Typical desktop app | Files, Registry, Shortcuts, File associations, App Paths | 3 files, 3 registry values (2 HKCU + 1 HKLM), desktop + Start Menu shortcuts, .tpkg/.tpkx | unchanged (per-user default) |
| Service + firewall | Files, Registry, Shortcuts, Windows service, Firewall | 2 files, 2 registry values, TestPackageSvc (Manual), TCP 19876 in / 19877 out | suggest per-machine |
| Enterprise | Files, Registry, Shortcuts, Service (Automatic), Scheduled task (Daily), Active Setup, App Paths, Env vars | full file/registry set, TESTPKG_HOME + PATH append | suggest per-machine |

Preset semantics: presets **replace the install-action behavior set and its sample data**; they do not touch Identity, Wizard pages, or Uninstall settings. Applying a preset with unsaved edits should be undoable or confirmed. Placeholders `<Publisher>`, `<AppName>`, `<AppExe>`, `<Version>` resolve from current Identity values at apply time (keeps the derived-defaults behavior consistent).

## Screenshots
- `screenshots/01-identity.png` — Frame 01 Identity editor
- `screenshots/02-wizard.png` — Frame 02 Wizard (page order, defaults, EULA, appearance)
- `screenshots/03-install-actions.png` — Frame 03 Install actions rows
- `screenshots/04-data-editor-registry.png` — Composite-field editor open on Registry entries (row selected)
- `screenshots/05-data-editor-add-entry.png` — Same editor immediately after "+ Add entry" (blank entry selected)
- `screenshots/06-first-run.png` — Frame 04 First run
- `screenshots/07-uninstall.png` — Frame 05 Uninstall
- `screenshots/08-generate-dialog.png` — Generate confirmation dialog
