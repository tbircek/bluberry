
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

        #endregion

        #region Constructor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DnaBluetoothLEDevice(ulong address, string name, short rssi, DateTimeOffset broadcastTime)
        {
            Address = address;
            Name = name;
            SignalStrengthInDB = rssi;
            BroadcastTime = broadcastTime;
        }

        #endregion

        /// <summary>
        /// user friendly ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{(string.IsNullOrEmpty(Name) ? "[No Name]" : Name)}{Address} ({SignalStrengthInDB})";
        }
    }
}
