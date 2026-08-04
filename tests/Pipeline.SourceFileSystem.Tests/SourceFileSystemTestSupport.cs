// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.SourceFileSystem.Tests;

using System.IO.Abstractions;
using System.Text;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.SevenZip;

internal static class SourceFileSystemTestSupport
{
    public static void WriteSevenZipArchive(
        IFileSystem host,
        string archivePath,
        params (string EntryPath, string Content)[] entries)
    {
        host.Directory.CreateDirectory(host.Path.GetDirectoryName(archivePath)!);
        using var archiveStream = host.File.Create(archivePath);
        using var writer = WriterFactory.OpenWriter(
            archiveStream,
            ArchiveType.SevenZip,
            new SevenZipWriterOptions());
        foreach (var (entryPath, content) in entries)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            using var entryStream = new MemoryStream(bytes);
            writer.Write(entryPath, entryStream, null);
        }
    }

    /// <summary>
    /// Writes a single-entry .7z whose stored entry key is exactly <paramref name="entryKey"/>.
    /// SharpCompress's writer sanitizes rooted/UNC/drive keys, so this builds a same-length
    /// placeholder archive then patches the UTF-16 name and refreshes the 7z header CRCs.
    /// </summary>
    public static void WriteSevenZipArchiveWithRawEntryKey(
        IFileSystem host,
        string archivePath,
        string entryKey,
        string content)
    {
        if (entryKey is null)
        {
            throw new ArgumentNullException(nameof(entryKey));
        }

        if (entryKey.Length == 0)
        {
            throw new ArgumentException("Entry key must be non-empty.", nameof(entryKey));
        }

        host.Directory.CreateDirectory(host.Path.GetDirectoryName(archivePath)!);

        var placeholder = new string('A', entryKey.Length);
        using var memory = new MemoryStream();
        using (var writer = new SevenZipWriter(memory, new SevenZipWriterOptions { CompressHeader = false }))
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            using var entryStream = new MemoryStream(bytes);
            writer.Write(placeholder, entryStream, null);
        }

        var raw = memory.ToArray();
        var placeholderUtf16 = Encoding.Unicode.GetBytes(placeholder);
        var entryKeyUtf16 = Encoding.Unicode.GetBytes(entryKey);
        var index = IndexOf(raw, placeholderUtf16);
        if (index < 0)
        {
            throw new InvalidOperationException("Could not locate placeholder entry name in 7z header.");
        }

        Buffer.BlockCopy(entryKeyUtf16, 0, raw, index, entryKeyUtf16.Length);

        var nextHeaderOffset = BitConverter.ToInt64(raw, 12);
        var nextHeaderSize = BitConverter.ToInt64(raw, 20);
        var headerStart = 32 + (int)nextHeaderOffset;
        var nextHeaderCrc = Crc32Ieee(raw.AsSpan(headerStart, (int)nextHeaderSize));
        BitConverter.TryWriteBytes(raw.AsSpan(28, 4), nextHeaderCrc);
        var startHeaderCrc = Crc32Ieee(raw.AsSpan(12, 20));
        BitConverter.TryWriteBytes(raw.AsSpan(8, 4), startHeaderCrc);

        host.File.WriteAllBytes(archivePath, raw);
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            var matched = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return i;
            }
        }

        return -1;
    }

    private static uint Crc32Ieee(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return crc ^ 0xFFFFFFFFu;
    }
}
