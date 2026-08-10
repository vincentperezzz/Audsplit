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
- Flyout shell: 12px radius, 1px border, soft navy fill.
- Header: logo + AUDSPLIT + tagline on the left; **Refresh** + **Reset all** compact pills on the right; cyan→purple rule under brand.
- App row: `[icon 32] name+process … [device ComboBox ~200]`.
- Status line under the list.
- ComboBox: fully templated dark chrome (`Surface2` + ink text), cyan hover/open border, dark popup list.

## Motion
150–200ms opacity/hover. Respect prefers-reduced-motion via instant fallbacks where applicable.
