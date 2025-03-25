
namespace Evbg.CC.Driver.SpotterGlobal.Radar.DeviceContracts
{
    public interface IGenericParent
    {
        RadarServer GetGenericServer(Radar d);
       // GenericServer GetGenericServer(one zone);
        RadarServer GetGenericServer(Zone.Zone zone);
        RadarServer GetGenericServer(UDC.DeviceContracts.UDC udc);
        RadarServer GetGenericServer(Camera.Camera camera);

        
    }
}
