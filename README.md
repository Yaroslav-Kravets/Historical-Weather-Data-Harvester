# Historical-Weather-Data-Harvester

Projects to collect HTML web pages with historical weather and to process them into a Ukrainian Historical Weather Dataset.

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

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

## Documentation

- [HtmlScrapper](src/HtmlScrapper/README.md) — scrape historical weather HTML from meteo.ua
- [Pipeline Runner](docs/pipeline-runner.md) — configure and run parsing, denormalization, time normalization, analysis, and post-run comparison
- [Pipeline Runner CSV output](docs/runner-csv-output.md) — output layout, column contracts, manifests, place resolution, encoding
- [HtmlLog CSV comparer](docs/htmllog-csv-comparer.md) — pair/chain CLI to compare `HtmlLog_*` folders or zips
