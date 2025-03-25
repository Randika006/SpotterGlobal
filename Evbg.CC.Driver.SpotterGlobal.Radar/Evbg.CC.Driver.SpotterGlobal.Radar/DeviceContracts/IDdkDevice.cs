using CNL.IPSecurityCenter.Driver;
using log4net;

namespace Evbg.CC.Driver.SpotterGlobal.Radar
{
    public interface IDdkDevice : IDevice
    {
        /// <summary>
        /// Gets SDK identifier, not to confuse with GUID Identifier which is a DDK Id of the device 
        /// </summary>
        string Id { get; }

        ILog Log { get; }

        T GetConnectedChildDevice<T>(string customIdentifier) where T : IDevice;

        void SetState(DeviceState state, string message);

        void RePopulateDevice<T>(string customIdentifier, T device) where T : IDevice;

        void AddAndConnectRange(InterfaceConnectionCollection connections);
    }
}