
using System;

namespace Blueberry.Desktop.WindowsApp.Bluetooth
{
    /// <summary>
    /// information about a BLE device
    /// </summary>
    public class DnaBluetoothLEDevice
    {
        #region Public Properties

        /// <summary>
        /// the time of the broadcast advertisement message of the device
        /// </summary>
        public DateTimeOffset BroadcastTime { get; }

        /// <summary>
        /// the address of the device
        /// </summary>
        public ulong Address { get; set; }

        /// <summary>
        /// the name of the device
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// the signal strength in dB
        /// </summary>
        public short SignalStrengthInDB { get; }

        /// <summary>
        /// indicates if we are connected to this device
        /// </summary>
        public bool Connected { get; }

        /// <summary>
        /// indicates if this device supports pairing
        /// </summary>
        public bool CanPair { get; }

        /// <summary>
        /// indicates if we are currently paired to this device
        /// </summary>
        public bool Paired { get; }

        /// <summary>
        /// the permanent unique ID of this device
        /// </summary>
        public string DeviceId { get; }


        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="address">the device address</param>
        /// <param name="name">the device name</param>
        /// <param name="rssi">the signal strength</param>
        /// <param name="broadcastTime">the broadcast time of the discovery</param>
        /// <param name="connected">if connected to the device</param>
        /// <param name="canPair">if we can pair to the device</param>
        /// <param name="paired">if we paired to the device</param>
        /// <param name="deviceId">the unique ID of the device</param>
        public DnaBluetoothLEDevice(
            ulong address, 
            string name, 
            short rssi, 
            DateTimeOffset broadcastTime,
            bool connected,
            bool canPair,
            bool paired,
            string deviceId)
        {
            Address = address;
            Name = name;
            SignalStrengthInDB = rssi;
            BroadcastTime = broadcastTime;
            Connected = connected;
            CanPair = canPair;
            Paired = paired;
            DeviceId = deviceId;
        }

        #endregion

        /// <summary>
        /// user friendly ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{(string.IsNullOrEmpty(Name) ? "[No Name]" : Name)} [{DeviceId}] ({SignalStrengthInDB})";
        }
    }
}
