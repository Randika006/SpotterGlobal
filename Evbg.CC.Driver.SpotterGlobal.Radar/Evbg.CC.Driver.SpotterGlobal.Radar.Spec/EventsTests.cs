using CNL.IPSecurityCenter.Driver;
using CNL.IPSecurityCenter.Driver.ServiceLocation;
using Evbg.CC.Driver.SpotterGlobal.Radar.API;
using Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs;
using Evbg.CC.Driver.SpotterGlobal.Radar.Spec.Mocks;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.Spec
{
    [TestClass]
    public class EventsTests
    {
        private ConnectionManagerMock _cm;
        private GenericApiMock _api;

        [TestInitialize]
        public void Initialize()
        {
            _cm = new ConnectionManagerMock();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _api = null;
        }

        [TestMethod]
        public void TestServerEvent()
        {
            MockApiOneDevice();

            //must set the Identifier otherwise CM mock Read() and Update() will not work correctly
            var serverDeviceMock = new GenericServerMock() { Identifier = Guid.NewGuid() };
            serverDeviceMock.InjectApiAndConnect(_api);
            var evt = new AutoResetEvent(false);

            serverDeviceMock.ServerEventEvent += (sender, args) =>
            {
                Assert.IsInstanceOfType(args, typeof(ServerEventEventArgs));
                Assert.AreEqual(args.DeviceIdentifier, serverDeviceMock.Identifier);
                evt.Set();
            };

            GenericEventArgs serverEvent = new GenericEventArgs(DateTime.Now, null);

            _api.RaiseGenericEvent(serverEvent);

            Assert.IsTrue(evt.WaitOne(1000));
        }


        [TestMethod]
        public async Task TestDeviceEvent()
        {
            var genericApiDevice = MockApiOneDevice();
            var log = MockRepository.GenerateStub<ILog>();

            //must set the Identifier otherwise CM mock Read() and Update() will not work correctly
            var serverDeviceMock = new GenericServerMock() { Identifier = Guid.NewGuid() };

            var deviceRepository = MockRepository.GenerateStub<IDeviceRepository>();
            ServiceFactory.SetService(deviceRepository);
            serverDeviceMock.DeviceRepository = deviceRepository;
            _cm.Devices.Add(serverDeviceMock);

            //make GetConnectedDevice() return null (child device not yet found)
            deviceRepository.Stub(x => x.GetConnectedDeviceIdentifier(Arg<Guid>.Is.Anything, Arg<string>.Is.Anything)).Return(Guid.Empty);
            deviceRepository.Stub(x => x.Read<Radar>(Arg<Guid>.Is.Anything)).Return(null);

            _cm.SubscribeToAddAndConnect(serverDeviceMock.Interfaces);
            serverDeviceMock.InjectApiAndConnect(_api);

            var devicePopulation = new DevicePopulation(serverDeviceMock, log);
            devicePopulation.Initialize(_api, 50);
            var retVal = await devicePopulation.PopulateCameraListAsync(true);
            serverDeviceMock.DevicesAdded.Add(_cm.Devices[1] as Radar);

            var evt = new AutoResetEvent(false);

            if (_cm.Devices[1] is Radar device)
            {
                device.DeviceEventEvent += (sender, args) =>
                {
                    Assert.IsInstanceOfType(args, typeof(DeviceEventEventArgs));
                    Assert.AreEqual(args.DeviceIdentifier, device.Identifier);
                    Assert.AreEqual(device.Id, genericApiDevice.DeviceId);
                    Assert.AreEqual(device.Label, genericApiDevice.Label);
                    Assert.IsTrue(CheckDeviceAndGeoSpatialValues(args, genericApiDevice));
                    evt.Set();
                };
            }

            GenericEventArgs deviceEventArgs = new GenericEventArgs(DateTime.Now, genericApiDevice.DeviceId, genericApiDevice.Latitude, genericApiDevice.Longitude, genericApiDevice.Altitude);
            _api.RaiseGenericEvent(deviceEventArgs);

            Assert.IsTrue(evt.WaitOne(1000));
        }

        [TestMethod]
        public async Task TestNDevicesEvent()
        {
            var genericApiDevices = MockApiNDevices(50);
            var log = MockRepository.GenerateStub<ILog>();

            //must set the Identifier otherwise CM mock Read() and Update() will not work correctly
            var serverDeviceMock = new GenericServerMock() { Identifier = Guid.NewGuid() };

            var deviceRepository = MockRepository.GenerateStub<IDeviceRepository>();
            ServiceFactory.SetService(deviceRepository);
            serverDeviceMock.DeviceRepository = deviceRepository;
            _cm.Devices.Add(serverDeviceMock);

            //make GetConnectedDevice() return null (child device not yet found)
            deviceRepository.Stub(x => x.GetConnectedDeviceIdentifier(Arg<Guid>.Is.Anything, Arg<string>.Is.Anything)).Return(Guid.Empty);
            deviceRepository.Stub(x => x.Read<Radar>(Arg<Guid>.Is.Anything)).Return(null);

            _cm.SubscribeToAddAndConnect(serverDeviceMock.Interfaces);
            serverDeviceMock.InjectApiAndConnect(_api);

            var devicePopulation = new DevicePopulation(serverDeviceMock, log);
            devicePopulation.Initialize(_api, 50);
            var retVal = await devicePopulation.PopulateCameraListAsync(true);
            for (int i = 1; i < _cm.Devices.Count; i++)
            {
                serverDeviceMock.DevicesAdded.Add(_cm.Devices[i] as Radar);
            }

            var evt1 = new AutoResetEvent(false);
            var evt2 = new AutoResetEvent(false);

            if (_cm.Devices[7] is Radar device1)
            {
                device1.DeviceEventEvent += (sender, args) =>
                {
                    Assert.IsInstanceOfType(args, typeof(DeviceEventEventArgs));
                    Assert.AreEqual(args.DeviceIdentifier, device1.Identifier);
                    Assert.AreEqual(device1.Id, genericApiDevices[6].DeviceId);
                    Assert.AreEqual(device1.Label, genericApiDevices[6].Label);
                    Assert.IsTrue(CheckDeviceAndGeoSpatialValues(args, genericApiDevices[6]));
                    evt1.Set();
                };
            }

            if (_cm.Devices[38] is Radar device2)
            {
                device2.DeviceEventEvent += (sender, args) =>
                {
                    Assert.IsInstanceOfType(args, typeof(DeviceEventEventArgs));
                    Assert.AreEqual(args.DeviceIdentifier, device2.Identifier);
                    Assert.AreEqual(device2.Id, genericApiDevices[37].DeviceId);
                    Assert.AreEqual(device2.Label, genericApiDevices[37].Label);
                    Assert.IsTrue(CheckDeviceAndGeoSpatialValues(args, genericApiDevices[37]));
                    evt2.Set();
                };
            }

            GenericEventArgs deviceEventArgs1 = new GenericEventArgs(DateTime.Now, genericApiDevices[6].DeviceId, genericApiDevices[6].Latitude, genericApiDevices[6].Longitude, genericApiDevices[6].Altitude);
            _api.RaiseGenericEvent(deviceEventArgs1);

            GenericEventArgs deviceEventArgs2 = new GenericEventArgs(DateTime.Now, genericApiDevices[37].DeviceId, genericApiDevices[37].Latitude, genericApiDevices[37].Longitude, genericApiDevices[37].Altitude);
            _api.RaiseGenericEvent(deviceEventArgs2);

            Assert.IsTrue(evt1.WaitOne(1000));
            Assert.IsTrue(evt2.WaitOne(1000));
        }


        [TestMethod]
        public async Task TestDeviceTamperStartEvent()
        {
            var genericApiDevice = MockApiOneDevice();
            var log = MockRepository.GenerateStub<ILog>();

            //must set the Identifier otherwise CM mock Read() and Update() will not work correctly
            var serverDeviceMock = new GenericServerMock() { Identifier = Guid.NewGuid() };

            var deviceRepository = MockRepository.GenerateStub<IDeviceRepository>();
            ServiceFactory.SetService(deviceRepository);
            serverDeviceMock.DeviceRepository = deviceRepository;
            _cm.Devices.Add(serverDeviceMock);

            //make GetConnectedDevice() return null (child device not yet found)
            deviceRepository.Stub(x => x.GetConnectedDeviceIdentifier(Arg<Guid>.Is.Anything, Arg<string>.Is.Anything)).Return(Guid.Empty);
            deviceRepository.Stub(x => x.Read<Radar>(Arg<Guid>.Is.Anything)).Return(null);


            _cm.SubscribeToAddAndConnect(serverDeviceMock.Interfaces);
            serverDeviceMock.InjectApiAndConnect(_api);

            var devicePopulation = new DevicePopulation(serverDeviceMock, log);
            devicePopulation.Initialize(_api, 50);
            var retVal = await devicePopulation.PopulateCameraListAsync(true);
            serverDeviceMock.DevicesAdded.Add(_cm.Devices[1] as Radar);

            var evtTamper = new AutoResetEvent(false);
            var evtState = new AutoResetEvent(false);

            if (_cm.Devices[1] is Radar device)
            {
                GenericParentMock genericParentMock = new GenericParentMock(serverDeviceMock);
                device.InjectGetServerInterface(genericParentMock);
                device.Enabled = true;
                device.TamperEvent += (sender, args) =>
                {
                    Assert.IsInstanceOfType(args, typeof(GenericTamperEventArgs));
                    //Assert.AreEqual(args.DeviceIdentifier, device.Identifier);
                    Assert.AreEqual(device.Id, genericApiDevice.DeviceId);
                    Assert.AreEqual(device.Label, genericApiDevice.Label);
                    Assert.IsTrue(CheckDeviceAndGeoSpatialValues(args, genericApiDevice));
                   // Assert.IsTrue(condition: args.Description == Events.TamperOn);
                    evtTamper.Set();
                };

                device.CustomStateChanged += (sender, args) =>
                {
                    Assert.IsInstanceOfType(args, typeof(CustomStateChangedEventArgs));
                    Assert.AreEqual(args.DeviceIdentifier, device.Identifier);
                    Assert.IsTrue(args.Message == Events.TamperStart);
                    Assert.IsInstanceOfType(args.CustomState, typeof(SampleState));
                    evtState.Set();
                };
            }

            genericApiDevice.IsTamper = true;
            GenericTamperEventArgs tamperEventArgs = new GenericTamperEventArgs(DateTime.Now, genericApiDevice.DeviceId, Events.TamperOn, GenericAlarmStatus.Start, genericApiDevice.Latitude, genericApiDevice.Longitude, genericApiDevice.Altitude);
            _api.RaiseTamper(tamperEventArgs);

            Assert.IsTrue(evtTamper.WaitOne(1000));
            Assert.IsTrue(evtState.WaitOne(1000));
        }

        private bool CheckDeviceAndGeoSpatialValues(object args, GenericApiDevice genericApiDevice)
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public async Task TestDeviceTamperEndEvent()
        {
            var genericApiDevice = MockApiOneDevice();
            var log = MockRepository.GenerateStub<ILog>();

            //must set the Identifier otherwise CM mock Read() and Update() will not work correctly
            var serverDeviceMock = new GenericServerMock() { Identifier = Guid.NewGuid() };

            var deviceRepository = MockRepository.GenerateStub<IDeviceRepository>();
            ServiceFactory.SetService(deviceRepository);
            serverDeviceMock.DeviceRepository = deviceRepository;
            _cm.Devices.Add(serverDeviceMock);

            //make GetConnectedDevice() return null (child device not yet found)
            deviceRepository.Stub(x => x.GetConnectedDeviceIdentifier(Arg<Guid>.Is.Anything, Arg<string>.Is.Anything)).Return(Guid.Empty);
            deviceRepository.Stub(x => x.Read<Radar>(Arg<Guid>.Is.Anything)).Return(null);

            _cm.SubscribeToAddAndConnect(serverDeviceMock.Interfaces);
            serverDeviceMock.InjectApiAndConnect(_api);

            var devicePopulation = new DevicePopulation(serverDeviceMock, log);
            devicePopulation.Initialize(_api, 50);
            var retVal = await devicePopulation.PopulateCameraListAsync(true);
            serverDeviceMock.DevicesAdded.Add(_cm.Devices[1] as Radar);

            var evtTamper = new AutoResetEvent(false);
            var evtState = new AutoResetEvent(false);

            if (_cm.Devices[1] is Radar device)
            {
                GenericParentMock genericParentMock = new GenericParentMock(serverDeviceMock);
                device.InjectGetServerInterface(genericParentMock);
                device.Enabled = true;
                device.TamperEvent += (sender, args) =>
                {
                    //Assert.IsInstanceOfType(args, typeof(TamperEventArgs));
                    //Assert.AreEqual(args.DeviceIdentifier, device.Identifier);
                    Assert.AreEqual(device.Id, genericApiDevice.DeviceId);
                    Assert.AreEqual(device.Label, genericApiDevice.Label);
                    Assert.IsTrue(CheckDeviceAndGeoSpatialValues(args, genericApiDevice));
                    //Assert.IsTrue(args.Description == Events.TamperOff);
                    evtTamper.Set();
                };

                device.StateChanged += (sender, args) =>
                {
                    Assert.IsInstanceOfType(args, typeof(DeviceStateChangedEventArgs));
                    Assert.AreEqual(args.DeviceIdentifier, device.Identifier);
                    Assert.IsTrue(args.Message == string.Empty);
                    evtState.Set();
                };
            }
            genericApiDevice.IsTamper = false;
            GenericTamperEventArgs tamperEventArgs = new GenericTamperEventArgs(DateTime.Now, genericApiDevice.DeviceId, Events.TamperOff, GenericAlarmStatus.End, genericApiDevice.Latitude, genericApiDevice.Longitude, genericApiDevice.Altitude);
            _api.RaiseTamper(tamperEventArgs);

            Assert.IsTrue(evtTamper.WaitOne(1000));
            Assert.IsTrue(evtState.WaitOne(1000));
        }

        private GenericApiDevice MockApiOneDevice()
        {
            var device = new GenericApiDevice("1", "Device 1")
            {
                Altitude = 1,
                Longitude = 100,
                Latitude = 150
            };
            _api = new GenericApiMock("127.0.0.1", 0, new List<GenericApiDevice> { device });
            return device;
        }

        private IList<GenericApiDevice> MockApiNDevices(int numberOfDevices)
        {
            var devices = new List<GenericApiDevice>();
            for (var i = 1; i <= numberOfDevices; i++)
            {
                devices.Add(new GenericApiDevice(i.ToString(), "Device " + i)
                {
                    Altitude = i,
                    Longitude = 100 + i,
                    Latitude = 150 + i
                });
            }
            _api = new GenericApiMock("127.0.0.1", 0, devices);
            return devices;
        }

        private bool CheckDeviceAndGeoSpatialValues(GeospatialAwareEventEventArgs eventArgs, GenericApiDevice apiDevice)
        {
            return
                (eventArgs.Latitude.HasValue && Math.Abs(eventArgs.Latitude.Value - apiDevice.Latitude) < 0.01f)
                && (eventArgs.Longitude.HasValue && Math.Abs(eventArgs.Longitude.Value - apiDevice.Longitude) < 0.01f)
                && (!apiDevice.Altitude.HasValue || (eventArgs.Altitude.HasValue && Math.Abs(eventArgs.Altitude.Value - apiDevice.Altitude.Value) < 0.01d));
        }
    }
}
