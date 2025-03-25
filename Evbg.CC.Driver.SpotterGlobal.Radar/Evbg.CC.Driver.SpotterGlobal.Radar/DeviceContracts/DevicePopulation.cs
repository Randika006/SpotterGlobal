using CNL.IPSecurityCenter.Driver;
using Evbg.CC.Driver.SpotterGlobal.Radar.API;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar
{
    public class DevicePopulation : IDisposable
    {
        private const int DefaultWaitTimeoutMilliseconds = 20000;
        private readonly IDdkDevice _parentDevice;
        private readonly ILog _log;
        private static readonly SemaphoreSlim DevicePopulationLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _devicePopulationCancellationTokenSource;
        private IGenericApi _api;

        public int BatchSize { get; set; }
        public int WaitTimeoutMilliseconds { get; set; } = DefaultWaitTimeoutMilliseconds;

        public bool Cancelled => _devicePopulationCancellationTokenSource == null || _devicePopulationCancellationTokenSource.IsCancellationRequested;

        public DevicePopulation(IDdkDevice parentDevice, ILog log)
        {
            _log = log;
            _parentDevice = parentDevice;
        }

        public void Initialize(IGenericApi api, int batchSize)
        {
            _api = api;
            BatchSize = batchSize;
        }

        /// <summary>
        /// Populate the generic devices
        /// </summary>
        /// <param name="addedDeviceCustomIdentifiers">custom identifiers</param>
        /// <param name="cancellationToken">token for cancellation</param>
        public async Task<bool> PopulateRadarListAsync(bool refreshProperties)
        {

            if (_api == null || !_api.Connected)
            {
                _log.Warn($"'{_parentDevice.Label}':Cannot populate devices - not connected to the server");
                return false;
            }

            _devicePopulationCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _devicePopulationCancellationTokenSource.Token;

            cancellationToken.ThrowIfCancellationRequested();

            if (!await DevicePopulationLock.WaitAsync(WaitTimeoutMilliseconds, cancellationToken))
            {
                _log.Warn($"'{_parentDevice.Label}': Another device population in progress");
                _devicePopulationCancellationTokenSource.Dispose();
                _parentDevice.SetState(DeviceState.Failed, Messages.FailedToPopulatePanels);
                return false;
            }
            _log.Debug($"'{_parentDevice.Label}': Populate Generic devices");

            try
            {
                await Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _parentDevice.SetState(DeviceState.Online, Messages.PopulatingDevices);

                    //Known Devices Ids
                    var knownDevicesCustomIds = new HashSet<string>();

                    //populate Generic devices
                    var newConnections = new InterfaceConnectionCollection();
                    var devicesAdded = 0;

                    ////TODO: get the devices from the API
                    var devices = await _api.GetRadarList();

                    foreach (var genericApiDevice in devices)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            //stop the loop if population is cancelled to populate whatever 
                            break;
                        }
                        var customIdentifier = CustomIds.GetGenericCustomId(genericApiDevice.DeviceId);

                        if (knownDevicesCustomIds.Contains(customIdentifier))
                        {
                            _log.Warn($"'{_parentDevice.Label}': Skipping device with Custom ID '{customIdentifier}' - this custom is already listed");
                            continue;
                        }

                        knownDevicesCustomIds.Add(customIdentifier);

                        var device = _parentDevice.GetConnectedChildDevice<Radar>(customIdentifier);
                        if (device != null)
                        {
                            //device already exists in Control Center
                            _log.Debug($"'{_parentDevice.Label}': Updating device device with Custom ID '{customIdentifier}', '{genericApiDevice.Label}'");
                            device.UpdateProperties();
                            device.SetState(DeviceState.Online);
                        }
                        else
                        {
                            _log.Info($"'{_parentDevice.Label}': Populating device device, [{genericApiDevice.DeviceId}] '{genericApiDevice.Label}'");

                            try
                            {
                                device = new Radar 
                                {
                                    //important: make sure this is never empty
                                    Label = genericApiDevice.Label,
                                    Id = genericApiDevice.DeviceId,
                                    Latitude = genericApiDevice.Latitude,
                                    Longitude = genericApiDevice.Longitude,
                                    Altitude = genericApiDevice.Altitude,
                                    ParentId = _parentDevice.Identifier.ToString()
                                };

                                if (!_parentDevice.Interfaces.Contains(customIdentifier))
                                {
                                    AddToConnections(customIdentifier, device, DeviceInterfaceType.Other, newConnections);
                                    devicesAdded++;
                                }
                                else
                                {
                                    _parentDevice.RePopulateDevice(customIdentifier, device);
                                }
                            }
                            catch (ArgumentException ex)
                            {
                                _log.Error($"'{_parentDevice.Label}': Failed to populate device '{customIdentifier}' - {ex.Message}", ex);
                            }
                            catch (InvalidOperationException ex)
                            {
                                _log.Error($"'{_parentDevice.Label}': Failed to populate device '{customIdentifier}' - {ex.Message}", ex);
                            }
                        }

                        try
                        {
                            //Populating devices in batches. Populating one by one is too slow, populating all at once is likely to result in SQL Timeout exception. Recommended batch size is 50 - 100.
                            if (devicesAdded >= BatchSize)
                            {
                                _parentDevice.AddAndConnectRange(newConnections);
                                newConnections = new InterfaceConnectionCollection();
                                devicesAdded = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"'{_parentDevice.Label}': Failed to populate devices: {ex.Message}", ex);

                            //clear the collection and reset the counter otherwise subsequent population attempts fail as well
                            newConnections = new InterfaceConnectionCollection();
                            devicesAdded = 0;
                        }
                    }

                    try
                    {
                        //Populating devices if their number is less than a batch size.
                        if (devicesAdded > 0)
                        {
                            _parentDevice.AddAndConnectRange(newConnections);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"'{_parentDevice.Label}': Failed to populate devices: {ex.Message}", ex);
                    }
                    UpdateOrphanedRadarDevices(knownDevicesCustomIds, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                },
                cancellationToken);

                return true;
            }
            catch (OperationCanceledException ex)
            {
                _log.Warn($"Populating of generic devices was cancelled: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _log.Warn($"Populating generic devices has failed: {ex.Message}", ex);
                return false;
            }
            finally
            {
                _devicePopulationCancellationTokenSource.Dispose();
                DevicePopulationLock?.Release();
                _parentDevice.SetState(DeviceState.Online, string.Empty);
            }
        }



        public async Task<bool> PopulateCameraListAsync(bool refreshProperties)
        {

            if (_api == null || !_api.Connected)
            {
                _log.Warn($"'{_parentDevice.Label}':Cannot populate devices - not connected to the server");
                return false;
            }

            _devicePopulationCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _devicePopulationCancellationTokenSource.Token;

            cancellationToken.ThrowIfCancellationRequested();

            if (!await DevicePopulationLock.WaitAsync(WaitTimeoutMilliseconds, cancellationToken))
            {
                _log.Warn($"'{_parentDevice.Label}': Another device population in progress");
                _devicePopulationCancellationTokenSource.Dispose();
                _parentDevice.SetState(DeviceState.Failed, Messages.FailedToPopulatePanels);
                return false;
            }
            _log.Debug($"'{_parentDevice.Label}': Populate Generic devices");

            try
            {
                await Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _parentDevice.SetState(DeviceState.Online, Messages.PopulatingDevices);

                    //Known Devices Ids
                    var knownDevicesCustomIds = new HashSet<string>();

                    //populate Generic devices
                    var newConnections = new InterfaceConnectionCollection();
                    var devicesAdded = 0;

                    ////TODO: get the devices from the API
                    var devices = await _api.GetCameraList();

                    foreach (var genericApiDevice in devices)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            //stop the loop if population is cancelled to populate whatever 
                            break;
                        }
                        var customIdentifier = CustomIds.GetGenericCustomId(genericApiDevice.DeviceId);

                        if (knownDevicesCustomIds.Contains(customIdentifier))
                        {
                            _log.Warn($"'{_parentDevice.Label}': Skipping device with Custom ID '{customIdentifier}' - this custom is already listed");
                            continue;
                        }

                        knownDevicesCustomIds.Add(customIdentifier);

                        var device = _parentDevice.GetConnectedChildDevice<Camera.Camera>(customIdentifier);
                        if (device != null)
                        {
                            //device already exists in Control Center
                            _log.Debug($"'{_parentDevice.Label}': Updating device device with Custom ID '{customIdentifier}', '{genericApiDevice.Label}'");
                            device.UpdateProperties();
                            device.SetState(DeviceState.Online);
                        }
                        else
                        {
                            _log.Info($"'{_parentDevice.Label}': Populating device device, [{genericApiDevice.DeviceId}] '{genericApiDevice.Label}'");

                            try
                            {
                                device = new Camera.Camera
                                {
                                    //important: make sure this is never empty
                                    Label = genericApiDevice.Label,
                                    Id = genericApiDevice.DeviceId,
                                    Latitude = genericApiDevice.Latitude,
                                    Longitude = genericApiDevice.Longitude,
                                    Altitude = genericApiDevice.Altitude,
                                    ParentId = _parentDevice.Identifier.ToString()
                                };

                                if (!_parentDevice.Interfaces.Contains(customIdentifier))
                                {
                                    AddToConnections(customIdentifier, device, DeviceInterfaceType.Other, newConnections);
                                    devicesAdded++;
                                }
                                else
                                {
                                    _parentDevice.RePopulateDevice(customIdentifier, device);
                                }
                            }
                            catch (ArgumentException ex)
                            {
                                _log.Error($"'{_parentDevice.Label}': Failed to populate device '{customIdentifier}' - {ex.Message}", ex);
                            }
                            catch (InvalidOperationException ex)
                            {
                                _log.Error($"'{_parentDevice.Label}': Failed to populate device '{customIdentifier}' - {ex.Message}", ex);
                            }
                        }

                        try
                        {
                            //Populating devices in batches. Populating one by one is too slow, populating all at once is likely to result in SQL Timeout exception. Recommended batch size is 50 - 100.
                            if (devicesAdded >= BatchSize)
                            {
                                _parentDevice.AddAndConnectRange(newConnections);
                                newConnections = new InterfaceConnectionCollection();
                                devicesAdded = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"'{_parentDevice.Label}': Failed to populate devices: {ex.Message}", ex);

                            //clear the collection and reset the counter otherwise subsequent population attempts fail as well
                            newConnections = new InterfaceConnectionCollection();
                            devicesAdded = 0;
                        }
                    }

                    try
                    {
                        //Populating devices if their number is less than a batch size.
                        if (devicesAdded > 0)
                        {
                            _parentDevice.AddAndConnectRange(newConnections);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"'{_parentDevice.Label}': Failed to populate devices: {ex.Message}", ex);
                    }
                    UpdateOrphanedCameraDevices(knownDevicesCustomIds, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                },
                cancellationToken);

                return true;
            }
            catch (OperationCanceledException ex)
            {
                _log.Warn($"Populating of generic devices was cancelled: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _log.Warn($"Populating generic devices has failed: {ex.Message}", ex);
                return false;
            }
            finally
            {
                _devicePopulationCancellationTokenSource.Dispose();
                DevicePopulationLock?.Release();
                _parentDevice.SetState(DeviceState.Online, string.Empty);
            }
        }

        public async Task<bool> PopulateZoneListAsync(bool refreshProperties)
        {

            if (_api == null || !_api.Connected)
            {
                _log.Warn($"'{_parentDevice.Label}':Cannot populate devices - not connected to the server");
                return false;
            }

            _devicePopulationCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _devicePopulationCancellationTokenSource.Token;

            cancellationToken.ThrowIfCancellationRequested();

            if (!await DevicePopulationLock.WaitAsync(WaitTimeoutMilliseconds, cancellationToken))
            {
                _log.Warn($"'{_parentDevice.Label}': Another device population in progress");
                _devicePopulationCancellationTokenSource.Dispose();
                _parentDevice.SetState(DeviceState.Failed, Messages.FailedToPopulatePanels);
                return false;
            }
            _log.Debug($"'{_parentDevice.Label}': Populate Generic devices");

            try
            {
                await Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _parentDevice.SetState(DeviceState.Online, Messages.PopulatingDevices);

                    //Known Devices Ids
                    var knownDevicesCustomIds = new HashSet<string>();

                    //populate Generic devices
                    var newConnections = new InterfaceConnectionCollection();
                    var devicesAdded = 0;

                    ////TODO: get the devices from the API
                    var devices = await _api.GetZoneList();

                    foreach (var genericApiDevice in devices)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            //stop the loop if population is cancelled to populate whatever 
                            break;
                        }
                        var customIdentifier = CustomIds.GetGenericCustomId(genericApiDevice.DeviceId);

                        if (knownDevicesCustomIds.Contains(customIdentifier))
                        {
                            _log.Warn($"'{_parentDevice.Label}': Skipping device with Custom ID '{customIdentifier}' - this custom is already listed");
                            continue;
                        }

                        knownDevicesCustomIds.Add(customIdentifier);

                        var device = _parentDevice.GetConnectedChildDevice<Zone.Zone>(customIdentifier);
                        if (device != null)
                        {
                            //device already exists in Control Center
                            _log.Debug($"'{_parentDevice.Label}': Updating device device with Custom ID '{customIdentifier}', '{genericApiDevice.Label}'");
                            device.UpdateProperties();
                            device.SetState(DeviceState.Online);
                        }
                        else
                        {
                            _log.Info($"'{_parentDevice.Label}': Populating device device, [{genericApiDevice.DeviceId}] '{genericApiDevice.Label}'");

                            try
                            {
                                device = new Zone.Zone
                                {
                                    //important: make sure this is never empty
                                    Label = genericApiDevice.Label,
                                    Id = genericApiDevice.DeviceId,
                                    Latitude = genericApiDevice.Latitude,
                                    Longitude = genericApiDevice.Longitude,
                                    Altitude = genericApiDevice.Altitude,
                                    ParentId = _parentDevice.Identifier.ToString()
                                };

                                if (!_parentDevice.Interfaces.Contains(customIdentifier))
                                {
                                    AddToConnections(customIdentifier, device, DeviceInterfaceType.Other, newConnections);
                                    devicesAdded++;
                                }
                                else
                                {
                                    _parentDevice.RePopulateDevice(customIdentifier, device);
                                }
                            }
                            catch (ArgumentException ex)
                            {
                                _log.Error($"'{_parentDevice.Label}': Failed to populate device '{customIdentifier}' - {ex.Message}", ex);
                            }
                            catch (InvalidOperationException ex)
                            {
                                _log.Error($"'{_parentDevice.Label}': Failed to populate device '{customIdentifier}' - {ex.Message}", ex);
                            }
                        }

                        try
                        {
                            //Populating devices in batches. Populating one by one is too slow, populating all at once is likely to result in SQL Timeout exception. Recommended batch size is 50 - 100.
                            if (devicesAdded >= BatchSize)
                            {
                                _parentDevice.AddAndConnectRange(newConnections);
                                newConnections = new InterfaceConnectionCollection();
                                devicesAdded = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"'{_parentDevice.Label}': Failed to populate devices: {ex.Message}", ex);

                            //clear the collection and reset the counter otherwise subsequent population attempts fail as well
                            newConnections = new InterfaceConnectionCollection();
                            devicesAdded = 0;
                        }
                    }

                    try
                    {
                        //Populating devices if their number is less than a batch size.
                        if (devicesAdded > 0)
                        {
                            _parentDevice.AddAndConnectRange(newConnections);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"'{_parentDevice.Label}': Failed to populate devices: {ex.Message}", ex);
                    }
                    UpdateOrphanedZoneDevices(knownDevicesCustomIds, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                },
                cancellationToken);

                return true;
            }
            catch (OperationCanceledException ex)
            {
                _log.Warn($"Populating of generic devices was cancelled: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _log.Warn($"Populating generic devices has failed: {ex.Message}", ex);
                return false;
            }
            finally
            {
                _devicePopulationCancellationTokenSource.Dispose();
                DevicePopulationLock?.Release();
                _parentDevice.SetState(DeviceState.Online, string.Empty);
            }
        }




        public async Task<bool> PopulateUDCListAsync(bool refreshProperties)
        {

            if (_api == null || !_api.Connected)
            {
                _log.Warn($"'{_parentDevice.Label}':Cannot populate devices - not connected to the server");
                return false;
            }

            _devicePopulationCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _devicePopulationCancellationTokenSource.Token;

            cancellationToken.ThrowIfCancellationRequested();

            if (!await DevicePopulationLock.WaitAsync(WaitTimeoutMilliseconds, cancellationToken))
            {
                _log.Warn($"'{_parentDevice.Label}': Another device population in progress");
                _devicePopulationCancellationTokenSource.Dispose();
                _parentDevice.SetState(DeviceState.Failed, Messages.FailedToPopulatePanels);
                return false;
            }
            _log.Debug($"'{_parentDevice.Label}': Populate Generic devices");

            try
            {
                await Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _parentDevice.SetState(DeviceState.Online, Messages.PopulatingDevices);

                    //Known Devices Ids
                    var knownDevicesCustomIds = new HashSet<string>();

                    //populate Generic devices
                    var newConnections = new InterfaceConnectionCollection();
                    var devicesAdded = 0;

                    ////TODO: get the devices from the API
                    var devices = await _api.GetUDCList();

                    foreach (var genericApiDevice in devices)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            //stop the loop if population is cancelled to populate whatever 
                            break;
                        }
                        var customIdentifier = CustomIds.GetGenericCustomId(genericApiDevice.DeviceId);

                        if (knownDevicesCustomIds.Contains(customIdentifier))
                        {
                            _log.Warn($"'{_parentDevice.Label}': Skipping device with Custom ID '{customIdentifier}' - this custom is already listed");
                            continue;
                        }

                        knownDevicesCustomIds.Add(customIdentifier);

                        var device = _parentDevice.GetConnectedChildDevice<UDC.DeviceContracts.UDC>(customIdentifier);
                        if (device != null)
                        {
                            //device already exists in Control Center
                            _log.Debug($"'{_parentDevice.Label}': Updating device device with Custom ID '{customIdentifier}', '{genericApiDevice.Label}'");
                            device.UpdateProperties();
                            device.SetState(DeviceState.Online);
                        }
                        else
                        {
                            _log.Info($"'{_parentDevice.Label}': Populating device device, [{genericApiDevice.DeviceId}] '{genericApiDevice.Label}'");

                            try
                            {
                                device = new UDC.DeviceContracts.UDC
                                {
                                    //important: make sure this is never empty
                                    Label = genericApiDevice.Label,
                                    Id = genericApiDevice.DeviceId,
                                    Latitude = genericApiDevice.Latitude,
                                    Longitude = genericApiDevice.Longitude,
                                    Altitude = genericApiDevice.Altitude,
                                    ParentId = _parentDevice.Identifier.ToString()
                                };

                                if (!_parentDevice.Interfaces.Contains(customIdentifier))
                                {
                                    AddToConnections(customIdentifier, device, DeviceInterfaceType.Other, newConnections);
                                    devicesAdded++;
                                }
                                else
                                {
                                    _parentDevice.RePopulateDevice(customIdentifier, device);
                                }
                            }
                            catch (ArgumentException ex)
                            {
                                _log.Error($"'{_parentDevice.Label}': Failed to populate device '{customIdentifier}' - {ex.Message}", ex);
                            }
                            catch (InvalidOperationException ex)
                            {
                                _log.Error($"'{_parentDevice.Label}': Failed to populate device '{customIdentifier}' - {ex.Message}", ex);
                            }
                        }

                        try
                        {
                            //Populating devices in batches. Populating one by one is too slow, populating all at once is likely to result in SQL Timeout exception. Recommended batch size is 50 - 100.
                            if (devicesAdded >= BatchSize)
                            {
                                _parentDevice.AddAndConnectRange(newConnections);
                                newConnections = new InterfaceConnectionCollection();
                                devicesAdded = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"'{_parentDevice.Label}': Failed to populate devices: {ex.Message}", ex);

                            //clear the collection and reset the counter otherwise subsequent population attempts fail as well
                            newConnections = new InterfaceConnectionCollection();
                            devicesAdded = 0;
                        }
                    }

                    try
                    {
                        //Populating devices if their number is less than a batch size.
                        if (devicesAdded > 0)
                        {
                            _parentDevice.AddAndConnectRange(newConnections);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"'{_parentDevice.Label}': Failed to populate devices: {ex.Message}", ex);
                    }
                    UpdateOrphanedUdcDevices(knownDevicesCustomIds, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                },
                cancellationToken);

                return true;
            }
            catch (OperationCanceledException ex)
            {
                _log.Warn($"Populating of generic devices was cancelled: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _log.Warn($"Populating generic devices has failed: {ex.Message}", ex);
                return false;
            }
            finally
            {
                _devicePopulationCancellationTokenSource.Dispose();
                DevicePopulationLock?.Release();
                _parentDevice.SetState(DeviceState.Online, string.Empty);
            }
        }


        public void UpdateDevices<TDevice>(IEnumerable<IRadar> genericDevices, CancellationToken cancellationToken, Action<IRadar, TDevice> customAction = null)
            where TDevice : IRadar, new()
        {
            _log.Debug($"'{_parentDevice.Label}': Update devices");

            foreach (var genericDevice in genericDevices)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var customIdentifier = CustomIds.GetGenericCustomId(genericDevice.Id);
                var device = _parentDevice.GetConnectedChildDevice<TDevice>(customIdentifier);
                if (device == null)
                {
                    _log.Warn($"Child Device with Custom ID '{customIdentifier}' not found");
                    continue;
                }

                //device.SetSession(_api, _parentDevice.Log);
                customAction?.Invoke(genericDevice, device);

                //this has to be in the end to leave the device in the correct state 
                //device.UpdateState();
            }
        }

        public void CancelPopulation()
        {
            _log.Info($"'{_parentDevice.Label}': Cancelling device population");

            try
            {
                _devicePopulationCancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                ////
            }
        }

        private void UpdateOrphanedRadarDevices(IEnumerable<string> knownDevicesCustomIds, CancellationToken cancellationToken)
        {
            _log.Debug("UpdateOrphanedRadarDevices");

            //all the devices which are not in the collection of knownDevicesCustomIds don't represent any Generic device - set to Failure
            var orphanedCustomIds = _parentDevice.Interfaces.Select(each => each.CustomIdentifier).Except(knownDevicesCustomIds);
            foreach (var customId in orphanedCustomIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var genericDevice = _parentDevice.GetConnectedChildDevice<Radar>(customId);
                genericDevice?.SetState(DeviceState.Failed, Messages.DeviceNotFound);
            }
        }

        private void UpdateOrphanedCameraDevices(IEnumerable<string> knownDevicesCustomIds, CancellationToken cancellationToken)
        {
            _log.Debug("UpdateOrphanedCameraDevices");

            //all the devices which are not in the collection of knownDevicesCustomIds don't represent any Generic device - set to Failure
            var orphanedCustomIds = _parentDevice.Interfaces.Select(each => each.CustomIdentifier).Except(knownDevicesCustomIds);
            foreach (var customId in orphanedCustomIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var genericDevice = _parentDevice.GetConnectedChildDevice<Camera.Camera>(customId);
                genericDevice?.SetState(DeviceState.Failed, Messages.DeviceNotFound);
            }
        }

        private void UpdateOrphanedZoneDevices(IEnumerable<string> knownDevicesCustomIds, CancellationToken cancellationToken)
        {
            _log.Debug("UpdateOrphanedZoneDevices");

            //all the devices which are not in the collection of knownDevicesCustomIds don't represent any Generic device - set to Failure
            var orphanedCustomIds = _parentDevice.Interfaces.Select(each => each.CustomIdentifier).Except(knownDevicesCustomIds);
            foreach (var customId in orphanedCustomIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var genericDevice = _parentDevice.GetConnectedChildDevice<Zone.Zone>(customId);
                genericDevice?.SetState(DeviceState.Failed, Messages.DeviceNotFound);
            }
        }


        private void UpdateOrphanedUdcDevices(IEnumerable<string> knownDevicesCustomIds, CancellationToken cancellationToken)
        {
            _log.Debug("UpdateOrphanedUdcDevices");

            //all the devices which are not in the collection of knownDevicesCustomIds don't represent any Generic device - set to Failure
            var orphanedCustomIds = _parentDevice.Interfaces.Select(each => each.CustomIdentifier).Except(knownDevicesCustomIds);
            foreach (var customId in orphanedCustomIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var genericDevice = _parentDevice.GetConnectedChildDevice<UDC.DeviceContracts.UDC>(customId);
                genericDevice?.SetState(DeviceState.Failed, Messages.DeviceNotFound);
            }
        }



        public static void AddToConnections(string customIdentifier, IDevice device, DeviceInterfaceType deviceType, InterfaceConnectionCollection newConnections)
        {
            if (device == null || newConnections == null)
            {
                return;
            }

            var serverInput = new DeviceInterface(deviceType, device.Label, customIdentifier);
            newConnections.AddAndConnect(serverInput, device.Interfaces[0]);
        }

        public static DeviceInterfaceType GetDeviceInterfaceType(DeviceType deviceType)
        {
            switch (deviceType)
            {
                case DeviceType.Generic:
                    return DeviceInterfaceType.Other;
                default:
                    return DeviceInterfaceType.Other;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CancelPopulation();
            }
        }
    }
}