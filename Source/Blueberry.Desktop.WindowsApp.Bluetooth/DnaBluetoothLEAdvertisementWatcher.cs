using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace Blueberry.Desktop.WindowsApp.Bluetooth
{
    /// <summary>
    /// wraps and makes use of the <see cref="BluetoothLEAdvertisementWatcher"/>
    /// for easier consumption
    /// </summary>
    public class DnaBluetoothLEAdvertisementWatcher
    {

        #region Private Members

        /// <summary>
        /// the underlying bluetooth watcher class
        /// </summary>
        private readonly BluetoothLEAdvertisementWatcher mWatcher;

        /// <summary>
        /// a list of discovered devices
        /// </summary>
        private readonly Dictionary<string, DnaBluetoothLEDevice> mDiscoveredDevices = new Dictionary<string, DnaBluetoothLEDevice>();

        /// <summary>
        /// the details about GATT Services
        /// </summary>
        private readonly GattServiceIds mGattServiceIds;

        /// <summary>
        /// a thread lock object for this class
        /// </summary>
        private readonly object mThreadLock = new object();

        #endregion

        #region Public Events

        /// <summary>
        /// fired when the bluetooth watcher stop listening
        /// </summary>
        public event Action StoppedListening = () => { };

        /// <summary>
        /// fired when the bluetooth watcher start listening
        /// </summary>
        public event Action StartedListening = () => { };

        /// <summary>
        /// fired when a device discovered
        /// </summary>
        public event Action<DnaBluetoothLEDevice> DeviceDiscovered = (device) => { };

        /// <summary>
        /// fired when a device name changes
        /// </summary>
        public event Action<DnaBluetoothLEDevice> NewDeviceDiscovered = (device) => { };

        /// <summary>
        /// fired when a new device discovered
        /// </summary>
        public event Action<DnaBluetoothLEDevice> DeviceNameChanged = (device) => { };

        /// <summary>
        /// fired when a device removed for timing out
        /// </summary>
        public event Action<DnaBluetoothLEDevice> DeviceTimeout = (device) => { };

        #endregion

        #region Public Properties

        /// <summary>
        /// a flag to indicate if the watcher is listening for advertisements.
        /// </summary>
        public bool Listening => mWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        /// <summary>
        /// the timeout in seconds that a device is removed from the <see cref="DiscoveredDevices"/>
        /// list if it is not re-advertised within this time
        /// </summary>
        public int HeartbeatTimeout { get; set; } = 30;

        /// <summary>
        /// list of discovered devices
        /// </summary>
        public IReadOnlyCollection<DnaBluetoothLEDevice> DiscoveredDevices
        {
            get
            {
                // clean up any timed out devices
                CleanupTimeOutDevices();

                // lock thread
                lock (mThreadLock)
                {
                    // convert to read only list
                    return mDiscoveredDevices.Values.ToList().AsReadOnly();
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// the default constructor
        /// </summary>
        public DnaBluetoothLEAdvertisementWatcher(GattServiceIds gattIds)
        {
            // null check 
            mGattServiceIds = gattIds ?? throw new ArgumentNullException(nameof(gattIds));

            // create bluetooth listener
            mWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active,
            };

            // listen out for new advertisements
            mWatcher.Received += WatcherAdvertisementReceivedAsync;

            // listen out for when the watcher stops listening
            mWatcher.Stopped += (watcher, e) =>
            {
                // inform listeners
                StoppedListening();
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// listens out for watcher advertisements
        /// </summary>
        /// <param name="sender">the watcher</param>
        /// <param name="args">the arguments</param>
        private async void WatcherAdvertisementReceivedAsync(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {

            // clean up timed out devices.
            CleanupTimeOutDevices();

            // get bluetooth device info
            var device = await GetBluetoothLEDeviceAsync(
                args.BluetoothAddress,
                args.Timestamp,
                args.RawSignalStrengthInDBm);

            // null check 
            if (device == null)
                return;

            // is new discovered device?
            var newDiscovery = false;

            var existingName = default(string);

            // lock the thread
            lock (mThreadLock)
            {
                // check if this is a new discovery
                newDiscovery = !mDiscoveredDevices.ContainsKey(device.DeviceId);

                // if this is not new 
                if (!newDiscovery)
                    // store the old name
                    existingName = mDiscoveredDevices[device.DeviceId].Name;
            }

            // name changed?
            var nameChanged =
                // it is not a new discovered device
                !newDiscovery &&
                // and is not a blank name
                !string.IsNullOrEmpty(device.Name) &&
                // and the name is different
                existingName != device.Name;

            // lock thread
            lock (mThreadLock)
            {

                // add/update the device in the dictionary
                mDiscoveredDevices[device.DeviceId] = device;
            }

            // inform listeners
            DeviceDiscovered(device);

            // if name changed
            if (nameChanged)
                // inform listeners
                DeviceNameChanged(device);

            // if new device discovered
            if (newDiscovery)
                // inform listeners
                NewDeviceDiscovered(device);
        }

        /// <summary>
        /// connects to the BLE device and extracts more information for the 
        /// <see cref="https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetoothledevice"/>
        /// </summary>
        /// <param name="address">the bluetooth device address to connect</param>
        /// <param name="broadcastTime">the time broadcast message received</param>
        /// <param name="rssi">the signal strength received</param>
        /// <returns></returns>
        private async Task<DnaBluetoothLEDevice> GetBluetoothLEDeviceAsync(ulong address, DateTimeOffset broadcastTime, short rssi)
        {

            // get bluetooth device info
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask();

            // null check 
            if (device == null)
                return null;

            // NOTE: this can throw a System.Exception for failure
            // get GATT Services that are available
            var gatt = await device.GetGattServicesAsync().AsTask();

            // if we have any services..
            if (gatt.Status == GattCommunicationStatus.Success)
            {
                // loop each GATT Service
                foreach (var service in gatt.Services)
                {
                    // this id contains GATT Profile Assigned number                     
                    var gattProfileID = service.Uuid;

                    //if(service.Uuid.ToString("N").Substring(4,4) == "1808")
                    //{
                    //    System.Diagnostics.Debugger.Break();
                    //}
                }
            }

            // return the new device information
            return new DnaBluetoothLEDevice
            (
                // Device Id
                deviceId: device.DeviceId,
                // Bluetooth address
                address: device.BluetoothAddress,
                // device name
                name: device.Name,
                // broadcast time
                broadcastTime: broadcastTime,
                // signal name
                rssi: rssi,
                // is connected?
                connected: device.ConnectionStatus == BluetoothConnectionStatus.Connected,
                // can pair?
                canPair: device.DeviceInformation.Pairing.CanPair,
                // is paired?
                paired: device.DeviceInformation.Pairing.IsPaired
            );

        }

        /// <summary>
        /// Prune any device that not heard from longer than <see cref="HeartbeatTimeout"/>
        /// </summary>
        private void CleanupTimeOutDevices()
        {
            lock (mThreadLock)
            {
                // decide threshold to flag heartbeat timed out devices
                var threshold = DateTime.UtcNow - TimeSpan.FromSeconds(HeartbeatTimeout);

                // any devices that have not send a new broadcast within the heartbeat time
                mDiscoveredDevices.Where(f => f.Value.BroadcastTime < threshold).ToList().ForEach(device =>
                 {
                     // remove device
                     mDiscoveredDevices.Remove(device.Key);

                     // inform listeners
                     DeviceTimeout(device.Value);
                 });
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts listening for advertisements
        /// </summary>
        public void StartListening()
        {
            lock (mThreadLock)
            {

                // if already listening
                if (Listening)
                    // do nothing
                    return;

                // start the watcher.
                mWatcher.Start();
            }

            // inform listeners
            StartedListening();
        }

        /// <summary>
        /// Stop listening for advertisements
        /// </summary>
        public void StopListening()
        {

            lock (mThreadLock)
            {
                // if already not listening
                if (!Listening)
                    // do nothing
                    return;

                // stop the watcher
                mWatcher.Stop();

                // clear any devices
                mDiscoveredDevices.Clear();
            }
        }

        /// <summary>
        /// attempts to pair a BLE device by ID
        /// </summary>
        /// <param name="deviceId">the BLE device Id</param>
        /// <returns></returns>
        public async Task PairToDeviceAsync(string deviceId)
        {
            // get bluetooth device info
            var device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask();

            // null check 
            if (device == null)
                // TODO: Localize
                throw new ArgumentNullException("Failed to get information about the Bluetooth device");

            // if we already paired...
            if (device.DeviceInformation.Pairing.IsPaired)
                // do nothing..
                return;

            // listen out for pairing requests
            device.DeviceInformation.Pairing.Custom.PairingRequested += (sender, args) =>
            {
                // log it
                // TODO: Remove
                Console.WriteLine("Accepting pairing request...");

                // accept all attempts
                args.Accept();
            };

            // try and pair to the device
            var result = await device.DeviceInformation.Pairing.Custom.PairAsync(
           
                // for contour we should try provide pin
                // TODO: try different types to see if any works
                DevicePairingKinds.ProvidePin
            ).AsTask();
            
            // log the result
            if (result.Status == DevicePairingResultStatus.Paired)
                // TODO: Remove
                Console.WriteLine("Pairing successful");
            else
                // TODO: remove
                Console.WriteLine($"Pairing failed: {result.Status}");
        }

        #endregion
    }
}
