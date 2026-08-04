// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Tests.Enums;

internal static class PlaceTestData
{
    internal static readonly (Place Place, string NameInHtml, string DisplayName)[] AllRows =
    {
        (Place.Hremiach, "Гремяче", "Hremiach"),
        (Place.Sevastopol, "Севастополе", "Sevastopol"),
        (Place.Solomonove, "Соломоновом", "Solomonove"),
        (Place.ChervonaZirka, "Червоной", "Chervona Zirka"),
        (Place.Hoverla, "Говерле", "Hoverla"),
        (Place.Kuyalnyk, "Куяльнике", "Kuyalnyk"),
        (Place.Mariupol, "Мариуполе", "Mariupol"),
        (Place.Sloviansk, "Славянске", "Sloviansk"),
        (Place.IvanoFrankivsk, "Ивано-Франковске", "Ivano-Frankivsk"),
        (Place.Zhytomyr, "Житомире", "Zhytomyr"),
        (Place.Lviv, "Львове", "Lviv"),
        (Place.Ternopil, "Тернополе", "Ternopil"),
        (Place.Kyiv, "Киеве", "Kyiv"),
        (Place.Simferopol, "Симферополе", "Simferopol"),
        (Place.Donetsk, "Донецке", "Donetsk"),
        (Place.Luhansk, "Луганске", "Luhansk"),
        (Place.Odesa, "Одессе", "Odesa"),
        (Place.Kharkiv, "Харькове", "Kharkiv"),
        (Place.Uzhhorod, "Ужгороде", "Uzhhorod"),
    };
}
