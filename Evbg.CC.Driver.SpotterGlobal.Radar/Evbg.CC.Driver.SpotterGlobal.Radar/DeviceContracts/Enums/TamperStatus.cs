using CNL.IPSecurityCenter.Driver.Attributes;

namespace Evbg.CC.Driver.SpotterGlobal.Radar
{
    [Description("Tamper Status")]
    public enum TamperStatus
    {
        /// <summary>
        /// Tamper Started.
        /// </summary>
        Start,

        /// <summary>
        /// Tamper Ended.
        /// </summary>
        End
    }
}
