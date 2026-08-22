# Pipeline Runner

How to configure and run **Pipeline.Runner**, which parses historical weather HTML into CSV stages under `HtmlLog_<timestamp>/`.

## Configuration

Edit [`src/Pipeline.Runner/appsettings.json`](../src/Pipeline.Runner/appsettings.json) and set `HistoricalWeatherFilesRoot` to your local weather HTML root directory, or to a `.7z` archive of that tree.

The committed `appsettings.json` keeps an empty root so clones do not inherit machine-specific paths.

| Key | Default | Role |
|-----|---------|------|
| `HistoricalWeatherFilesRoot` | `""` | Directory of HTML, or a `.7z` of that tree |
| `RunInParallel` | `false` | Parallel parse; must be `false` for archive mode |
| `RunTimeNormalization` | `true` | Write `time-normalized/` after denormalization |
| `RunAnalysis` | `true` | Weather-characteristic usage stats per analyzed stage |
| `RunHtmlLogCsvComparison` | `true` | Chain-compare `HtmlLog_*` folders/zips after the run |

If you still have `RunNormalization` in an older config, rename it to `RunTimeNormalization` — the old key is not read.

## Source root (directory vs archive)

- **Directory** — HTML files under place-named folders (see [CSV output — place names](runner-csv-output.md#place-names)).
- **`.7z` archive** — entries are read sequentially without extracting to disk. `RunInParallel` must be `false`; the parser fails immediately if both archive mode and parallel are set.

Archives are assumed to be trusted internal weather dumps: there is currently no uncompressed entry-size or total-bytes cap, so untrusted `.7z` input is a zip-bomb risk. In archive mode, `parsed-source-files.csv` stores archive-relative entry keys rather than host absolute paths (see [CSV output](runner-csv-output.md)).

## Stages and flags

1. **Parse** — writes narrow per-place CSVs and manifests under `parsed/`.
2. **Parsed-stage analysis** — when `RunAnalysis` is `true`, analyzes `parsed/` and writes `weather-characteristics-usage.csv` and `result-analysis{timestamp}.html`.
3. **Denormalization** — always runs after optional parsed-stage analysis; writes wide-format CSVs at the `parsed/` stage root. If it produces no place files, the run fails.
4. **Time normalization** — when `RunTimeNormalization` is `true`, writes under `time-normalized/` (`normalized-columns/` plus wide CSVs at that stage root). Set the flag to `false` to skip.
5. **Time-normalized-stage analysis** — when both `RunTimeNormalization` and `RunAnalysis` are `true`, analyzes `time-normalized/` and writes its usage CSV and analysis report.
6. **HtmlLog CSV comparison** — when `RunHtmlLogCsvComparison` is `true`, runs chain comparison after the pipeline finishes (diagnostic only; see below).

Each stage directory has one text log (`parsed/log{timestamp}.log` and, when enabled, `time-normalized/log{timestamp}.log`) and HTML reports (`result{timestamp}.html` for parsing/time-normalization; `result-analysis{timestamp}.html` for each analysis).

The run folder `HtmlLog_<yyyy-MM-dd_HH-mm-ss>/` is created under the **process current working directory**, not under `HistoricalWeatherFilesRoot`.

## Run

```bash
dotnet run --project src/Pipeline.Runner/Pipeline.Runner.csproj
```

## Post-run HtmlLog comparison

When `RunHtmlLogCsvComparison` is `true` (default), chain comparison discovers all `HtmlLog_*` folders and `HtmlLog_*.zip` files under the working directory (any depth), compares each adjacent chronological pair, and appends results to `parsed/log{timestamp}.log` and the console **without failing the pipeline** on comparer exit 1 or 2.

Set `RunHtmlLogCsvComparison` to `false` to skip. Legacy `HtmlLog_*` folders from before an output-layout change may produce expected `NOT EQUAL` lines; archive or prune old trees if you want a clean chain.

Standalone pair/chain CLI details: [HtmlLog CSV comparer](htmllog-csv-comparer.md).

## Failure modes (ops)

- **Empty or missing `HistoricalWeatherFilesRoot`** — Pipeline.Runner logs an error and exits with a non-zero status (previously it exited 0).
- **Unknown HTML city name / path–place mismatch** — hard-fail or reject rules are documented under [CSV output — place names](runner-csv-output.md#place-names). An unknown HTML city aborts the run; a file whose path has no known place folder is rejected as a path/place mismatch even when its HTML names a known place.
- **Denormalization with zero place files** — the run fails with an error.

## Related docs

- [Pipeline Runner CSV output](runner-csv-output.md) — layout, column contracts, manifests, encoding
- [HtmlLog CSV comparer](htmllog-csv-comparer.md) — pair/chain CLI, matching rules, exit codes
- [Repository README](../README.md) — publication stubs and documentation index
