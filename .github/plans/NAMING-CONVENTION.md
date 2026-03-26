# Konvence názvů plánů

Tento dokument popisuje pravidla pro pojmenování.

---

## Formát názvu souboru

```
.github/plans/PLAN-YYYY-MM-DD-<slug>.md
```

### Části názvu

| Část | Popis | Příklad |
|------|-------|---------|
| `PLAN-` | Pevný prefix – označuje, že jde o plán | `PLAN-` |
| `YYYY-MM-DD` | Datum vytvoření plánu ve formátu ISO 8601 | `2026-02-20` |
| `<slug>` | Krátký popis obsahu plánu | `add-auth-module` |
| `.md` | Přípona Markdown | `.md` |

---

## Pravidla pro `<slug>`

- Pouze malá písmena ASCII: `a–z`, číslice `0–9` a pomlčka `-`
- Vzor: `[a-z0-9-]+`
- Slova odděluj pomlčkou `-` (ne podtržítkem `_` ani mezerou)
- Bez diakritiky – převeď česká písmena na ASCII ekvivalenty:
  - á → a, č → c, ď → d, é → e, ě → e, í → i, ň → n, ó → o, ř → r, š → s, ť → t, ú/ů → u, ý → y, ž → z
- Délka slugu: 3–50 znaků
- Nepoužívej obecné slugy jako `zmena`, `uprava`, `fix` – buď konkrétní

### Příklady správných názvů

```
.github/plans/PLAN-2026-02-20-add-controlled-workflow.md
.github/plans/PLAN-2026-03-01-refactor-db-connection.md
.github/plans/PLAN-2026-04-15-migrate-winforms-to-wpf.md
.github/plans/PLAN-2026-05-10-add-unit-tests-repository-layer.md
```

### Příklady špatných názvů

```
.github/plans/plan-nova-funkce.md          ← chybí datum, chybí PLAN- prefix
.github/plans/PLAN-2026-02-20-Nová věc.md  ← diakritika a mezery ve slugu
.github/plans/PLAN-2026-02-20-zmena.md     ← příliš obecný slug
.github/plans/PLAN-20260220-auth.md        ← špatný formát data
```

---

## Umístění souborů

| Stav plánu | Umístění |
|------------|----------|
| Aktivní (návrh, schváleno, v realizaci) | `.github/plans/` |

### Soubory v `plans/`

- `_template.md` – šablona pro nové plány (není to plán, nepřesouvej ho)
- `NAMING-CONVENTION.md` – tento soubor (není to plán, nepřesouvej ho)
- `PLAN-*.md` – jednotlivé plány

---

## Šablona plánu

Nové plány vytvárej vždy podle šablony `.github/plans/_template.md`.
