# Historical-Weather-Data-Harvester

Projects to collect HTML web pages with historical weather and to process it to Ukrainian Historical Weather Dataset.

See [src/HtmlScrapper/README.md](src/HtmlScrapper/README.md) for build and run instructions.

## Publication

This software was developed as part of the following scientific article (not yet published):

Yaroslav Kravets and Iryna Liutenko, "Methodology for Automated Extraction and Unification of Historical Meteorological Data from Semi-Structured Web Sources," *[Journal/Conference Name]*, [vol. X, no. Y, pp. xx–yy], [Year], doi: [DOI].

<!-- TODOs when the article is published:
- Replace [Journal/Conference Name] with the venue
- Replace [vol. X, no. Y, pp. xx–yy] with volume, issue, and pages
- Replace [Year] with the publication year
- Replace [DOI] with the DOI (or remove doi: if none)
- Remove the "(not yet published)" note above
-->

### Data availability

Source and processed datasets are not deposited yet:

- **Source corpus** (HTML, preferably as one or few `.7z` archives): *[Source Repository]* — doi: [Source DOI]
- **Processed dataset** (unified CSVs + data dictionary): *[Processed Repository]* — doi: [Processed DOI]

<!-- TODOs when datasets are published:
- Choose a repository for the source corpus and replace [Source Repository] / [Source DOI]
- Choose a repository for the processed dataset and replace [Processed Repository] / [Processed DOI]
- Prefer one or few .7z archives for source HTML (avoid thousands of unzipped files)
- Include a short data dictionary with the processed CSVs
- Cite both deposits in the article Data availability section
-->

## Pipeline Runner

Copy [`src/Pipeline.Runner/appsettings.example.json`](src/Pipeline.Runner/appsettings.example.json) to `appsettings.json` and set `HistoricalWeatherFilesRoot` to your local weather HTML root directory, or to a `.7z` archive of that tree. Archive mode reads entries sequentially without extracting to disk and requires `RunInParallel` to be `false` (the parser fails immediately if both are set). Archives are assumed to be trusted internal weather dumps: there is currently no uncompressed entry-size or total-bytes cap, so untrusted `.7z` input is a zip-bomb risk. In archive mode, `parsed-source-files.csv` stores archive-relative entry keys rather than host absolute paths (see [docs/runner-csv-output.md](docs/runner-csv-output.md)). The committed `appsettings.json` keeps an empty root so clones do not inherit machine-specific paths.

Wide-format CSVs are written at the `parsed/` stage root when denormalization produces at least one place file. Set `RunTimeNormalization` to `false` in `appsettings.json` to skip observation-time normalization (`time-normalized/normalized-columns/` and wide CSVs at `time-normalized/` root). If you still have `RunNormalization` in an older config, rename it to `RunTimeNormalization` — the old key is not read. Set `RunAnalysis` to `false` to skip weather-characteristic usage stats (`weather-characteristics-usage.csv` and the matching table written into each stage `result{timestamp}.html` before it closes; enabled by default). Set `RunHtmlLogCsvComparison` to `false` to skip chain comparison of `HtmlLog_*` folders and zips after the pipeline finishes (enabled by default). When enabled, chain comparison is diagnostic only: it discovers all `HtmlLog_*` folders and `HtmlLog_*.zip` files under the working directory (any depth), compares each adjacent chronological pair, and logs results to `parsed/log-compare<timestamp>.log` and the console without failing the pipeline run. Legacy `HtmlLog_*` folders from before an output-layout change may produce expected `NOT EQUAL` lines; archive or prune old trees if you want a clean chain.

```bash
dotnet run --project src/Pipeline.Runner/Pipeline.Runner.csproj
```

## Pipeline Runner CSV output

Pipeline.Runner writes per-place weather CSVs (English filenames, one row per observation) and three manifest files under `HtmlLog_<timestamp>/parsed/`; when `RunTimeNormalization` is enabled, normalized output goes under `HtmlLog_<timestamp>/time-normalized/`.

If `HistoricalWeatherFilesRoot` is missing or empty in `appsettings.json`, Pipeline.Runner logs an error and exits with a non-zero status (previously it exited 0).

See **[docs/runner-csv-output.md](docs/runner-csv-output.md)** for how output is structured, how place and weather labels are resolved, and how unknown place names are handled. Note that an unknown HTML city name aborts the run, and a file whose path has no known place folder is rejected as a path/place mismatch even when its HTML names a known place.

## HtmlLog CSV comparer

Compare CSV output across `HtmlLog_*` run folders or equivalent `.zip` files (pair or chain mode).

See **[docs/htmllog-csv-comparer.md](docs/htmllog-csv-comparer.md)** for CLI, matching rules, ZIP layout, PARTLY EQUAL semantics, verbose JSON, and exit behavior.
