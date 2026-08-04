# HtmlLog CSV comparer

Use this tool to compare CSV trees from Pipeline Runner runs and see whether they match (or how they differ). Pass `HtmlLog_*` folders, equivalent `.zip` files, or one of each.

## Pair compare

```bash
dotnet run --project src/HtmlLogCsvComparer -- --compare /path/to/HtmlLog_<ts-a> /path/to/HtmlLog_<ts-b>
dotnet run --project src/HtmlLogCsvComparer -- /path/to/HtmlLog_<ts-a>.zip /path/to/HtmlLog_<ts-b>
```

## Chain compare

To compare **adjacent** runs discovered under a search root at any depth (valid `HtmlLog_<timestamp>` folders and `.zip` files, mixed and sorted chronologically):

```bash
dotnet run --project src/HtmlLogCsvComparer -- --chain /path/to/search/root
dotnet run --project src/HtmlLogCsvComparer -- --verbose --chain /path/to/search/root
```

Fewer than 2 sources, or duplicate run names (e.g. both `HtmlLog_<ts>` and `HtmlLog_<ts>.zip` under the search root), abort discovery with exit 2.

## Matching and comparison rules

The tool walks all `*.csv` files under each source (typically under `parsed/` and `time-normalized/`), pairs files by same relative path (case-insensitive) first, then by SHA-256 hash of parsed CSV content (BOM-stripped headers, parsed row fields, and row count) when that hash is unique on both sides, then by file name, CSV header columns, and data-row count for any leftovers. Duplicate shared hashes or signatures are an error. Every compared CSV must have at least one data row; a header-only or empty CSV aborts the comparison (exit 2).

A ZIP may contain the folder contents at its root or under one top-level folder matching the ZIP base name (without the `.zip` extension). ZIP entry names must be archive-relative; Unix-rooted, UNC, drive-qualified, and `..` segment keys are rejected (same idea as `.7z` pipeline sources). Leading `./` segments are stripped.

Matched pairs are compared by parsed-field equality, not raw bytes — CSVs that differ only in quoting or line endings can still be content-identical. Unequal results are PARTLY EQUAL when column sets differ but all rows match on the intersecting columns (positional `Rows[i]` checks, not keyed — an insert or delete near the top can shift later rows and drop the pair out of PARTLY EQUAL), otherwise NOT EQUAL. The overall logged `STATUS` is PARTLY EQUAL only when there is at least one partly-equal pair, no unmatched paths, and no NOT EQUAL pairs (EQUAL pairs may still be present); otherwise the run is NOT EQUAL even if some pairs are partly equal.

## Logging output

Each comparison logs a status line `[n/m] <left_path> (N csv) vs <right_path> (M csv) — STATUS`. Equal pairs log at information level with status only (compact JSON only with `--verbose`); partly-equal and not-equal pairs log at warning level and append indented JSON counts:

```json
{
  "matching": {
    "matched": {
      "total": 80,
      "by_path": 76,
      "by_hash": 2,
      "by_columns": 2
    },
    "unmatched_left": 4,
    "unmatched_right": 3
  },
  "comparison": {
    "equal": 62,
    "partly_equal": 0,
    "different": 18
  }
}
```

`comparison.equal` is the content-identical pair count. With `--verbose` (pair or chain mode), EQUAL per-pair JSON adds a compact `matched` breakdown; PARTLY EQUAL / NOT EQUAL JSON expands `unmatched_left`, `unmatched_right`, `comparison.partly_equal`, and `comparison.different` to include paths (and partly-equal column groups). Both modes always emit compact counts JSON for unequal pairs even without `--verbose`. On the first load or compare error in chain mode, the tool logs the error and exits 2 (no SUMMARY); prior pair lines may already have been printed.

## Exit codes

- **0** — all matched pairs content-identical and no unmatched paths
- **1** — overall STATUS is PARTLY EQUAL or NOT EQUAL (including unmatched leftovers)
- **2** — load/compare/discovery error (including header-only or empty CSV); chain mode prints no SUMMARY

These exit codes apply to the standalone CLI. When Pipeline Runner runs chain comparison via `RunHtmlLogCsvComparison`, it logs the result and does not fail the pipeline on exit 1 or 2.

## Related docs

- [Pipeline Runner CSV output](runner-csv-output.md) — output layout, manifests, place resolution
- Pipeline Runner can run chain comparison after a pipeline finish via `RunHtmlLogCsvComparison` (see the repository [README](../README.md#pipeline-runner))
