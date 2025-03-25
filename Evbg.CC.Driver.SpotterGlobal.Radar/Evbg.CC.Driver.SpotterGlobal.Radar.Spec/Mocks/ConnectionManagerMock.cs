using CNL.IPSecurityCenter.Driver;
using Evbg.CC.Driver.SpotterGlobal.Radar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.Spec
{
    public class ConnectionManagerMock
    {
        private int _maximumDevices;

        //using List rather than a Dictionary to mock a Database where devices can be doubled (in case of a bug)
        public IList<IDevice> Devices { get; }
        public Dictionary<string, bool> AddedDeviceInterfaces { get; private set; }
        public Dictionary<string, MultipleConnectionEventArgs.ConnectionDetails> ConnectedInterfaces { get; private set; }
        public Dictionary<string, IDevice> UpdatedDevices { get; private set; }

        public event EventHandler PopulatedNDevices;

        public ConnectionManagerMock()
        {
            Devices = new List<IDevice>();
            Init();
        }

        private void Init()
        {
            AddedDeviceInterfaces = new Dictionary<string, bool>();
            ConnectedInterfaces = new Dictionary<string, MultipleConnectionEventArgs.ConnectionDetails>();
            UpdatedDevices = new Dictionary<string, IDevice>();

            _maximumDevices = -1;
        }

        public void SubscribeToAddAndConnect(DeviceInterfaceCollection interfaces)
        {
            //Called when driver adds and connects a new device
            interfaces.ItemsAdded += (sender, args) =>
            {
                foreach (var iface in args.Items)
                {
                    AddedDeviceInterfaces[iface.CustomIdentifier] = true;
                }
            };

            interfaces.Connection += OnDeviceInterfaceObserverConnection;
        }

        /// <summary>
        /// Update cache when given device interfaces are updated, raise PopulatedNDevices event when the target number of devices is reached
        /// </summary>
        public void SubscribeToAddAndConnect(DeviceInterfaceCollection interfaces, int targetNumberOfDevices)
        {
            //Called when driver adds and connects a new device
            interfaces.ItemsAdded += (sender, args) =>
            {
                foreach (var iface in args.Items)
                {
                    AddedDeviceInterfaces[iface.CustomIdentifier] = true;
                }
            };

            interfaces.Connection += OnDeviceInterfaceObserverConnection;
            _maximumDevices = targetNumberOfDevices;
        }

        public void SubscribeToPropertyChanged<T>(string customIdentifier, T device)
            where T : Radar
        {
            device.PropertyChanged += (sender, args) => { UpdatedDevices[customIdentifier] = (IDevice)sender; };
        }

        protected void OnPopulatedNDevices(object sender, EventArgs args)
        {
            PopulatedNDevices?.Invoke(sender, args);
        }

        private void OnDeviceInterfaceObserverConnection(object sender, MultipleConnectionEventArgs e)
        {
            IDevice parentDevice = null;
            var devicesToAdd = new List<IDevice>();
            var devicesToSave = new List<IDevice>();

            foreach (var connection in e.Connections)
            {
                ConnectedInterfaces[connection.DeviceInterface1.CustomIdentifier] = connection;

                if (parentDevice == null)
                {
                    parentDevice = ReadDevice(connection.DeviceInterface1.Parent.Parent.Identifier);
                    if (parentDevice == null)
                    {
                        //parent device is corrupted and cannot be read - don't connect devices to it
                        Debug.WriteLine($"Failed to connect device1 Interface '{connection.DeviceInterface1}' to device Interface '{connection.DeviceInterface2}': failed to read parent device");
                        continue;
                    }

                    devicesToSave.Add(connection.DeviceInterface1.Parent.Parent);
                }

                var deviceInterface2Parent = connection.DeviceInterface2.Parent.Parent;
                var childDevice = ReadDevice(deviceInterface2Parent.Identifier);

                if (childDevice == null)
                {
                    if (deviceInterface2Parent.Identifier.Equals(Guid.Empty))
                    {
                        deviceInterface2Parent.Identifier = Guid.NewGuid();
                    }

                    devicesToAdd.Add(deviceInterface2Parent);
                }
                else
                {
                    devicesToSave.Add(deviceInterface2Parent);
                }
            }

            foreach (var device in devicesToAdd)
            {
                Debug.WriteLine($"Add device '{device.Label}' [{device.Identifier}]");
                Devices.Add(device);

                if (_maximumDevices > 0 && Devices.Count >= _maximumDevices)
                {
                    _maximumDevices = -1;
                    OnPopulatedNDevices(this, EventArgs.Empty);
                }
            }

            foreach (var device in devicesToSave)
            {
                UpdateDevice(device);
            }
        }

        public IDevice ReadDevice(Guid id) => Devices.FirstOrDefault(d => d.Identifier == id);

        public void UpdateDevice(IDevice device)
        {
            Debug.WriteLine($"Update device '{device.Label}' [{device.Identifier}]");

            lock (Devices)
            {
                for (var i = 0; i < Devices.Count; i++)
                {
                    if (Devices[i].Identifier == device.Identifier)
                    {
                        Debug.WriteLine($"Device '{device.Label}' [{device.Identifier}] updated");
                        Devices[i] = device;
                        break;
                    }
                }
            }
        }
    }
}