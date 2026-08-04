// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Parser;

using System.Globalization;
using Common;
using Microsoft.Extensions.Logging;

public sealed class ParseResultOrganizer
{
    private readonly ILogger<ParseResultOrganizer> logger;
    private readonly PlaceConverter placeConverter;

    public ParseResultOrganizer(ILogger<ParseResultOrganizer> logger, PlaceConverter placeConverter)
    {
        Argument.ThrowIfNull(logger);
        Argument.ThrowIfNull(placeConverter);

        this.logger = logger;
        this.placeConverter = placeConverter;
    }

    public ParseOrganizationResult OrganizeByPlaceAndDate(
        IEnumerable<(string FilePath, HtmlParseResult Result)> parseResults,
        ParsingIssueCollector issueCollector)
    {
        Argument.ThrowIfNull(parseResults);
        Argument.ThrowIfNull(issueCollector);
        var structuredResults = new Dictionary<string, SortedDictionary<DateTime, ParsedDateEntry>>(StringComparer.OrdinalIgnoreCase);
        var parsedPlacesByTranslatedName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pathPlaceMismatchRejections = 0;

        foreach (var (filePath, result) in parseResults
            .OrderBy(entry => entry.FilePath, StringComparer.OrdinalIgnoreCase))
        {
            var nameInHtml = result.CityName!.Trim();

            if (!this.ApplyPathSelfCheck(
                    filePath,
                    nameInHtml,
                    issueCollector,
                    ref pathPlaceMismatchRejections))
            {
                continue;
            }

            var translatedPlaceName = this.placeConverter.ToDisplayNameFromNameInHtml(result.CityName, filePath);

            if (string.IsNullOrWhiteSpace(result.Date) ||
                !DateTime.TryParseExact(result.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                issueCollector.AddSkippedFile(translatedPlaceName);
                this.logger.LogWarning("Skipping file {FilePath} due to missing or invalid date value '{DateValue}'.", filePath, result.Date);
                continue;
            }

            if (!parsedPlacesByTranslatedName.ContainsKey(translatedPlaceName))
            {
                parsedPlacesByTranslatedName[translatedPlaceName] = nameInHtml;
            }

            if (!structuredResults.TryGetValue(translatedPlaceName, out var resultsByDate))
            {
                resultsByDate = new SortedDictionary<DateTime, ParsedDateEntry>();
                structuredResults[translatedPlaceName] = resultsByDate;
            }

            if (resultsByDate.ContainsKey(parsedDate))
            {
                issueCollector.AddDuplicateDate(translatedPlaceName);
                this.logger.LogWarning(
                    "Duplicate entry detected for {Place} on {Date}. Overwriting previous data with file {FilePath}.",
                    translatedPlaceName,
                    parsedDate.ToString("yyyy-MM-dd"),
                    filePath);
            }

            resultsByDate[parsedDate] = new ParsedDateEntry(filePath, result);
        }

        var sourceFileEntries = structuredResults
            .SelectMany(pair => pair.Value.Select(
                dateEntry => new ParsedSourceFileEntry(pair.Key, dateEntry.Key, dateEntry.Value.FilePath)))
            .ToList();

        PlacePathSelfCheckLogger.LogPerPlaceSummary(this.logger, issueCollector);

        var parsedPlaces = parsedPlacesByTranslatedName
            .Select(pair => (EnglishName: pair.Key, NameInHtml: pair.Value))
            .OrderBy(pair => pair.EnglishName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ParseOrganizationResult(
            structuredResults,
            parsedPlaces,
            sourceFileEntries,
            pathPlaceMismatchRejections);
    }

    private bool ApplyPathSelfCheck(
        string filePath,
        string nameInHtml,
        ParsingIssueCollector issueCollector,
        ref int pathPlaceMismatchRejections)
    {
        if (!this.placeConverter.TryFromFilePath(filePath, out var pathPlace))
        {
            // No known place folder in the path. The HTML place must still be known
            // (FromNameInHtml throws otherwise), and the file is rejected — otherwise
            // files under arbitrary folders would be grouped by HTML alone.
            var htmlPlaceWithoutPath = this.placeConverter.FromNameInHtml(nameInHtml, filePath);
            var htmlPlaceFromUnknownPath = this.placeConverter.ToDisplayName(htmlPlaceWithoutPath);
            issueCollector.AddPathPlaceMismatch(
                filePath,
                ParsingIssueCollector.UnknownPlace,
                nameInHtml,
                htmlPlaceFromUnknownPath);

            this.logger.LogError(
                "Place path self-check failed for {FilePath}: path has no known place segment but HTML city is '{HtmlCityName}' ('{HtmlPlace}').",
                filePath,
                nameInHtml,
                htmlPlaceFromUnknownPath);

            pathPlaceMismatchRejections++;
            return false;
        }

        var pathPlaceDisplay = this.placeConverter.ToDisplayName(pathPlace);
        var htmlPlace = this.placeConverter.FromNameInHtml(nameInHtml, filePath);
        var htmlPlaceDisplay = this.placeConverter.ToDisplayName(htmlPlace);

        if (pathPlace != htmlPlace)
        {
            issueCollector.AddPathPlaceMismatch(
                filePath,
                pathPlaceDisplay,
                nameInHtml,
                htmlPlaceDisplay);

            this.logger.LogError(
                "Place path self-check failed for {FilePath}: path place '{PathPlace}' does not match HTML city '{HtmlCityName}' ('{HtmlPlace}').",
                filePath,
                pathPlaceDisplay,
                nameInHtml,
                htmlPlaceDisplay);

            pathPlaceMismatchRejections++;
            return false;
        }

        issueCollector.AddPathPlaceMatch(
            filePath,
            pathPlaceDisplay,
            nameInHtml,
            htmlPlaceDisplay);

        return true;
    }
}
