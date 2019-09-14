using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;

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
        private readonly Dictionary<ulong, DnaBluetoothLEDevice> mDiscoveredDevices = new Dictionary<ulong, DnaBluetoothLEDevice>();

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
        public DnaBluetoothLEAdvertisementWatcher()
        {
            // create bluetooth listener
            mWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active,
            };

            // listen out for new advertisements
            mWatcher.Received += WatcherAdvertisementReceived;

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
        private void WatcherAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {

            // clean up timed out devices.
            CleanupTimeOutDevices();

            DnaBluetoothLEDevice device = null;

            // is new discovered device?
            var newDiscovery = !mDiscoveredDevices.ContainsKey(args.BluetoothAddress);

            // name changed?
            var nameChanged =
                // it is not a new discovered device
                !newDiscovery &&
                // and is not a blank name
                !string.IsNullOrEmpty(args.Advertisement.LocalName) &&
                // and the name is different
                mDiscoveredDevices[args.BluetoothAddress].Name != args.Advertisement.LocalName;

            // lock thread
            lock (mThreadLock)
            {
                // get the name of the device
                var name = args.Advertisement.LocalName;

                // if new name is blank, and we already have a device
                if (string.IsNullOrEmpty(name) && !newDiscovery)
                    // don't override what could be an actual name already
                    name = mDiscoveredDevices[args.BluetoothAddress].Name;

                // create new device info class
                device = new DnaBluetoothLEDevice
                    (
                        // address of the device
                        address: args.BluetoothAddress,

                        // name of the device
                        name: name,

                        // broadcast time
                        broadcastTime: args.Timestamp,

                        // signal strength 
                        rssi: args.RawSignalStrengthInDBm
                    );

                // add/update the device in the dictionary
                mDiscoveredDevices[args.BluetoothAddress] = device;
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

        #endregion
    }
}
