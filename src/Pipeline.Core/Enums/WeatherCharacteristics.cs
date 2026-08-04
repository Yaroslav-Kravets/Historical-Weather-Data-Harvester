// ---------------------------------------------------------------------------
// University: NTU "KhPI"
// Authors:
//   Yaroslav Kravets <beeengine1983@gmail.com>
//   ORCID: https://orcid.org/0000-0002-3893-1607
//   Iryna Liutenko <cherliv68@gmail.com>
//   ORCID: https://orcid.org/0000-0003-4357-1826
// ---------------------------------------------------------------------------

namespace Pipeline.Core.Enums;

[Flags]
public enum WeatherCharacteristics : long
{
    None = 0,
    [NameInHtml("гололед")]
    BlackIce = 1L << 0,
    [NameInHtml("град")]
    Hail = 1L << 1,
    [NameInHtml("гроза")]
    Thunderstorm = 1L << 2,
    [NameInHtml("дождь")]
    Rain = 1L << 3,
    [NameInHtml("дождь с градом")]
    RainAndHail = 1L << 4,
    [NameInHtml("дождь с грозой")]
    RainAndThunderstorm = 1L << 5,
    [NameInHtml("дождь с грозой и градом")]
    RainThunderstormAndHail = 1L << 6,
    [NameInHtml("дождь со снегом")]
    RainAndSnow = 1L << 7,
    [NameInHtml("дымка")]
    Haze = 1L << 8,
    [NameInHtml("ледяной дождь")]
    FreezingRain = 1L << 9,
    [NameInHtml("ливневый дождь")]
    ShowerRain = 1L << 10,
    [NameInHtml("ливневый дождь со снегом")]
    ShowerRainWithSnow = 1L << 11,
    [NameInHtml("мгла")]
    Mist = 1L << 12,
    [NameInHtml("мряка")]
    Drizzle = 1L << 13,
    [NameInHtml("небольшая облачность")]
    FewClouds = 1L << 14,
    [NameInHtml("переменная облачность")]
    VariableCloudiness = 1L << 15,
    [NameInHtml("песчанная буря")]
    Sandstorm = 1L << 16,
    [NameInHtml("преимущественно облачно")]
    MostlyCloudy = 1L << 17,
    [NameInHtml("преимущественно ясно")]
    MostlyClear = 1L << 18,
    [NameInHtml("сильная гроза")]
    SevereThunderstorm = 1L << 19,
    [NameInHtml("сильная снежная крупа")]
    HeavySnowPellets = 1L << 20,
    [NameInHtml("сильный дождь")]
    HeavyRain = 1L << 21,
    [NameInHtml("сильный дождь со снегом")]
    HeavyRainWithSnow = 1L << 22,
    [NameInHtml("сильный ливневый дождь")]
    HeavyShowerRain = 1L << 23,
    [NameInHtml("сильный снег")]
    HeavySnow = 1L << 24,
    [NameInHtml("слабая метель")]
    LightBlizzard = 1L << 25,
    [NameInHtml("слабая мряка")]
    LightDrizzle = 1L << 26,
    [NameInHtml("слабая снежная крупа")]
    LightSnowPellets = 1L << 27,
    [NameInHtml("слабый град")]
    LightHail = 1L << 28,
    [NameInHtml("слабый дождь")]
    LightRain = 1L << 29,
    [NameInHtml("слабый дождь с грозой")]
    LightRainWithThunderstorm = 1L << 30,
    [NameInHtml("слабый дождь со снегом")]
    LightRainWithSnow = 1L << 31,
    [NameInHtml("слабый ливневый дождь")]
    LightShowerRain = 1L << 32,
    [NameInHtml("слабый ливневый дождь со снегом")]
    LightShowerRainWithSnow = 1L << 33,
    [NameInHtml("слабый поземок")]
    LightGroundBlizzard = 1L << 34,
    [NameInHtml("слабый снег")]
    LightSnow = 1L << 35,
    [NameInHtml("слабый туман")]
    LightFog = 1L << 36,
    [NameInHtml("снег")]
    Snow = 1L << 37,
    [NameInHtml("сплошная облачность")]
    Overcast = 1L << 38,
    [NameInHtml("туман")]
    Fog = 1L << 39,
    [NameInHtml("ухудшение видимости из-за дыма")]
    ReducedVisibilityDueToSmoke = 1L << 40,
    [NameInHtml("частично облачно")]
    PartlyCloudy = 1L << 41,
    [NameInHtml("ясно")]
    Clear = 1L << 42,
    [NameInHtml("шквал")]
    Squall = 1L << 43,
    [NameInHtml("местами туман")]
    PatchyFog = 1L << 44,
    [NameInHtml("сильная метель")]
    HeavyBlizzard = 1L << 45,
    [NameInHtml("сильный ливневый дождь со снегом")]
    HeavyShowerRainWithSnow = 1L << 46,
    [NameInHtml("пылевая буря")]
    DustStorm = 1L << 47,
    [NameInHtml("осадки")]
    Precipitation = 1L << 48,
    [NameInHtml("сильный град")]
    HeavyHail = 1L << 49,
    [NameInHtml("пыль")]
    Dust = 1L << 50,
    [NameInHtml("слабая пылевая буря")]
    LightDustStorm = 1L << 51,
    [NameInHtml("поземок")]
    GroundBlizzard = 1L << 52,
}
