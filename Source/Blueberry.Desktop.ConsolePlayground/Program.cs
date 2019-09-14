using System;
using Blueberry.Desktop.WindowsApp.Bluetooth;

namespace Blueberry.Desktop.ConsolePlayground
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // new watcher
            var watcher = new DnaBluetoothLEAdvertisementWatcher();

            // hook into start event
            watcher.StartedListening += () =>
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Started Listening");
            };

            // hook into stop event
            watcher.StoppedListening += () =>
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Stopped Listening");
            };

            watcher.NewDeviceDiscovered += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"New device: {device}");
            };

            watcher.DeviceNameChanged += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Device name changed: {device}");
            };

            watcher.DeviceTimeout += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Device timed out: {device}");
            };

            // start listening
            watcher.StartListening();

            while (true)
            {
                // pause until we press enter
                Console.ReadLine();

                // get discovered devices
                var devices = watcher.DiscoveredDevices;

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{devices.Count} devices....");

                // show devices in console
                foreach (var device in devices)
                    Console.WriteLine(device);

            }
        }
    }
}
