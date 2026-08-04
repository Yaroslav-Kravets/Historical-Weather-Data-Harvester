// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLogCsvComparer;

using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Common;

internal static class CsvContentHashSerializer
{
    private const int StackAllocByteThreshold = 512;

    /// <summary>
    /// Appends parsed CSV content. Layout: header fields, each data row's fields, then row count
    /// (count last so a single streaming pass can hash without buffering rows).
    /// </summary>
    public static void AppendParsedContent(
        IncrementalHash hash,
        CsvHeader header,
        IReadOnlyList<CsvRow> rows)
    {
        Argument.ThrowIfNull(hash);
        Argument.ThrowIfNull(header);
        Argument.ThrowIfNull(rows);

        AppendFieldList(hash, header.Columns);
        foreach (var row in rows)
        {
            AppendFieldList(hash, row.Fields);
        }

        AppendInt32(hash, rows.Count);
    }

    public static void AppendHeader(IncrementalHash hash, CsvHeader header)
    {
        Argument.ThrowIfNull(hash);
        Argument.ThrowIfNull(header);

        AppendFieldList(hash, header.Columns);
    }

    public static void AppendRowFields(IncrementalHash hash, IReadOnlyList<string> fields)
    {
        Argument.ThrowIfNull(hash);
        Argument.ThrowIfNull(fields);

        AppendFieldList(hash, fields);
    }

    public static void AppendRowCount(IncrementalHash hash, int rowCount)
    {
        Argument.ThrowIfNull(hash);

        AppendInt32(hash, rowCount);
    }

    private static void AppendFieldList(IncrementalHash hash, IReadOnlyList<string> fields)
    {
        AppendInt32(hash, fields.Count);
        foreach (var field in fields)
        {
            AppendLengthPrefixedUtf8(hash, field);
        }
    }

    private static void AppendLengthPrefixedUtf8(IncrementalHash hash, string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        if (maxByteCount <= StackAllocByteThreshold)
        {
            Span<byte> buffer = stackalloc byte[StackAllocByteThreshold];
            var byteCount = Encoding.UTF8.GetBytes(value, buffer);
            AppendInt32(hash, byteCount);
            hash.AppendData(buffer[..byteCount]);
            return;
        }

        var rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
        try
        {
            var byteCount = Encoding.UTF8.GetBytes(value, rented);
            AppendInt32(hash, byteCount);
            hash.AppendData(rented.AsSpan(0, byteCount));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static void AppendInt32(IncrementalHash hash, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        hash.AppendData(buffer);
    }
}
