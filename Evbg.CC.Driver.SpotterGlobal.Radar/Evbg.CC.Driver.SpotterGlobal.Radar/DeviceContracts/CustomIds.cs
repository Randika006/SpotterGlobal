using System.Globalization;

namespace Evbg.CC.Driver.SpotterGlobal.Radar
{
    public static class CustomIds
    {
        public const string CustomIdGeneric = "Generic";
        public const string CustomIdConnector = "G";

        public static string GetGenericCustomId(string id)
        {
            return $"{CustomIdGeneric}-{CustomIdConnector}-{id}".ToString(CultureInfo.InvariantCulture);
        }
    }
}