using CNL.IPSecurityCenter.Driver;

namespace Evbg.CC.Driver.SpotterGlobal.Radar
{
    /// <summary>
    /// Provides default values for the device class.
    /// </summary>
    internal static class DeviceDefaults
    {
        /// <summary>
        /// Returns the default port value.
        /// </summary>
        /// <param name="device">device implementing INetworkedDevice interface</param>
        /// <returns>Default port device value or the current .Port property value is not 0</returns>
        public static int DefaultPort(INetworkedDevice device)
        {
            //TODO: set the relevant default port value
            const int Port = 443;
            return device.Port != 0 ? device.Port : Port;
        }

        public static int DefaultKeepAliveInterval(IRadarServer device)
        {
            const int DefaultKeepAliveIntervalSec = 10;

            return device.KeepAliveInterval != 0 ? device.KeepAliveInterval : DefaultKeepAliveIntervalSec;
        }

        public static int DefaultDevicePopulationBatchSize(IRadarServer device)
        {
            const int DefaultChildDevicePopulationBatchSize = 50;
            return device.DevicePopulationBatchSize != 0 ? device.DevicePopulationBatchSize : DefaultChildDevicePopulationBatchSize;
        }
    }
}
