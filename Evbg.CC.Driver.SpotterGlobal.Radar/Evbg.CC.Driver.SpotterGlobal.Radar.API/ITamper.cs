
namespace Evbg.CC.Driver.SpotterGlobal.Radar.API
{
    public interface ITamper
    {
        /// <summary>
        /// Returns true if there is at least one tamper on the device, returns false if there are no tampers on the device
        /// </summary>
        bool IsTamper { get; set; }
    }
}
