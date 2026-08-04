// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Converters;

using Common;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

public sealed class WeatherCharacteristicsEnglishCsvConverter : DefaultTypeConverter
{
    private readonly WeatherCharacteristicConverter converter;

    public WeatherCharacteristicsEnglishCsvConverter(WeatherCharacteristicConverter converter)
    {
        Argument.ThrowIfNull(converter);

        this.converter = converter;
    }

    /// <inheritdoc/>
    public override string? ConvertToString(
        object? value,
        IWriterRow row,
        MemberMapData memberMapData) =>
        value is WeatherCharacteristics characteristics
            ? this.converter.ToEnglishCsvCell(characteristics)
            : base.ConvertToString(value, row, memberMapData);

    /// <inheritdoc/>
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
        this.converter.FromEnglishCsvCell(text);
}
