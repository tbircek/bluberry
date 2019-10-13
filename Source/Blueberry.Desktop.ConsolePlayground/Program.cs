using System;
using System.Linq;
using System.Threading.Tasks;
using Blueberry.Desktop.WindowsApp.Bluetooth;

namespace Blueberry.Desktop.ConsolePlayground
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var tcs = new TaskCompletionSource<bool>();

            Task.Run(async () =>
            {

                try
                {
                    // new watcher
                    var watcher = new DnaBluetoothLEAdvertisementWatcher(new GattServiceIds());

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
                        var command = Console.ReadLine();

                        if (string.IsNullOrEmpty(command))
                        {

                            // get discovered devices
                            var devices = watcher.DiscoveredDevices;

                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"{devices.Count} devices....");

                            // show devices in console
                            foreach (var device in devices)
                                Console.WriteLine(device);

                        }

                        // C to connect
                        else if (command == "c")
                        {
                            // attempt to find contour device
                            var contourDevice = watcher.DiscoveredDevices.FirstOrDefault(
                                f => f.Name.ToLower().StartsWith("lg"));

                            // if we don't find it...
                            if (contourDevice == null)
                            {
                                // let the user know
                                Console.WriteLine("No contour device found for connecting");
                                continue;
                            }

                            // try and connect
                            Console.WriteLine("connecting to Contour Device...");

                            try
                            {
                                // try and connect
                                await watcher.PairToDeviceAsync(contourDevice.DeviceId);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Failed to pair to contour device.");
                                Console.WriteLine(ex);

                            }
                        }

                        // Q to quit
                        else if(command == "q")
                        {
                            break;
                        }
                    }

                    // finish console application
                    tcs.TrySetResult(true);
                }
                finally
                {
                    // if anything goes wrong, exit out
                    tcs.TrySetResult(false);

                }
            });

            tcs.Task.Wait();

        }
    }
}
