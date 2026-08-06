# Pipeline Runner CSV output

How **Pipeline.Runner** writes CSV files after a run.

---

## Overview

Pipeline Runner orchestrates parsing, denormalization, and optional time normalization. Historical weather HTML files are parsed, grouped by place, and written under `HtmlLog_<timestamp>/parsed/`. Denormalization always runs next and writes wide-format CSVs at the `parsed/` stage root; if it produces no place files, the run fails with an error. When `RunTimeNormalization` is enabled (default), observation-time normalization writes under `HtmlLog_<timestamp>/time-normalized/`.

Each place gets its own CSV file. Narrow CSVs store weather conditions as English labels in a single column. Three manifest files at the parsed stage root record places, weather flags, and which source HTML file won for each `(place, date)` pair.

The run folder `HtmlLog_<yyyy-MM-dd_HH-mm-ss>/` is created under the **process current working directory** (not under `HistoricalWeatherFilesRoot`).

**Terminology:** `normalized-columns/` means the **narrow** CSV column shape (single `Weather Characteristics` cell). It is unrelated to the `time-normalized/` stage name. Both stages can contain their own `normalized-columns/` tree.

---

## Output layout

```
HtmlLog_<timestamp>/                 # under process CWD
  parsed/
    log<timestamp>.log               # parsing stage log
    log-denorm<timestamp>.log        # denormalization stage log
    result<timestamp>.html           # parsing HTML report
    log-compare<timestamp>.log       # optional; when RunHtmlLogCsvComparison is true
    parsed-source-files.csv          # (place, date) → winning source HTML path
    parsed-places.csv                # places seen in this run
    weather-characteristics.csv      # weather flags seen in this run
    Kyiv.csv                         # wide format; see Per-place weather CSVs
    Kharkiv.csv
    ...
    normalized-columns/              # narrow format
      Kyiv.csv
      Kharkiv.csv
      ...
  time-normalized/                   # only when RunTimeNormalization is true
    log<timestamp>.log
    result<timestamp>.html
    Kyiv.csv                         # wide format
    Kharkiv.csv
    ...
    normalized-columns/              # narrow format
      Kyiv.csv
      Kharkiv.csv
      ...
```

- **`parsed/`** — parsing and denormalization stage logs/reports, manifests, narrow per-place CSVs in `normalized-columns/`, and wide-format denormalized CSVs at the stage root.
- **`time-normalized/`** — time normalization stage logs/reports, narrow per-place CSVs in `normalized-columns/`, and wide-format CSVs at the stage root. Created only when `RunTimeNormalization` is `true`.

Both `normalized-columns/` trees use the same narrow CSV shape (`NormalizedWeatherCsvColumns.CoreColumns`) and naming rules. The place name is **not** repeated inside those files — read it from the filename. **Wide** CSVs at both stage roots include a leading `Place` column.

---

## Per-place weather CSVs

### Filenames

One file per place. The filename is the **English display name** plus `.csv`:

- `Kyiv.csv`
- `Kharkiv.csv`
- `Chervona Zirka.csv`
- `Ivano-Frankivsk.csv`

Invalid filesystem characters in the name are replaced with `_`.

### Narrow format (`normalized-columns/`)

Files under both `parsed/normalized-columns/` and `time-normalized/normalized-columns/` share the same shape. The place name is **not** repeated inside the file — read it from the filename.

| Column | Description |
|--------|-------------|
| `DateTime` | Observation time (`yyyy-MM-dd HH:mm`) |
| `Temperature` | Integer, °C |
| `WindDirection` | Integer, ° (azimuth 0–359) |
| `WindSpeed` | Decimal, m/s |
| `AtmosphericPressure` | Integer, mmHg |
| `Humidity` | Integer, % |
| `Weather Characteristics` | Active conditions as English labels; see [Weather characteristics column](#weather-characteristics-column) |

Example — file: `parsed/normalized-columns/Kyiv.csv` or `time-normalized/normalized-columns/Kyiv.csv`

```csv
DateTime,Temperature,WindDirection,WindSpeed,AtmosphericPressure,Humidity,Weather Characteristics
2003-01-01 00:00,-12,315,2.0,750,70,Clear
2003-01-01 06:00,0,90,3.0,755,65,"Clear, Rain"
```

### Wide format (stage root)

Wide CSVs at `parsed/{Place}.csv` and `time-normalized/{Place}.csv` lead with `Place`, then the six scalar columns and one column per weather flag.

| Column | Description |
|--------|-------------|
| `Place` | English display name (same as the `.csv` filename without extension; not the original NameInHtml) |
| `DateTime` | Observation time (`yyyy-MM-dd HH:mm`) |
| `Temperature` | Integer, °C |
| `WindDirection` | Integer, ° (azimuth 0–359) |
| `WindSpeed` | Decimal, m/s |
| `AtmosphericPressure` | Integer, mmHg |
| `Humidity` | Integer, % |
| *(flag columns)* | One column per possible weather characteristic; `1` or `0` |

Example — file: `parsed/Kyiv.csv` or `time-normalized/Kyiv.csv`

```csv
Place,DateTime,Temperature,WindDirection,WindSpeed,AtmosphericPressure,Humidity,Clear,...
Kyiv,2003-01-01 00:00,-12,315,2.0,750,70,1,...
```

Rows are ordered by `DateTime` within each place file.

---

## Denormalized weather characteristics

Pipeline.Runner always runs [`Pipeline.Denormalizer`](../src/Pipeline.Denormalizer/DenormalizingPipeline.cs) **immediately after** parsing:

- Reads `parsed/normalized-columns/*.csv`, writes `parsed/*.csv` (stage root)

If denormalization writes **zero** place files, it throws and the run fails.

When `RunTimeNormalization` is `true` (default), [`Pipeline.TimeNormalizer`](../src/Pipeline.TimeNormalizer/TimeNormalizingPipeline.cs) reads wide CSVs from the `parsed/` stage root, applies observation-time normalization, and writes:

- `time-normalized/normalized-columns/*.csv` (narrow format)
- `time-normalized/*.csv` (wide format, stage root)

Set `RunTimeNormalization` to `false` in `appsettings.json` to skip the time normalization stage entirely.

Each denormalized file keeps the six scalar columns (`DateTime`, `Temperature` (°C), `WindDirection` (°), `WindSpeed` (m/s), `AtmosphericPressure` (mmHg), `Humidity` (%)) and replaces the single `"Weather Characteristics"` column with **one column per possible weather flag** (English display name, sorted alphabetically, case-insensitive). Cell values are `1` when that flag is set on the row, otherwise `0`.

Wide headers always include the **full** `WeatherCharacteristics` catalog except `None` (see [`WeatherCharacteristicsColumns`](../src/Pipeline.Core/Csv/Metadata/WeatherCharacteristicsColumns.cs)) — not only flags observed in the run. By contrast, `weather-characteristics.csv` lists only flags that actually occurred.

**Wide** CSVs at both the `parsed/` and `time-normalized/` stage roots include a leading `Place` column (English display name from the filename, repeated on every row; not the original NameInHtml).

Neither `parsed/normalized-columns/` nor `time-normalized/normalized-columns/` include `Place`.

---

## Weather characteristics column

Each observation can have zero or more weather conditions. They are stored in one cell as **English labels**, separated by `, ` (comma + space), sorted alphabetically (case-insensitive).

Examples:

| Flags in data | Cell value |
|---------------|------------|
| Clear only | `Clear` |
| Clear and Rain | `Clear, Rain` |
| None | *(empty cell)* |

Labels come from the `WeatherCharacteristics` enum via `EnumDisplayNameFormatter` (PascalCase splitting). The NameInHtml strings in the HTML are converted to these flags first; the CSV always shows the English form.

---

## Manifest files

Written to the **parsed stage root** (alongside wide denormalized CSVs and the `normalized-columns/` folder).

`parsed-places.csv` and `weather-characteristics.csv` share the same columns:

| Column | Description |
|--------|-------------|
| `EnglishName` | English label used in filenames and CSV cells |
| `NameInHtml` | Original HTML string as it appears in the source |

### `parsed-source-files.csv`

Maps each `(place, date)` in parsed output to the source HTML file that contributed the data. One row per pair, sorted by `Place` then `Date`.

| Column | Description |
|--------|-------------|
| `Place` | English place name (same as per-place CSV filenames, without `.csv`) |
| `Date` | Archive date (`yyyy-MM-dd`) |
| `SourceFilePath` | Winning HTML path for duplicate `(place, date)` resolution. Shape depends on the run source: host absolute path for a directory root; archive-relative entry key (e.g. `Kyiv/2003-01-01.html`) when `HistoricalWeatherFilesRoot` is a `.7z` (the opened archive is its own read-only file system) |

Downstream consumers must not assume `SourceFilePath` is always a host absolute path. When multiple HTML files map to the same place and date, only the lexicographically last file path is kept (same rule as under "Folder path self-check and aliases"). The normalizer reads this manifest to attribute normalization errors and log messages to the correct source file; if the file is absent, it falls back to `{place}/{yyyy-MM-dd}`.

### `parsed-places.csv`

Lists every place that appeared in successfully parsed files for this run. One row per distinct English place name, sorted A–Z.

Use it to look up which HTML wording maps to which English filename.

### `weather-characteristics.csv`

Lists every weather characteristic **that actually occurred** in the parsed data for this run — not the full catalog of possible values. One row per observed flag, sorted by `EnglishName`.

Use it to see which original NameInHtml terms were seen and how they are labeled in English output.

---

## Place names

### How a place is resolved

1. The HTML parser reads the city name from the page title (Cyrillic, prepositional form — e.g. `Киеве`, `Червоной`).
2. `PlaceConverter` maps that string to a `Place` enum value (all members of the `Place` enum; currently 19 locations).
3. `PlaceConverter` / `EnumDisplayNameFormatter` turn the enum into the English display name used for grouping and filenames (e.g. `Киеве` → `Kyiv`).

All rows for the same place are grouped under one English name, regardless of how many HTML files contributed.

### Folder path self-check and aliases

During parsing, Pipeline.Parser compares the Cyrillic city name from the HTML with the place inferred from **directory segments** in the file path (not the filename). `PlaceConverter.TryFromFilePath` scans path segments from the leaf directory upward and matches against English aliases built at startup from:

- the `Place` enum member name (e.g. `Kyiv`);
- the English display name from `EnumDisplayNameAttribute` / `EnumDisplayNameFormatter` (when it differs from the enum name);
- any extra folder names declared on `AlternateNameAttribute` (e.g. folder `Kiev` for display name `Kyiv`).

If a known alias is found in the path but does not match the HTML city, the file is rejected and counted as a path/place mismatch.

**Note:** a file whose path contains **no known place alias** is also rejected and counted as a path/place mismatch, even when the HTML names a known place. Files under arbitrary folders are never grouped by their HTML city alone — this is stricter than earlier versions of the pipeline, which accepted such files. Keep source files under a folder named after a known place alias.

Duplicate `(place, date)` entries are resolved deterministically: when several files map to the same place and date, the lexicographically last file path wins.

### Unknown or unmapped places

The Cyrillic name **must** match an entry in `PlaceConverter`. If it is missing, empty, or not in the table, the run **stops** with an error. It does not write CSVs for other places in that batch.

To support a new location, add a `Place` enum member with the appropriate `NameInHtml` / `EnumDisplayName` / `AlternateName` attributes in Pipeline.Core (and HtmlScrapper options if you scrape it). `PlaceConverter` builds its maps from those attributes at startup — no converter code changes are required. Then re-run.

### Parse failures vs place failures

- **HTML parse error on one file** — that file is skipped and counted as unsuccessful; the run continues with the rest.
- **Unmapped place name** — fails when results are grouped, after parsing; the whole run aborts.

---

## Encoding and write behavior

| Behavior | Detail |
|----------|--------|
| Encoding | UTF-8 with BOM |
| Delimiter | Comma (CsvHelper defaults; fields quoted when needed) |
| Culture | `InvariantCulture` for numbers and datetimes |
| DateTime format | `yyyy-MM-dd HH:mm` |
| Overwrite | Existing files at the same path are replaced (`FileMode.Create`) |

---

## HtmlLog CSV comparison

See **[htmllog-csv-comparer.md](htmllog-csv-comparer.md)** for pair/chain CLI, matching rules, ZIP layout, PARTLY EQUAL semantics, verbose JSON, and exit behavior.

---

## Related code

| Piece | Project | Role |
|-------|---------|------|
| `PlaceConverter` | Pipeline.Core | Cyrillic HTML name → `Place` enum |
| `EnumDisplayNameFormatter` | Pipeline.Core | `Place` / `WeatherCharacteristics` → English display label |
| `WeatherCharacteristicConverter` | Pipeline.Core | NameInHtml strings ↔ flags; builds the English CSV cell |
| `WeatherScalarCsvColumns` | Pipeline.Core | Scalar column header names and DateTime format |
| `NormalizedWeatherCsvColumns` | Pipeline.Core | Narrow `CoreColumns` including `Weather Characteristics` |
| `WeatherCharacteristicsColumns` | Pipeline.Core | Wide one-hot flag column names (full catalog except `None`) |
| `WeatherCsvColumns` | Pipeline.Core | Facade re-exporting the column constants above |
| `NormalizedColumnsWeatherDataCsvWriter` | Pipeline.Core | Writes narrow per-place CSVs under `normalized-columns/` (parsed and time-normalized stages) |
| `ParsedStageManifestCsvWriter` | Pipeline.Parser | Writes `parsed-places.csv` and `weather-characteristics.csv` |
| `ParsedSourceFilesManifestWriter` | Pipeline.Parser | Writes `parsed-source-files.csv` |
| `ParsedSourceFilesManifestReader` | Pipeline.TimeNormalizer | Reads `parsed-source-files.csv` for normalization context |
| `NormalizedColumnsWeatherDataCsvReader` | Pipeline.Core | Reads narrow-format CSVs from a `normalized-columns/` directory |
| `DenormalizedWeatherDataCsvReader` | Pipeline.Core | Reads wide-format CSVs from the `parsed/` stage root for normalization |
| `DenormalizedWeatherDataCsvWriter` | Pipeline.Core | Writes wide-format denormalized per-place CSVs |
| `DenormalizingPipeline` | Pipeline.Denormalizer | Reads `parsed/normalized-columns/`, writes wide CSVs at `parsed/` root |

Unit tests live in `tests/Pipeline.Core.Tests` (CSV readers/writers and shared helpers), `tests/Pipeline.Parser.Tests`, `tests/Pipeline.Denormalizer.Tests`, and `tests/Pipeline.TimeNormalizer.Tests`.
