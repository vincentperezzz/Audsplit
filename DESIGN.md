# DESIGN.md

## Theme
Dark navy tool surface. Brand accents from cyan → blue → purple (logo-matched). Restrained color strategy: neutrals dominate; gradient accents on logo, active borders, primary chips.

## Palette (OKLCH-ish targets → WPF hex)
- `--bg`: `#070B14`
- `--surface`: `#0E1524`
- `--surface-2`: `#151D2E`
- `--ink`: `#F4F7FF`
- `--muted`: `#8B95A8`
- `--cyan`: `#22D3EE`
- `--blue`: `#3B82F6`
- `--purple`: `#A855F7`
- `--border`: `#2A3550`
- `--row-hover`: `#182033`
- `--danger-soft`: `#F87171`

## Typography
Segoe UI Variable / Segoe UI. Wordmark: SemiBold Italic 16–18. Body 13. Meta 11 muted. No display fonts in controls.

## Components
- Flyout shell: 14px radius, soft navy gradient fill, bright top-edge highlight, soft drop shadow.
- Header: logo 28 + AUDSPLIT + tagline; **Reset all** ghost + **Refresh** cyan-tint primary; cyan→blue→purple rule.
- Segment: Apps | Speakers (`UniformGrid`). Active pill = surface + soft cyan border + cyan label. Press scale 0.97.
- App row: `[36 icon | 12 | * name | 12 | 188 combo]`, padding 12×10, gap 8, min-height 52.
- Speaker row: `[name | 12 | 44% cyan | 8 | 34 mute]` + brand-gradient slider fill; mute danger soft when on.
- Empty: MDL2 speaker glyph + “Nothing playing” + one muted line.
- Status: hairline above muted status line.
- ComboBox: dark chrome, cyan hover/open, chevron rotates open, press scale.

## Motion
- Button/mute/segment press: 120ms ease-out scale 0.97 (0.95 mute).
- Page switch: 120ms fade-out / 180ms fade-in ease-out. Skip when `SystemParameters.ClientAreaAnimation` is off.
- Slider thumb: 1.15 scale while dragging.
