using CNL.IPSecurityCenter.Driver;
using CNL.IPSecurityCenter.Driver.ServiceLocation;
using Evbg.CC.Driver.SpotterGlobal.Radar.API;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.Spec
{
    [TestClass]
    public class DevicePopulationTests
    {
        private ConnectionManagerMock _cm;
        private IGenericApi _api;

        [TestInitialize]
        public void Initialize()
        {
            _cm = new ConnectionManagerMock();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _api = null;
            _cm = null;
        }


        [TestMethod]
        public void TestInterfaceCreationDevice()
        {
            var device = new Radar();

            //new device has to have a single Device Interface of type Other
            Assert.AreEqual(1, device.Interfaces.Count);
            Assert.AreEqual(DeviceInterfaceType.Other, device.Interfaces[0].Type);
        }

        //tests only DevicePopulation class - it should call the correct DDK methods
        [TestMethod]
        public async Task PopulateOneDevice_BasicTest()
        {
            MockApiOneDevice();

            var log = MockRepository.GenerateStub<ILog>();
            var serverDeviceMock = MockRepository.GenerateMock<IDdkDevice>();
            serverDeviceMock.Stub(x => x.Log).Return(log);
            serverDeviceMock.Stub(x => x.Interfaces).Return(new DeviceInterfaceCollection());

            //device is not populated yet
            serverDeviceMock.Stub(x => x.GetConnectedChildDevice<Radar>(Arg<string>.Is.Anything)).Return(null);

            //criteria for normal device population:
            serverDeviceMock.Expect(action => action.SetState(DeviceState.Online, Messages.PopulatingDevices)).Repeat.Once();
            serverDeviceMock.Expect(action => action.AddAndConnectRange(Arg<InterfaceConnectionCollection>.Is.NotNull));

            var devicePopulation = new DevicePopulation(serverDeviceMock, log);
            devicePopulation.Initialize(_api, 10);

            var retVal = await devicePopulation.PopulateCameraListAsync(false);

            //assert
            Assert.IsTrue(retVal);
            serverDeviceMock.VerifyAllExpectations();
        }

        [TestMethod]
        public async Task PopulateOneDevice()
        {
            var genericApiDevice = MockApiOneDevice();
            var log = MockRepository.GenerateStub<ILog>();

            //must set the Identifier otherwise CM mock Read() and Update() will not work correctly
            var serverDevice = new RadarServer() { Identifier = Guid.NewGuid() };

            var deviceRepository = MockRepository.GenerateStub<IDeviceRepository>();
            ServiceFactory.SetService(deviceRepository);

            _cm.Devices.Add(serverDevice);

            //make GetConnectedDevice() return null (child device not yet found)
            deviceRepository.Stub(x => x.GetConnectedDeviceIdentifier(Arg<Guid>.Is.Anything, Arg<string>.Is.Anything)).Return(Guid.Empty);
            deviceRepository.Stub(x => x.Read<Radar>(Arg<Guid>.Is.Anything)).Return(null);

            _cm.SubscribeToAddAndConnect(serverDevice.Interfaces);

            //act
            var devicePopulation = new DevicePopulation(serverDevice, log);
            devicePopulation.Initialize(_api, 50);
            var retVal = await devicePopulation.PopulateCameraListAsync(true);

            //assert
            Assert.IsTrue(retVal);

            //device populated criteria:
            //1 - parent device has a new Device Interface of correct type with correct custom Identifier
            Assert.AreEqual(1, serverDevice.Interfaces.Count);
            Assert.AreEqual(DeviceInterfaceType.Other, serverDevice.Interfaces[0].Type);

            var deviceCustomId = CustomIds.GetGenericCustomId(genericApiDevice.DeviceId);
            Assert.AreEqual(deviceCustomId, serverDevice.Interfaces[0].CustomIdentifier);
            Assert.AreEqual(1, _cm.AddedDeviceInterfaces.Count);
            Assert.IsTrue(_cm.AddedDeviceInterfaces.ContainsKey(deviceCustomId));

            //2 - new device is added to the CM repository
            Assert.AreEqual(2, _cm.Devices.Count);
            var device = _cm.Devices[1] as Radar;
            Assert.IsNotNull(device);
            Assert.IsTrue(IsSameAsApiDevice(device, genericApiDevice));
            Assert.AreEqual(device.ParentId, serverDevice.Identifier.ToString());

            //3 - parent device's Interface[0] is connected to the Device Interface of a new device 
            Assert.AreEqual(1, _cm.ConnectedInterfaces.Count);
            Assert.IsTrue(_cm.ConnectedInterfaces.ContainsKey(deviceCustomId));

            var connection = _cm.ConnectedInterfaces[deviceCustomId];
            Assert.AreEqual(DeviceInterfaceType.Other, connection.DeviceInterface1.Type);
            Assert.AreEqual(DeviceInterfaceType.Other, connection.DeviceInterface2.Type);
            Assert.IsNotNull(connection.DeviceInterface2.Parent);
            Assert.AreEqual(serverDevice.Identifier, connection.DeviceInterface1.Parent.Parent.Identifier);
            Assert.AreEqual(device.Identifier, connection.DeviceInterface2.Parent.Parent.Identifier);
        }

        [TestMethod]
        public async Task UpdateOneDevice()
        {
            var genericDevice = MockApiOneDevice();
            var log = MockRepository.GenerateStub<ILog>();

            //must set the Identifier
            var serverDevice = new RadarServer() { Identifier = Guid.NewGuid() };
            var device = new Radar() { Identifier = Guid.NewGuid(), Label = genericDevice.Label };
            var deviceCustomId = CustomIds.GetGenericCustomId(device.Identifier.ToString());
            var deviceRepository = MockRepository.GenerateStub<IDeviceRepository>();
            ServiceFactory.SetService(deviceRepository);

            _cm.SubscribeToPropertyChanged(deviceCustomId, device);

            //make GetConnectedDevice() return the populated device
            deviceRepository.Stub(x => x.GetConnectedDeviceIdentifier(Arg<Guid>.Is.Equal(serverDevice.Identifier), Arg<string>.Is.Equal(deviceCustomId))).Return(device.Identifier);
            deviceRepository.Stub(x => x.Read<Radar>(Arg<Guid>.Is.Equal((object)device.Identifier))).Return((Radar)device);

            //act
            //rename the device, then call population
            device.Label = "New Device";
            var devicePopulation = new DevicePopulation(serverDevice, log);
            device.SaveChangedProperties("Label");
            devicePopulation.Initialize(_api, 10);
            var retVal = await devicePopulation.PopulateCameraListAsync(true);

            //assert
            Assert.IsTrue(retVal);

            // device is updated correctly in the CM mock
            Assert.IsTrue(_cm.UpdatedDevices.ContainsKey(deviceCustomId));
            var updatedDevice = _cm.UpdatedDevices[deviceCustomId];
            Assert.AreEqual(device.Label, updatedDevice.Label);
        }

        [TestMethod]
        public async Task RepopulateDeletedDevice()
        {
            var apiDevice = MockApiOneDevice();
            var log = MockRepository.GenerateStub<ILog>();
            var deviceCustomId = CustomIds.GetGenericCustomId(apiDevice.DeviceId);

            //must set the Identifier
            var serverDeviceMock = MockRepository.GeneratePartialMock<GenericServerMock>();
            var deviceRepository = MockRepository.GenerateStub<IDeviceRepository>();
            ServiceFactory.SetService(deviceRepository);
            serverDeviceMock.DeviceRepository = deviceRepository;

            //server must have the Device Interface remaining from the pre-existing device
            var serverInput = new DeviceInterface(DeviceInterfaceType.Other, apiDevice.Label, deviceCustomId);
            serverDeviceMock.Interfaces.Add(serverInput);

            //make GetConnectedDevice() returns null (device was deleted)
            deviceRepository.Stub(x => x.GetConnectedDeviceIdentifier(Arg<Guid>.Is.Equal(serverDeviceMock.Identifier), Arg<string>.Is.Equal(deviceCustomId))).Return(Guid.Empty);
            deviceRepository.Stub(x => x.Read<Radar>(Arg<Guid>.Is.Anything)).Return(null);

            //act
            var devicePopulation = new DevicePopulation(serverDeviceMock, log);
            devicePopulation.Initialize(_api, 10);
            var retVal = await devicePopulation.PopulateCameraListAsync(true);

            //assert
            Assert.IsTrue(retVal);

            //device re-populated criteria:
            //1 - server mock must have a single device populated
            Assert.AreEqual(1, serverDeviceMock.PopulatedDevices.Count);
            var device = serverDeviceMock.PopulatedDevices[0] as Radar;
            Assert.IsNotNull(device);

            //2 - the new device has to have correct details
            Assert.IsTrue(IsSameAsApiDevice(device, apiDevice));
            Assert.AreEqual(device.ParentId, serverDeviceMock.Identifier.ToString());
        }

        [TestMethod]
        public async Task PopulateDevicesInBatches()
        {
            MockApiNDevices(100);
            var log = MockRepository.GenerateStub<ILog>();
            var serverDevice = MockRepository.GenerateMock<RadarServer>();
            var deviceRepository = MockRepository.GenerateStub<IDeviceRepository>();
            ServiceFactory.SetService(deviceRepository);

            //all calls GetConnectedDevice() return null (child device not yet found)
            deviceRepository.Stub(x => x.GetConnectedDeviceIdentifier(Arg<Guid>.Is.Anything, Arg<string>.Is.Anything)).Return(Guid.Empty);
            deviceRepository.Stub(x => x.Read<Radar>(Arg<Guid>.Is.Anything)).Return(null);

            //act
            var devicePopulation = new DevicePopulation(serverDevice, log);
            devicePopulation.Initialize(_api, 20);
            var retVal = await devicePopulation.PopulateCameraListAsync(true);

            //assert
            Assert.IsTrue(retVal);
            serverDevice.AssertWasCalled(x => x.AddAndConnectRange(Arg<InterfaceConnectionCollection>.Is.NotNull), options => options.Repeat.Times(5));
        }

        [TestMethod]
        public async Task CancelDevicePopulation_After_One_Batch()
        {
            MockApiNDevices(50);
            var log = new LogMock();
            var serverDevice = new RadarServer() { Identifier = Guid.NewGuid(), Label = "Generic Server" };
            var deviceRepository = MockRepository.GenerateStub<IDeviceRepository>();
            ServiceFactory.SetService(deviceRepository);

            _cm.Devices.Add(serverDevice);

            //all calls GetConnectedDevice() return null (child device not yet found)
            deviceRepository.Stub(x => x.GetConnectedDeviceIdentifier(Arg<Guid>.Is.Anything, Arg<string>.Is.Anything)).Return(Guid.Empty);
            deviceRepository.Stub(x => x.Read<Radar>(Arg<Guid>.Is.Anything)).Return(null);

            _cm.SubscribeToAddAndConnect(serverDevice.Interfaces, 10);

            //act
            var devicePopulation = new DevicePopulation(serverDevice, log);
            devicePopulation.Initialize(_api, 10);
            _cm.PopulatedNDevices += (sender, args) =>
            {
                devicePopulation.CancelPopulation();
            };

            var task = devicePopulation.PopulateCameraListAsync(true);

            var retVal = await task;

            //assert
            Assert.IsFalse(retVal);
            Assert.AreEqual(11, _cm.Devices.Count);
        }

        [TestMethod]
        public async Task CancelDevicePopulation()
        {
            var devices = MockApiNDevices(50);
            var log = new LogMock();
            var serverMock = MockRepository.GeneratePartialMock<GenericServerMock>();
            serverMock.Identifier = Guid.NewGuid();
            var deviceRepository = MockRepository.GenerateStub<IDeviceRepository>();
            ServiceFactory.SetService(deviceRepository);

            _cm.Devices.Add(serverMock);

            //all calls GetConnectedDevice() return null (child device not yet found)
            deviceRepository.Stub(x => x.GetConnectedDeviceIdentifier(Arg<Guid>.Is.Anything, Arg<string>.Is.Anything)).Return(Guid.Empty);
            deviceRepository.Stub(x => x.Read<Radar>(Arg<Guid>.Is.Anything)).Return(null);

            _cm.SubscribeToAddAndConnect(serverMock.Interfaces);

            //act
            var devicePopulation = new DevicePopulation(serverMock, log);
            devicePopulation.Initialize(_api, 10);

            var numberOfCalls = 0;
            serverMock.Expect(action => action.GetConnectedChildDevice<Radar>(Arg<string>.Is.Anything)).WhenCalled(_ =>
            {
                if (numberOfCalls++ == 3)
                {
                    devicePopulation.CancelPopulation();
                }
            }).Return(null).Repeat.Any();

            var task = devicePopulation.PopulateCameraListAsync(true);

            var retVal = await task;

            //assert
            Assert.IsFalse(retVal);
            Assert.AreEqual(5, _cm.Devices.Count);
            Assert.AreEqual(devices[3].DeviceId, ((Radar)_cm.Devices[4]).Id);
        }

        /// <summary>
        /// Expected outcome - the device population must be cancelled when server device is Disabled, and population must start
        /// only when the previous device population is over
        /// </summary>
        [TestMethod]
        public void QuickReEnableServerDevice_NoPopulationOverlap()
        {
            MockApiNDevices(50);
            var log = new LogMock();
            var serverMock = MockRepository.GeneratePartialMock<RadarServer>();
            serverMock.Identifier = Guid.NewGuid();
            var deviceRepository = new DeviceRepositoryMock(_cm);
            ServiceFactory.SetService(deviceRepository);


            _cm.Devices.Add(serverMock);
            _cm.SubscribeToAddAndConnect(serverMock.Interfaces, 5);

            //act
            var evt = new AutoResetEvent(false);
            var devicePopulationQueue = DevicePopulationQueue.Instance(log);
            devicePopulationQueue.Empty += (sender, args) => { evt.Set(); };

            var devicePopulation = new DevicePopulation(serverMock, log);
            devicePopulation.Initialize(_api, 10);

            var restarted = false;

            _cm.PopulatedNDevices += (sender, args) =>
            {
                //react to event only once
                if (restarted)
                {
                    return;
                }

                Task.Run(() =>
                {
                    restarted = true;

                    //this code is called when server device is Disabled
                    devicePopulation.CancelPopulation();

                    //immediately simulate server device is re-Enabled
                    devicePopulation.Initialize(_api, 10);
                    devicePopulationQueue.Enqueue(() => devicePopulation.PopulateCameraListAsync(true));
                });
                Assert.IsTrue(evt.WaitOne(1000));
                Debug.WriteLine("All tasks finished");
                Assert.AreEqual(51, _cm.Devices.Count);
            };
        }

        [TestMethod]
        public void NoPopulationOverlap_2populations_same_server()
        {
            MockApiNDevices(10);
            var log = new LogMock();
            var serverDeviceMock = MockRepository.GenerateMock<IDdkDevice>();
            serverDeviceMock.Stub(x => x.Log).Return(log);
            serverDeviceMock.Stub(x => x.Interfaces).Return(new DeviceInterfaceCollection());

            //devices are not populated yet
            serverDeviceMock.Stub(x => x.GetConnectedChildDevice<Radar>(Arg<string>.Is.Anything)).WhenCalled(_ =>
            {
                //assume this call takes some time in CM 
                Thread.Sleep(50);
            }).Return(null);

            //act
            var devicePopulation = new DevicePopulation(serverDeviceMock, log);
            devicePopulation.Initialize(_api, 50);

            //set to small timeout to 
            devicePopulation.WaitTimeoutMilliseconds = 50;
            var tasks = new List<Task>();

            var task1 = Task.Run(async () =>
            {
                Debug.WriteLine("------- Start 1st device population ------");
                var result = await devicePopulation.PopulateCameraListAsync(true);
                Debug.WriteLine($"Population 1 returned {result}");

                Assert.IsTrue(result);
            });

            tasks.Add(task1);

            var task2 = Task.Run(async () =>
            {
                await Task.Delay(100);
                Debug.WriteLine("------- Start 2nd device population ------");
                var result = await devicePopulation.PopulateCameraListAsync(true);
                Debug.WriteLine($"Population 2 returned {result}");

                //2nd population should not start as the first one is in progress
                Assert.IsFalse(result);
            });

            tasks.Add(task2);
            Task.WaitAll(tasks.ToArray());
        }

        [TestMethod]
        public void NoPopulationOverlap_1st_population_canceled()
        {
            MockApiNDevices(50);
            var log = new LogMock();
            var serverDeviceMock = MockRepository.GenerateMock<IDdkDevice>();
            serverDeviceMock.Stub(x => x.Log).Return(log);
            serverDeviceMock.Stub(x => x.Interfaces).Return(new DeviceInterfaceCollection());

            //devices are not populated yet
            serverDeviceMock.Stub(x => x.GetConnectedChildDevice<Radar>(Arg<string>.Is.Anything)).Return(null);

            //act
            var devicePopulation = new DevicePopulation(serverDeviceMock, log);
            devicePopulation.Initialize(_api, 50);
            var tasks = new List<Task>();

            var task1 = Task.Run(async () =>
            {
                var result = await devicePopulation.PopulateCameraListAsync(true);
                Debug.WriteLine($"Population 1 returned {result}");
            });

            tasks.Add(task1);

            Thread.Sleep(150);
            Debug.WriteLine("Cancelling device population..");
            devicePopulation.CancelPopulation();

            var task2 = Task.Run(async () =>
            {
                Debug.WriteLine("------- Start 2nd device population ------");
                var result = await devicePopulation.PopulateCameraListAsync(true);
                Debug.WriteLine($"Population 2 returned {result}");

                //2nd population should finish successfully
                Assert.IsTrue(result);
            });

            tasks.Add(task2);
            Task.WaitAll(tasks.ToArray());
        }


        #region API Stubs

        private GenericApiDevice MockApiOneDevice()
        {
            _api = MockRepository.GenerateStub<IGenericApi>();
            var device = new GenericApiDevice("1", "Device 1")
            {
                Altitude = 1,
                Longitude = 100,
                Latitude = 150
            };
            _api.Connected = true;
            _api.Stub(x => x.GetCameraList()).Return(Task.FromResult((IEnumerable<GenericApiDevice>)new List<GenericApiDevice> { device }));
            return device;
        }

        private IList<GenericApiDevice> MockApiNDevices(int numberOfDevices)
        {
            _api = MockRepository.GenerateStub<IGenericApi>();

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

            _api.Connected = true;
            _api.Stub(x => x.GetCameraList()).Return(Task.FromResult((IEnumerable<GenericApiDevice>)devices));

            return devices;
        }

        private bool IsSameAsApiDevice(Radar device, GenericApiDevice apiDevice)
        {
            return
                apiDevice.Altitude != null
                && device.Altitude != null
                && device.Id == apiDevice.DeviceId
                && device.Label == apiDevice.Label
                && Math.Abs(device.Latitude - apiDevice.Latitude) < 0.01f
                && Math.Abs(device.Longitude - apiDevice.Longitude) < 0.01f
                && Math.Abs(device.Altitude.Value - apiDevice.Altitude.Value) < 0.01d;
        }

        #endregion
    }
}
