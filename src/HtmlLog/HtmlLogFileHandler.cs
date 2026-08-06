// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace HtmlLog;

using System.IO.Abstractions;
using System.Text;
using Common;

/// <summary>
/// Handles file operations for a specific HTML summary file.
/// </summary>
internal sealed class HtmlLogFileHandler : IDisposable
{
    /// <summary>
    /// Exact footer opening markup written by <see cref="WriteFooter"/>; used to insert
    /// additional sections into a closed HTML report before the footer.
    /// </summary>
    internal const string FooterStartMarker = "        <div class=\"footer\">";

    private readonly StreamWriter writer;
    private readonly string filePath;
    private readonly string title;
    private readonly object lockObject = new object();
    private bool isDisposed;
    private bool isHeaderWritten;
    private bool isFooterWritten;

    public HtmlLogFileHandler(IFileSystem fileSystem, string filePath, string title)
    {
        Argument.ThrowIfNull(fileSystem);
        Argument.ThrowIfNull(filePath);
        Argument.ThrowIfNull(title);
        this.filePath = filePath;
        this.title = title;

        var directory = fileSystem.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !fileSystem.Directory.Exists(directory))
        {
            fileSystem.Directory.CreateDirectory(directory);
        }

        // FileShare.Read matches prior StreamWriter(path) behavior so viewers can open the report while writing.
        this.writer = new StreamWriter(
            fileSystem.File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read),
            Encoding.UTF8);
        this.WriteHeader();
    }

    public void WriteLine(string line)
    {
        if (this.isDisposed || this.isFooterWritten)
        {
            return;
        }

        lock (this.lockObject)
        {
            if (this.isDisposed || this.isFooterWritten)
            {
                return;
            }

            this.writer.WriteLine(line);
            this.writer.Flush();
        }
    }

    public void Write(string text)
    {
        if (this.isDisposed || this.isFooterWritten)
        {
            return;
        }

        lock (this.lockObject)
        {
            if (this.isDisposed || this.isFooterWritten)
            {
                return;
            }

            this.writer.Write(text);
            this.writer.Flush();
        }
    }

    public void WriteFooter()
    {
        if (this.isFooterWritten)
        {
            return;
        }

        lock (this.lockObject)
        {
            if (this.isFooterWritten)
            {
                return;
            }

            this.writer.WriteLine($@"{FooterStartMarker}
            End of summary report
        </div>
    </div>
</body>
</html>");
            this.writer.Flush();
            this.isFooterWritten = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!this.isDisposed)
        {
            lock (this.lockObject)
            {
                if (!this.isDisposed)
                {
                    this.WriteFooter();
                    this.writer.Dispose();
                    this.isDisposed = true;
                }
            }
        }
    }

    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private void WriteHeader()
    {
        if (this.isHeaderWritten)
        {
            return;
        }

        lock (this.lockObject)
        {
            if (this.isHeaderWritten)
            {
                return;
            }

            this.writer.WriteLine(@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>" + EscapeHtml(this.title) + @"</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background-color: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            overflow: hidden;
        }
        h1 {
            background-color: #2c3e50;
            color: white;
            margin: 0;
            padding: 20px;
            font-size: 24px;
        }
        .metadata {
            padding: 15px 20px;
            background-color: #ecf0f1;
            border-bottom: 1px solid #bdc3c7;
            font-size: 14px;
            color: #34495e;
        }
        .footer {
            padding: 15px 20px;
            background-color: #ecf0f1;
            text-align: center;
            color: #7f8c8d;
            font-size: 12px;
        }
        .table-container {
            margin: 20px;
            padding: 20px;
            background-color: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .table-title {
            font-size: 18px;
            font-weight: 600;
            color: #2c3e50;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 2px solid #34495e;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }
        th {
            background-color: #34495e;
            color: white;
            padding: 12px;
            text-align: left;
            font-weight: 600;
            border: 1px solid #2c3e50;
        }
        td {
            padding: 10px 12px;
            border: 1px solid #ecf0f1;
        }
        .numeric {
            text-align: right;
        }
        tr:nth-child(even) {
            background-color: #f8f9fa;
        }
        tr:hover {
            background-color: #e8f4f8;
        }
        .image-container {
            margin: 20px;
            padding: 20px;
            background-color: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            text-align: center;
        }
        .image-container img {
            max-width: 100%;
            height: auto;
            border: 1px solid #ecf0f1;
            border-radius: 4px;
            display: block;
            margin: 0 auto;
        }
        .image-caption {
            margin-top: 10px;
            font-size: 14px;
            color: #34495e;
            font-style: italic;
            text-align: center;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>" + EscapeHtml(this.title) + @"</h1>
        <div class=""metadata"">
            Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"<br>
            File: " + EscapeHtml(this.filePath) + @"
        </div>");

            this.isHeaderWritten = true;
            this.writer.Flush();
        }
    }
}
