# Pipeline Runner CSV output

How **Pipeline.Runner** writes CSV files after a run. For configuration, stage flags, and how to run the pipeline, see [Pipeline Runner](pipeline-runner.md).

## Contents

- [Overview](#overview)
- [Output layout](#output-layout)
- [Per-place weather CSVs](#per-place-weather-csvs)
  - [Filenames](#filenames)
  - [Narrow format (`narrow-format/`)](#narrow-format-narrow-format)
  - [Wide format (`wide-format/`)](#wide-format-wide-format)
- [Wide-format weather characteristics](#wide-format-weather-characteristics)
- [Weather characteristics column](#weather-characteristics-column)
- [Manifest files](#manifest-files)
  - [`parsed-source-files.csv`](#parsed-source-filescsv)
  - [`parsed-places.csv`](#parsed-placescsv)
  - [`weather-characteristics.csv`](#weather-characteristicscsv)
  - [`weather-characteristics-usage.csv`](#weather-characteristics-usagecsv)
- [Place names](#place-names)
  - [How a place is resolved](#how-a-place-is-resolved)
  - [Folder path self-check and aliases](#folder-path-self-check-and-aliases)
  - [Unknown or unmapped places](#unknown-or-unmapped-places)
  - [Parse failures vs place failures](#parse-failures-vs-place-failures)
- [Encoding and write behavior](#encoding-and-write-behavior)
- [HtmlLog CSV comparison](#htmllog-csv-comparison)
- [Reference catalogs](#reference-catalogs)
  - [Supported places](#supported-places)
  - [Supported weather characteristics](#supported-weather-characteristics)
- [Related code](#related-code)

---

## Overview

Historical weather HTML files are parsed, grouped by place, and written under `HtmlLog_<timestamp>/parsed/`. After optional parsed-stage analysis, denormalization writes wide-format CSVs under `parsed/wide-format/`; if it produces no place files, the run fails with an error. When `RunTimeNormalization` is enabled (default), observation-time normalization writes under `HtmlLog_<timestamp>/time-normalized/`.

Each place gets its own CSV file. Narrow CSVs store weather conditions as English labels in a single column. Three manifest files at the parsed stage root record places, weather flags, and which source HTML file won for each `(place, date)` pair. When `RunAnalysis` is enabled (default), each analyzed stage also gets `weather-characteristics-usage.csv` and `result-analysis{timestamp}.html`. Stage text logs and HTML reports are described in [Pipeline Runner](pipeline-runner.md#stages-and-flags).

The run folder `HtmlLog_<yyyy-MM-dd_HH-mm-ss>/` is created under the **process current working directory** (not under `HistoricalWeatherFilesRoot`).

**Terminology:** `narrow-format/` means the **narrow** CSV column shape (single `Weather Characteristics` cell). It is unrelated to the `time-normalized/` stage name. Both stages can contain their own `narrow-format/` and `wide-format/` trees.

---

## Output layout

```
HtmlLog_<timestamp>/                 # under process CWD
  parsed/
    log<timestamp>.log               # text log for parse, denorm, parsed analysis, compare
    result<timestamp>.html           # parsing HTML report
    result-analysis<timestamp>.html  # parsed-stage analysis report
    parsed-source-files.csv          # (place, date) → winning source HTML path
    parsed-places.csv                # places seen in this run
    weather-characteristics.csv      # weather flags seen in this run
    weather-characteristics-usage.csv  # flag counts/% over all place rows (when analysis enabled)
    narrow-format/                   # narrow format
      Kyiv.csv
      Kharkiv.csv
      ...
    wide-format/                     # wide format
      Kyiv.csv
      Kharkiv.csv
      ...
  time-normalized/                   # only when RunTimeNormalization is true
    log<timestamp>.log               # text log for time-norm + time-norm analysis
    result<timestamp>.html           # time-normalization HTML report
    result-analysis<timestamp>.html  # time-normalized analysis report
    weather-characteristics-usage.csv  # same analysis over time-normalized rows
    narrow-format/                   # narrow format
      Kyiv.csv
      Kharkiv.csv
      ...
    wide-format/                     # wide format
      Kyiv.csv
      Kharkiv.csv
      ...
```

- **`parsed/`** — stage text log, parsing and analysis HTML reports, manifests, narrow per-place CSVs in `narrow-format/`, and wide-format CSVs in `wide-format/`.
- **`time-normalized/`** — stage text log, time-normalization and analysis HTML reports, narrow per-place CSVs in `narrow-format/`, and wide-format CSVs in `wide-format/`. Created only when `RunTimeNormalization` is `true`.
- **`weather-characteristics-usage.csv`** — written by weather-characteristics analysis (default on via `RunAnalysis`) under each analyzed stage root. One row per known flag with `EnglishName`, `NameInHtml`, `RowCount`, and `PercentOfRows` (counts across all `{stage}/narrow-format/*.csv` rows). Analysis writes `result-analysis{timestamp}.html` in that stage directory (footer once) and appends its text output to that stage’s text log.

Both `narrow-format/` trees use the same narrow CSV shape (`NarrowFormatWeatherCsvColumns.CoreColumns`) and naming rules. The place name is **not** repeated inside those files — read it from the filename. **Wide** CSVs under both `wide-format/` directories include a leading `Place` column.

---

## Per-place weather CSVs

### Filenames

One file per place. The filename is the **English display name** plus `.csv`. Full list: [Supported places](#supported-places).

Invalid filesystem characters in the name are replaced with `_`.

### Narrow format (`narrow-format/`)

Files under both `parsed/narrow-format/` and `time-normalized/narrow-format/` share the same shape. The place name is **not** repeated inside the file — read it from the filename.

| Column | Description |
|--------|-------------|
| `DateTime` | Observation time (`yyyy-MM-dd HH:mm`) |
| `Temperature` | Integer, °C |
| `WindDirection` | Integer, ° (azimuth 0–359) |
| `WindSpeed` | Decimal, m/s |
| `AtmosphericPressure` | Integer, mmHg |
| `Humidity` | Integer, % |
| `Weather Characteristics` | Active conditions as English labels; see [Weather characteristics column](#weather-characteristics-column) |

Example — file: `parsed/narrow-format/Kyiv.csv` or `time-normalized/narrow-format/Kyiv.csv`

```csv
DateTime,Temperature,WindDirection,WindSpeed,AtmosphericPressure,Humidity,Weather Characteristics
2003-01-01 00:00,-12,315,2.0,750,70,Clear
2003-01-01 06:00,0,90,3.0,755,65,"Clear, Rain"
```

### Wide format (`wide-format/`)

Wide CSVs under `parsed/wide-format/` and `time-normalized/wide-format/` lead with `Place`, then the six scalar columns and one column per weather flag.

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

Example — file: `parsed/wide-format/Kyiv.csv` or `time-normalized/wide-format/Kyiv.csv`

```csv
Place,DateTime,Temperature,WindDirection,WindSpeed,AtmosphericPressure,Humidity,Clear,...
Kyiv,2003-01-01 00:00,-12,315,2.0,750,70,1,...
```

Rows from multiple source HTML files for the same place are merged into one stream and sorted by `DateTime` only (not by source file or archive date).

---

## Wide-format weather characteristics

After optional parsed-stage analysis, Pipeline.Runner always runs [`Pipeline.Denormalizer`](../src/Pipeline.Denormalizer/DenormalizingPipeline.cs):

- Reads `parsed/narrow-format/*.csv`, writes `parsed/wide-format/*.csv`

If denormalization writes **zero** place files, it throws and the run fails.

When `RunTimeNormalization` is `true` (default), [`Pipeline.TimeNormalizer`](../src/Pipeline.TimeNormalizer/TimeNormalizingPipeline.cs) reads wide CSVs from `parsed/wide-format/`, applies observation-time normalization, and writes:

- `time-normalized/narrow-format/*.csv` (narrow format)
- `time-normalized/wide-format/*.csv` (wide format)

Set `RunTimeNormalization` to `false` to skip the time normalization stage entirely (see [Pipeline Runner](pipeline-runner.md#stages-and-flags)).

Each wide-format file keeps the six scalar columns (`DateTime`, `Temperature` (°C), `WindDirection` (°), `WindSpeed` (m/s), `AtmosphericPressure` (mmHg), `Humidity` (%)) and replaces the single `"Weather Characteristics"` column with **one column per possible weather flag** (English display name, sorted alphabetically, case-insensitive). Cell values are `1` when that flag is set on the row, otherwise `0`.

Wide headers always include the **full** [Supported weather characteristics](#supported-weather-characteristics) catalog (see also [`WeatherCharacteristicsColumns`](../src/Pipeline.Core/Csv/Metadata/WeatherCharacteristicsColumns.cs)) — not only flags observed in the run. By contrast, `weather-characteristics.csv` lists only flags that actually occurred in that run.

**Wide** CSVs under both `wide-format/` directories include a leading `Place` column (English display name from the filename, repeated on every row; not the original NameInHtml).

Neither `parsed/narrow-format/` nor `time-normalized/narrow-format/` include `Place`.

---

## Weather characteristics column

Each observation can have zero or more weather conditions. They are stored in one cell as **English labels**, separated by `, ` (comma + space), sorted alphabetically (case-insensitive).

Examples:

| Flags in data | Cell value |
|---------------|------------|
| Clear only | `Clear` |
| Clear and Rain | `Clear, Rain` |
| None | *(empty cell)* |

Labels come from the `WeatherCharacteristics` enum via `EnumDisplayNameFormatter` (PascalCase splitting). The NameInHtml strings in the HTML are converted to these flags first; the CSV always shows the English form. Full catalog: [Supported weather characteristics](#supported-weather-characteristics). Narrow cells list comma-separated **EnglishName** values from that table; composite HTML strings (e.g. `дождь с грозой`) map to a single flag, not multiple source terms.

---

## Manifest files

Written to the **parsed stage root** (alongside the `narrow-format/` and `wide-format/` folders).

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

Lists every weather characteristic **that actually occurred** in the parsed data for this run — not the full catalog of possible values. One row per observed flag, sorted by `EnglishName`. For the complete static catalog, see [Supported weather characteristics](#supported-weather-characteristics).

Use it to see which original NameInHtml terms were seen and how they are labeled in English output.

### `weather-characteristics-usage.csv`

Written by [`Pipeline.Analysis`](../src/Pipeline.Analysis/) when `RunAnalysis` is `true` (default). Present under each analyzed stage root (`parsed/` always; `time-normalized/` when that stage ran). When analysis runs, a missing or empty `{stage}/narrow-format/` **aborts the run** (same hard-fail policy for parsed and time-normalized).

Unlike `weather-characteristics.csv`, this file lists the **full catalog** of known flags (all entries in [Supported weather characteristics](#supported-weather-characteristics), even when `RowCount` is 0) with occurrence counts over all `{stage}/narrow-format/*.csv` data rows:

| Column | Description |
|--------|-------------|
| `EnglishName` | English display name |
| `NameInHtml` | Original HTML string |
| `RowCount` | Number of data rows where the flag bit is set |
| `PercentOfRows` | `RowCount / totalDataRows * 100`, formatted to five decimal places with `%` |

Rows are sorted by `PercentOfRows` descending, then `EnglishName`. Percentages may sum above 100% when rows carry multiple flags. Analysis appends to that stage’s text log and writes the same table to `result-analysis{timestamp}.html` in that stage directory.

---

## Place names

### How a place is resolved

1. The HTML parser reads the city name from the page title (Cyrillic, prepositional form — e.g. `Киеве`, `Червоной`).
2. `PlaceConverter` maps that string to a `Place` enum value (see [Supported places](#supported-places)).
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

See **[htmllog-csv-comparer.md](htmllog-csv-comparer.md)** for pair/chain CLI, matching rules, ZIP layout, PARTLY EQUAL semantics, verbose JSON, and exit behavior. Pipeline integration via `RunHtmlLogCsvComparison` is covered in [Pipeline Runner](pipeline-runner.md#post-run-htmllog-comparison).

---

## Reference catalogs

Both tables mirror the fixed `Place` and `WeatherCharacteristics` enums in Pipeline.Core. Manifest `EnglishName` values, narrow CSV cells, and wide CSV column headers use these English labels. `WeatherCharacteristics.None` is not a column; an empty narrow cell means no flags are set.

When adding a new enum member, update the matching table here (same discipline as `PlaceTestData` and weather-characteristic converter tests).

### Supported places

All `Place` enum members (19 locations). **Folder aliases** are path segments accepted by `PlaceConverter.TryFromFilePath` (enum member name, English display name when it differs, and `AlternateName` values).

| EnglishName | Example filename | Folder aliases |
|-------------|------------------|----------------|
| Chervona Zirka | `Chervona Zirka.csv` | `ChervonaZirka`, `Chervona Zirka`, `Chervonaya-Zirka` |
| Donetsk | `Donetsk.csv` | `Donetsk` |
| Hoverla | `Hoverla.csv` | `Hoverla`, `Goverla` |
| Hremiach | `Hremiach.csv` | `Hremiach`, `Gremyach`, `Hremyach` |
| Ivano-Frankivsk | `Ivano-Frankivsk.csv` | `IvanoFrankivsk`, `Ivano-Frankivsk`, `Ivano-Frankovsk` |
| Kharkiv | `Kharkiv.csv` | `Kharkiv`, `Harkov` |
| Kuyalnyk | `Kuyalnyk.csv` | `Kuyalnyk`, `Kuyalnik` |
| Kyiv | `Kyiv.csv` | `Kyiv`, `Kiev` |
| Luhansk | `Luhansk.csv` | `Luhansk`, `Lugansk` |
| Lviv | `Lviv.csv` | `Lviv`, `Lvov` |
| Mariupol | `Mariupol.csv` | `Mariupol` |
| Odesa | `Odesa.csv` | `Odesa`, `Odessa` |
| Sevastopol | `Sevastopol.csv` | `Sevastopol` |
| Simferopol | `Simferopol.csv` | `Simferopol` |
| Sloviansk | `Sloviansk.csv` | `Sloviansk`, `Slavyansk` |
| Solomonove | `Solomonove.csv` | `Solomonove`, `Solomonovo` |
| Ternopil | `Ternopil.csv` | `Ternopil`, `Ternopol` |
| Uzhhorod | `Uzhhorod.csv` | `Uzhhorod`, `Ujgorod` |
| Zhytomyr | `Zhytomyr.csv` | `Zhytomyr`, `Jitomir` |

### Supported weather characteristics

All `WeatherCharacteristics` enum members except `None` (53 flags). Sorted A–Z by English display name — the same labels used in narrow CSV cells, manifest `EnglishName` values, and wide CSV flag columns.

| EnglishName |
|-------------|
| Black ice |
| Clear |
| Drizzle |
| Dust |
| Dust storm |
| Few clouds |
| Fog |
| Freezing rain |
| Ground blizzard |
| Hail |
| Haze |
| Heavy blizzard |
| Heavy hail |
| Heavy rain |
| Heavy rain with snow |
| Heavy shower rain |
| Heavy shower rain with snow |
| Heavy snow |
| Heavy snow pellets |
| Light blizzard |
| Light drizzle |
| Light dust storm |
| Light fog |
| Light ground blizzard |
| Light hail |
| Light rain |
| Light rain with snow |
| Light rain with thunderstorm |
| Light shower rain |
| Light shower rain with snow |
| Light snow |
| Light snow pellets |
| Mist |
| Mostly clear |
| Mostly cloudy |
| Overcast |
| Partly cloudy |
| Patchy fog |
| Precipitation |
| Rain |
| Rain and hail |
| Rain and snow |
| Rain and thunderstorm |
| Rain thunderstorm and hail |
| Reduced visibility due to smoke |
| Sandstorm |
| Severe thunderstorm |
| Shower rain |
| Shower rain with snow |
| Snow |
| Squall |
| Thunderstorm |
| Variable cloudiness |

---

## Related code

| Piece | Project | Role |
|-------|---------|------|
| `PlaceConverter` | Pipeline.Core | Cyrillic HTML name → `Place` enum |
| `EnumDisplayNameFormatter` | Pipeline.Core | `Place` / `WeatherCharacteristics` → English display label |
| `WeatherCharacteristicConverter` | Pipeline.Core | NameInHtml strings ↔ flags; builds the English CSV cell |
| `WeatherScalarCsvColumns` | Pipeline.Core | Scalar column header names and DateTime format |
| `NarrowFormatWeatherCsvColumns` | Pipeline.Core | Narrow `CoreColumns` including `Weather Characteristics` |
| `WeatherCharacteristicsColumns` | Pipeline.Core | Wide one-hot flag column names (full catalog except `None`) |
| `WeatherCsvColumns` | Pipeline.Core | Facade re-exporting the column constants above |
| `NarrowFormatWeatherDataCsvWriter` | Pipeline.Core | Writes narrow per-place CSVs under `narrow-format/` (parsed and time-normalized stages) |
| `ParsedStageManifestCsvWriter` | Pipeline.Parser | Writes `parsed-places.csv` and `weather-characteristics.csv` |
| `ParsedSourceFilesManifestWriter` | Pipeline.Parser | Writes `parsed-source-files.csv` |
| `ParsedSourceFilesManifestReader` | Pipeline.TimeNormalizer | Reads `parsed-source-files.csv` for normalization context |
| `NarrowFormatWeatherDataCsvReader` | Pipeline.Core | Reads narrow-format CSVs from a `narrow-format/` directory |
| `WideFormatWeatherDataCsvReader` | Pipeline.Core | Reads wide-format CSVs from a `wide-format/` directory for time normalization |
| `WideFormatWeatherDataCsvWriter` | Pipeline.Core | Writes wide-format per-place CSVs under `wide-format/` |
| `DenormalizingPipeline` | Pipeline.Denormalizer | Reads `parsed/narrow-format/`, writes wide CSVs under `parsed/wide-format/` |
| `AnalysisPipeline` | Pipeline.Analysis | Own runner stage writing to the host stage text log; reads `{stage}/narrow-format/`, writes usage CSV + `result-analysis{timestamp}.html` |

Unit tests live in `tests/Pipeline.Core.Tests` (CSV readers/writers and shared helpers), `tests/Pipeline.Parser.Tests`, `tests/Pipeline.Denormalizer.Tests`, `tests/Pipeline.TimeNormalizer.Tests`, and `tests/Pipeline.Analysis.Tests`.
