// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer.Tests;

using System.IO.Abstractions;
using System.IO.Compression;
using Common;
using FileSystem.TestSupport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class CsvComparisonOutputTests
{
    private const string IdenticalCsv = "DateTime,Temperature\n2020-01-01,1\n";

    [Fact]
    public void CompareDirectories_Returns0WhenTreesAreEqual()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = CreateDirectoryWithCsv(fileSystem, "left", IdenticalCsv);
        var right = CreateDirectoryWithCsv(fileSystem, "right", IdenticalCsv);

        var exitCode = CreateOutput(fileSystem).CompareDirectories(left, right);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void CompareDirectories_WithVerbose_IncludesCompactJsonWhenEqual()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var left = CreateDirectoryWithCsv(fileSystem, "left", IdenticalCsv);
        var right = CreateDirectoryWithCsv(fileSystem, "right", IdenticalCsv);

        var exitCode = CreateOutput(fileSystem, logger).CompareDirectories(left, right, verbose: true);

        Assert.Equal(0, exitCode);
        var equal = Assert.Single(logger.Messages, message => message.Contains("— EQUAL", StringComparison.Ordinal));
        Assert.Contains($"[1/1] {left} (1 csv) vs {right} (1 csv) — EQUAL", equal, StringComparison.Ordinal);
        Assert.Contains(
            """
            {
              "matched": {
                "total": 1,
                "by_path": 1,
                "by_hash": 0,
                "by_columns": 0
              }
            }
            """.ReplaceLineEndings("\n"),
            equal.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void CompareDirectories_WithVerbose_IncludesExpandedJsonWhenNotEqual()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var left = CreateDirectoryWithCsv(fileSystem, "left", IdenticalCsv);
        var right = CreateDirectoryWithCsv(fileSystem, "right", "DateTime,Temperature\n2020-01-01,2\n");

        var exitCode = CreateOutput(fileSystem, logger).CompareDirectories(left, right, verbose: true);

        Assert.Equal(1, exitCode);
        var notEqual = Assert.Single(logger.Messages, message => message.Contains("— NOT EQUAL", StringComparison.Ordinal));
        Assert.Contains("\"different\"", notEqual, StringComparison.Ordinal);
        Assert.Contains("\"files\"", notEqual, StringComparison.Ordinal);
        Assert.Contains("\"unmatched_left\"", notEqual, StringComparison.Ordinal);
        Assert.Contains("\"partly_equal\"", notEqual, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareDirectories_FormatsCompactCountsForNotEqualPairsWithoutVerbose()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "b"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "a", "Odesa.csv"),
            "DateTime,Humidity\n2020-01-01,50\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "parsed", "LeftOnly.csv"),
            "DateTime,Wind\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "parsed", "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "b", "Odesa.csv"),
            "DateTime,Humidity\n2020-01-01,50\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "parsed", "RightOnly.csv"),
            "DateTime,Pressure\n2020-01-01,1013\n");

        var exitCode = CreateOutput(fileSystem, logger).CompareDirectories(left, right);

        Assert.Equal(1, exitCode);
        var notEqual = Assert.Single(logger.Messages, message => message.Contains("— NOT EQUAL", StringComparison.Ordinal));
        Assert.Contains($"[1/1] {left} (3 csv) vs {right} (3 csv) — NOT EQUAL", notEqual, StringComparison.Ordinal);
        Assert.Contains(
            """
            {
              "matching": {
                "matched": {
                  "total": 2,
                  "by_path": 1,
                  "by_hash": 1,
                  "by_columns": 0
                },
                "unmatched_left": 1,
                "unmatched_right": 1
              },
              "comparison": {
                "equal": 1,
                "partly_equal": 0,
                "different": 1
              }
            }
            """.ReplaceLineEndings("\n"),
            notEqual.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
        Assert.DoesNotContain("\"files\"", notEqual, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareDirectories_FormatsCompactCountsForPartlyEqualPairsWithoutVerbose()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var left = CreateDirectoryWithCsv(
            fileSystem,
            "left",
            "Place,DateTime,Temperature\nKyiv,2020-01-01,1\n");
        var right = CreateDirectoryWithCsv(
            fileSystem,
            "right",
            "DateTime,Temperature\n2020-01-01,1\n");

        var exitCode = CreateOutput(fileSystem, logger).CompareDirectories(left, right);

        Assert.Equal(1, exitCode);
        var partlyEqual = Assert.Single(logger.Messages, message => message.Contains("— PARTLY EQUAL", StringComparison.Ordinal));
        Assert.Contains($"[1/1] {left} (1 csv) vs {right} (1 csv) — PARTLY EQUAL", partlyEqual, StringComparison.Ordinal);
        Assert.Contains(
            """
            {
              "matching": {
                "matched": {
                  "total": 1,
                  "by_path": 1,
                  "by_hash": 0,
                  "by_columns": 0
                },
                "unmatched_left": 0,
                "unmatched_right": 0
              },
              "comparison": {
                "equal": 0,
                "partly_equal": 1,
                "different": 0
              }
            }
            """.ReplaceLineEndings("\n"),
            partlyEqual.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
        Assert.DoesNotContain("\"groups\"", partlyEqual, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareDirectories_Returns1WhenTreesDiffer()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = CreateDirectoryWithCsv(fileSystem, "left", IdenticalCsv);
        var right = CreateDirectoryWithCsv(fileSystem, "right", "DateTime,Temperature\n2020-01-01,2\n");

        var exitCode = CreateOutput(fileSystem).CompareDirectories(left, right);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void CompareDirectories_Returns2WhenHashIsDuplicated()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "b"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "c"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "d"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "a", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "b", "Odesa.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "c", "Lviv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "d", "Kharkiv.csv"), IdenticalCsv);

        var exitCode = CreateOutput(fileSystem).CompareDirectories(left, right);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void CompareDirectories_Returns2WhenColumnsAreDuplicated()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "Kyiv.csv"),
            "DateTime,Temperature,DateTime\n2020-01-01,1,2020-01-01\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");

        var exitCode = CreateOutput(fileSystem).CompareDirectories(left, right);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void CompareDirectories_Returns2WhenDataRowHasFewerFieldsThanHeader()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(left);
        fileSystem.Directory.CreateDirectory(right);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "Kyiv.csv"),
            "A,B,C\n1,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "Kyiv.csv"),
            "A,B,C\n1,2,x\n");

        var exitCode = CreateOutput(fileSystem).CompareDirectories(left, right);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void CompareDirectories_Returns2WhenSignatureIsDuplicated()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "b"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "c"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "d"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "a", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "b", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,2\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "c", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,3\n");
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(right, "d", "Kyiv.csv"), "DateTime,Temperature\n2020-01-01,4\n");

        var exitCode = CreateOutput(fileSystem).CompareDirectories(left, right);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void CompareDirectories_Returns2WhenSourceIsNeitherDirectoryNorZip()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = CreateDirectoryWithCsv(fileSystem, "left", IdenticalCsv);
        var missing = InMemoryFileSystem.UnderRoot(fileSystem, "missing");

        var exitCode = CreateOutput(fileSystem).CompareDirectories(left, missing);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void CompareDirectories_Returns2WhenZipIsCorrupt()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = CreateDirectoryWithCsv(fileSystem, "left", IdenticalCsv);
        var zip = InMemoryFileSystem.UnderRoot(fileSystem, "right.zip");
        fileSystem.File.WriteAllBytes(zip, [0x00, 0x01, 0x02, 0x03]);

        var exitCode = CreateOutput(fileSystem).CompareDirectories(left, zip);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void CompareDirectories_Returns2AndLogsSourceErrorWhenCsvHasNoDataRows()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var left = CreateDirectoryWithCsv(fileSystem, "left", "DateTime,Temperature\n");
        var right = CreateDirectoryWithCsv(fileSystem, "right", IdenticalCsv);
        var logger = new CapturingLogger<CsvComparisonOutput>();

        var exitCode = CreateOutput(fileSystem, logger).CompareDirectories(left, right);

        Assert.Equal(2, exitCode);
        Assert.Contains(
            logger.Messages,
            message => message.Contains("no data rows", StringComparison.Ordinal));
    }

    [Fact]
    public void CompareChain_Returns0ForEqualMixedFolderAndZipSources()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        var thirdName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 7));
        var first = fileSystem.Path.Combine(root, firstName);
        var secondZip = fileSystem.Path.Combine(root, secondName + ".zip");
        var third = fileSystem.Path.Combine(root, thirdName);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(third, "parsed"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(first, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(third, "parsed", "Kyiv.csv"), IdenticalCsv);
        CreateZip(fileSystem, secondZip, ($"{secondName}/parsed/Kyiv.csv", IdenticalCsv));

        var exitCode = CreateOutput(fileSystem).CompareChain(root);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void CompareChain_WithVerbose_IncludesCompactJsonForEqualPairs()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        var first = fileSystem.Path.Combine(root, firstName);
        var second = fileSystem.Path.Combine(root, secondName);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(second, "parsed"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(first, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(second, "parsed", "Kyiv.csv"), IdenticalCsv);

        var exitCode = CreateOutput(fileSystem, logger).CompareChain(root, verbose: true);

        Assert.Equal(0, exitCode);
        var equal = Assert.Single(logger.Messages, message => message.Contains("— EQUAL", StringComparison.Ordinal));
        Assert.Contains($"[1/1] {first} (1 csv) vs {second} (1 csv) — EQUAL", equal, StringComparison.Ordinal);
        Assert.Contains(
            """
            {
              "matched": {
                "total": 1,
                "by_path": 1,
                "by_hash": 0,
                "by_columns": 0
              }
            }
            """.ReplaceLineEndings("\n"),
            equal.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void CompareChain_Returns2WhenCorruptZipAbortsImmediately()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        var thirdName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 7));
        var first = fileSystem.Path.Combine(root, firstName);
        var corruptZip = fileSystem.Path.Combine(root, secondName + ".zip");
        var third = fileSystem.Path.Combine(root, thirdName);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(third, "parsed"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(first, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(third, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllBytes(corruptZip, [0x00, 0x01, 0x02, 0x03]);

        var exitCode = CreateOutput(fileSystem, logger).CompareChain(root);

        Assert.Equal(2, exitCode);
        Assert.Contains(logger.Messages, message => message.Contains("error:", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("SUMMARY:", StringComparison.Ordinal));
    }

    [Fact]
    public void CompareChain_Returns2WhenCorruptZipAbortsImmediately_Verbose()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        var thirdName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 7));
        var first = fileSystem.Path.Combine(root, firstName);
        var corruptZip = fileSystem.Path.Combine(root, secondName + ".zip");
        var third = fileSystem.Path.Combine(root, thirdName);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(third, "parsed"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(first, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(third, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllBytes(corruptZip, [0x00, 0x01, 0x02, 0x03]);

        var exitCode = CreateOutput(fileSystem, logger).CompareChain(root, verbose: true);

        Assert.Equal(2, exitCode);
        Assert.Contains(logger.Messages, message => message.Contains("error:", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("SUMMARY:", StringComparison.Ordinal));
    }

    [Fact]
    public void CompareChain_Returns2WhenCorruptZipIsLastSource()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        var thirdName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 7));
        var first = fileSystem.Path.Combine(root, firstName);
        var second = fileSystem.Path.Combine(root, secondName);
        var corruptZip = fileSystem.Path.Combine(root, thirdName + ".zip");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(second, "parsed"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(first, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "parsed", "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,2\n");
        fileSystem.File.WriteAllBytes(corruptZip, [0x00, 0x01, 0x02, 0x03]);

        var exitCode = CreateOutput(fileSystem, logger).CompareChain(root);

        Assert.Equal(2, exitCode);
        Assert.Contains(logger.Messages, message => message.Contains("error:", StringComparison.Ordinal));
    }

    [Fact]
    public void CompareChain_FormatsGroupedCountsForNotEqualPairs()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        var first = fileSystem.Path.Combine(root, firstName);
        var second = fileSystem.Path.Combine(root, secondName);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(second, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(second, "b"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(first, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(first, "a", "Odesa.csv"),
            "DateTime,Humidity\n2020-01-01,50\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(first, "parsed", "LeftOnly.csv"),
            "DateTime,Wind\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "parsed", "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "b", "Odesa.csv"),
            "DateTime,Humidity\n2020-01-01,50\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "parsed", "RightOnly.csv"),
            "DateTime,Pressure\n2020-01-01,1013\n");

        var exitCode = CreateOutput(fileSystem, logger).CompareChain(root);

        Assert.Equal(1, exitCode);
        var notEqual = Assert.Single(logger.Messages, message => message.Contains("— NOT EQUAL", StringComparison.Ordinal));
        Assert.Contains($"[1/1] {first} (3 csv) vs {second} (3 csv) — NOT EQUAL", notEqual, StringComparison.Ordinal);
        Assert.Contains(
            """
            {
              "matching": {
                "matched": {
                  "total": 2,
                  "by_path": 1,
                  "by_hash": 1,
                  "by_columns": 0
                },
                "unmatched_left": 1,
                "unmatched_right": 1
              },
              "comparison": {
                "equal": 1,
                "partly_equal": 0,
                "different": 1
              }
            }
            """.ReplaceLineEndings("\n"),
            notEqual.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
        Assert.DoesNotContain("byte_identical=", notEqual, StringComparison.Ordinal);
        Assert.DoesNotContain("left_only=", notEqual, StringComparison.Ordinal);
        Assert.DoesNotContain("\"files\"", notEqual, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareChain_WithVerbose_IncludesUnmatchedAndDifferentPathsInJson()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        var first = fileSystem.Path.Combine(root, firstName);
        var second = fileSystem.Path.Combine(root, secondName);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "a"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(second, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(second, "b"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(first, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(first, "a", "Odesa.csv"),
            "DateTime,Humidity\n2020-01-01,50\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(first, "parsed", "LeftOnly.csv"),
            "DateTime,Wind\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "parsed", "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "b", "Odesa.csv"),
            "DateTime,Humidity\n2020-01-01,50\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "parsed", "RightOnly.csv"),
            "DateTime,Pressure\n2020-01-01,1013\n");

        var exitCode = CreateOutput(fileSystem, logger).CompareChain(root, verbose: true);

        Assert.Equal(1, exitCode);
        var notEqual = Assert.Single(logger.Messages, message => message.Contains("— NOT EQUAL", StringComparison.Ordinal));
        Assert.Contains(
            """
            {
              "matching": {
                "matched": {
                  "total": 2,
                  "by_path": 1,
                  "by_hash": 1,
                  "by_columns": 0
                },
                "unmatched_left": {
                  "total": 1,
                  "files": [
                    "parsed/LeftOnly.csv"
                  ]
                },
                "unmatched_right": {
                  "total": 1,
                  "files": [
                    "parsed/RightOnly.csv"
                  ]
                }
              },
              "comparison": {
                "equal": 1,
                "partly_equal": {
                  "total": 0,
                  "groups": []
                },
                "different": {
                  "total": 1,
                  "files": [
                    {
                      "left": "parsed/Kyiv.csv",
                      "right": "parsed/Kyiv.csv"
                    }
                  ]
                }
              }
            }
            """.ReplaceLineEndings("\n"),
            notEqual.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void CompareChain_WithVerbose_IncludesPartlyEqualColumnListsInJson()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        var first = fileSystem.Path.Combine(root, firstName);
        var second = fileSystem.Path.Combine(root, secondName);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(second, "parsed"));
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(first, "parsed", "Kyiv.csv"),
            "Place,DateTime,Temperature\nKyiv,2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "parsed", "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");

        var exitCode = CreateOutput(fileSystem, logger).CompareChain(root, verbose: true);

        Assert.Equal(1, exitCode);
        var partlyEqual = Assert.Single(logger.Messages, message => message.Contains("— PARTLY EQUAL", StringComparison.Ordinal));
        Assert.Contains(
            """
            {
              "matching": {
                "matched": {
                  "total": 1,
                  "by_path": 1,
                  "by_hash": 0,
                  "by_columns": 0
                },
                "unmatched_left": {
                  "total": 0,
                  "files": []
                },
                "unmatched_right": {
                  "total": 0,
                  "files": []
                }
              },
              "comparison": {
                "equal": 0,
                "partly_equal": {
                  "total": 1,
                  "groups": [
                    {
                      "left_only_columns": [
                        "Place"
                      ],
                      "right_only_columns": [],
                      "intersecting_columns": [
                        "DateTime",
                        "Temperature"
                      ],
                      "total": 1,
                      "pairs": [
                        {
                          "left": "parsed/Kyiv.csv",
                          "right": "parsed/Kyiv.csv"
                        }
                      ]
                    }
                  ]
                },
                "different": {
                  "total": 0,
                  "files": []
                }
              }
            }
            """.ReplaceLineEndings("\n"),
            partlyEqual.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
        var summary = Assert.Single(logger.Messages, message => message.Contains("SUMMARY:", StringComparison.Ordinal));
        Assert.Contains("1 partly equal, 0 not equal", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareChain_WithVerbose_GroupsPartlyEqualPairsByColumnSignature()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        var first = fileSystem.Path.Combine(root, firstName);
        var second = fileSystem.Path.Combine(root, secondName);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(second, "parsed"));
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(first, "parsed", "Kyiv.csv"),
            "Place,DateTime,Temperature\nKyiv,2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(first, "parsed", "Odesa.csv"),
            "Place,DateTime,Temperature\nOdesa,2020-01-01,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "parsed", "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "parsed", "Odesa.csv"),
            "DateTime,Temperature\n2020-01-01,2\n");

        var exitCode = CreateOutput(fileSystem, logger).CompareChain(root, verbose: true);

        Assert.Equal(1, exitCode);
        var partlyEqual = Assert.Single(logger.Messages, message => message.Contains("— PARTLY EQUAL", StringComparison.Ordinal));
        Assert.Contains(
            """
            "partly_equal": {
                  "total": 2,
                  "groups": [
                    {
                      "left_only_columns": [
                        "Place"
                      ],
                      "right_only_columns": [],
                      "intersecting_columns": [
                        "DateTime",
                        "Temperature"
                      ],
                      "total": 2,
                      "pairs": [
                        {
                          "left": "parsed/Kyiv.csv",
                          "right": "parsed/Kyiv.csv"
                        },
                        {
                          "left": "parsed/Odesa.csv",
                          "right": "parsed/Odesa.csv"
                        }
                      ]
                    }
                  ]
                }
            """.ReplaceLineEndings("\n"),
            partlyEqual.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(partlyEqual, "\"left_only_columns\": ["));
        Assert.Contains("\"Place\"", partlyEqual, StringComparison.Ordinal);
        Assert.Contains("\"DateTime\"", partlyEqual, StringComparison.Ordinal);
        Assert.Contains("\"Temperature\"", partlyEqual, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareChain_WithVerbose_DoesNotMergeGroupsWhenCommaJoinWouldCollide()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var firstName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var secondName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 6));
        var first = fileSystem.Path.Combine(root, firstName);
        var second = fileSystem.Path.Combine(root, secondName);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(first, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(second, "parsed"));
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(first, "parsed", "pair-a.csv"),
            "\"A,B\",X\n1,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(first, "parsed", "pair-b.csv"),
            "A,B,X\n1,2,3\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "parsed", "pair-a.csv"),
            "X\n2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(second, "parsed", "pair-b.csv"),
            "X\n3\n");

        var exitCode = CreateOutput(fileSystem, logger).CompareChain(root, verbose: true);

        Assert.Equal(1, exitCode);
        var partlyEqual = Assert.Single(logger.Messages, message => message.Contains("— PARTLY EQUAL", StringComparison.Ordinal));
        Assert.Equal(2, CountOccurrences(partlyEqual, "\"left_only_columns\": ["));
        Assert.Contains(
            """
            "left_only_columns": [
                        "A,B"
                      ]
            """.ReplaceLineEndings("\n"),
            partlyEqual.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
        Assert.Contains(
            """
            "left_only_columns": [
                        "A",
                        "B"
                      ]
            """.ReplaceLineEndings("\n"),
            partlyEqual.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void CompareDirectories_GroupsPartlyEqualPairsByColumnSignatureInJson()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "left");
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "right");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "parsed"));
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "parsed", "Kyiv.csv"),
            "Place,DateTime,Temperature\nKyiv,2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(left, "parsed", "Odesa.csv"),
            "Place,DateTime,Temperature\nOdesa,2020-01-01,2\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "parsed", "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,1\n");
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "parsed", "Odesa.csv"),
            "DateTime,Temperature\n2020-01-01,2\n");

        var exitCode = CreateOutput(fileSystem, logger).CompareDirectories(left, right, verbose: true);

        Assert.Equal(1, exitCode);
        var partlyEqual = Assert.Single(logger.Messages, message => message.Contains("— PARTLY EQUAL", StringComparison.Ordinal));
        Assert.Contains($"[1/1] {left} (2 csv) vs {right} (2 csv) — PARTLY EQUAL", partlyEqual, StringComparison.Ordinal);
        Assert.Contains(
            """
            "partly_equal": {
                  "total": 2,
                  "groups": [
                    {
                      "left_only_columns": [
                        "Place"
                      ],
                      "right_only_columns": [],
                      "intersecting_columns": [
                        "DateTime",
                        "Temperature"
                      ],
                      "total": 2,
                      "pairs": [
                        {
                          "left": "parsed/Kyiv.csv",
                          "right": "parsed/Kyiv.csv"
                        },
                        {
                          "left": "parsed/Odesa.csv",
                          "right": "parsed/Odesa.csv"
                        }
                      ]
                    }
                  ]
                }
            """.ReplaceLineEndings("\n"),
            partlyEqual.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(partlyEqual, "\"left_only_columns\": ["));
    }

    [Fact]
    public void CompareDirectories_StatusLineUsesFullPathsWhenLeafNamesMatch()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var logger = new CollectingLogger<CsvComparisonOutput>();
        var runName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2024, 1, 1));
        var left = InMemoryFileSystem.UnderRoot(fileSystem, "a", runName);
        var right = InMemoryFileSystem.UnderRoot(fileSystem, "b", runName);
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(left, "parsed"));
        fileSystem.Directory.CreateDirectory(fileSystem.Path.Combine(right, "parsed"));
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(left, "parsed", "Kyiv.csv"), IdenticalCsv);
        fileSystem.File.WriteAllText(
            fileSystem.Path.Combine(right, "parsed", "Kyiv.csv"),
            "DateTime,Temperature\n2020-01-01,2\n");

        var exitCode = CreateOutput(fileSystem, logger).CompareDirectories(left, right);

        Assert.Equal(1, exitCode);
        var notEqual = Assert.Single(logger.Messages, message => message.Contains("— NOT EQUAL", StringComparison.Ordinal));
        Assert.Contains($"[1/1] {left} (1 csv) vs {right} (1 csv) — NOT EQUAL", notEqual, StringComparison.Ordinal);
        Assert.DoesNotContain(
            $"[1/1] {runName} (1 csv) vs {runName} (1 csv) — NOT EQUAL",
            notEqual,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CompareChain_Returns2WhenDirectoryAndZipShareRunName()
    {
        var fileSystem = InMemoryFileSystem.Create();
        var root = InMemoryFileSystem.UnderRoot(fileSystem, "runs");
        var runName = HtmlLogRunDirectory.FormatDirectoryName(new DateTime(2026, 1, 2, 3, 4, 5));
        var directory = fileSystem.Path.Combine(root, runName);
        var zip = fileSystem.Path.Combine(root, runName + ".zip");
        fileSystem.Directory.CreateDirectory(directory);
        fileSystem.File.WriteAllBytes(zip, []);

        var exitCode = CreateOutput(fileSystem).CompareChain(root);

        Assert.Equal(2, exitCode);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var index = 0; (index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0; index += needle.Length)
        {
            count++;
        }

        return count;
    }

    private static CsvComparisonOutput CreateOutput(
        IFileSystem fileSystem,
        ILogger<CsvComparisonOutput>? logger = null) =>
        new(
            logger ?? NullLogger<CsvComparisonOutput>.Instance,
            fileSystem,
            new CsvTreeComparer(fileSystem),
            new HtmlLogDirectoryDiscovery(fileSystem));

    private static string CreateDirectoryWithCsv(IFileSystem fileSystem, string name, string content)
    {
        var root = InMemoryFileSystem.UnderRoot(fileSystem, name);
        fileSystem.Directory.CreateDirectory(root);
        fileSystem.File.WriteAllText(fileSystem.Path.Combine(root, "shared.csv"), content);
        return root;
    }

    private static void CreateZip(
        IFileSystem fileSystem,
        string path,
        params (string Path, string Content)[] entries)
    {
        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
        using var stream = fileSystem.File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var (entryPath, content) in entries)
        {
            var entry = archive.CreateEntry(entryPath);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            this.Messages.Add(formatter(state, exception));

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
