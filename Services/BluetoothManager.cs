using System.Collections.ObjectModel;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace Bluetooth.Services
{
    public class BluetoothManager
    {
        private readonly IBluetoothLE _bluetoothLe;
        private readonly IAdapter _adapter;
        private BluetoothDevice? _deviceConnected;
        public ObservableCollection<BluetoothDevice> Devices { get; set; }

        public BluetoothManager()
        {
            _bluetoothLe = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
            Devices = new ObservableCollection<BluetoothDevice>();

            _adapter.DeviceDiscovered += (s, a) =>
            {
                if (!Devices.Any(d => d.Id.ToString() == a.Device.Id.ToString()) && !string.IsNullOrEmpty(a.Device.Name))
                {
                    Devices.Add(new BluetoothDevice { Name = a.Device.Name, Id = a.Device.Id.ToString(), Device = a.Device, Rssi = a.Device.Rssi, IsConnecting = false, IsConnected = false });
                }
            };

            _adapter.DeviceConnected += async (s, a) =>
            {
                System.Diagnostics.Debug.WriteLine($"Connect ==> {a.Device.Name} - {a.Device.Rssi}");
                foreach (var dev in Devices)
                {
                    if (dev.Id.ToString() == a.Device.Id.ToString())
                    {
                        dev.IsConnected = true;
                        _deviceConnected = dev;
                        await GetDeviceServicesAndCharacteristicsAsync(dev);
                    }
                }
            };

            _adapter.DeviceDisconnected += (s, a) =>
            {
                System.Diagnostics.Debug.WriteLine($"Disconnected ==> {a.Device.Name}");
                foreach (var dev in Devices)
                {
                    if (dev.Id.ToString() == a.Device.Id.ToString())
                    {
                        dev.IsConnected = false;
                    }
                }
            };

            _bluetoothLe.StateChanged += (sender, args) =>
            {
                if (_bluetoothLe.IsOn)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await ScanForDevicesAsync();
                        SortDevicesByRssi();
                    });
                }
            };
        }

        public async Task ScanForDevicesAsync()
        {
            if (!_bluetoothLe.IsOn) return;
            Devices.Clear();
            await _adapter.StartScanningForDevicesAsync();
        }

        public async Task ConnectToDeviceAsync(BluetoothDevice device)
        {
            if (device.IsConnected || device.IsConnecting) return;
            if (_deviceConnected != null)
            {
                await DisConnectToDeviceAsync(_deviceConnected);
            }
            device.IsConnecting = true;
            await _adapter.ConnectToDeviceAsync(device.Device);
            device.IsConnecting = false;
        }

        public async Task DisConnectToDeviceAsync(BluetoothDevice device)
        {
            _deviceConnected = null;
            device.IsConnected = false;
            await _adapter.DisconnectDeviceAsync(device.Device);
        }

        public void SortDevicesByRssi()
        {
            var sortedDevices = Devices.OrderByDescending(d => d.Rssi).ToList();
            Devices.Clear();
            foreach (var device in sortedDevices)
            {
                Devices.Add(device);
            }
        }

        private async Task GetDeviceServicesAndCharacteristicsAsync(BluetoothDevice device)
        {
            try
            {
                if (device == null || device.Device == null)
                {
                    System.Diagnostics.Debug.WriteLine("Device or Device reference is null.");
                    return;
                }
                var services = await device.Device.GetServicesAsync();

                if (services == null || !services.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No services found.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Services found:");
                foreach (var service in services)
                {
                    System.Diagnostics.Debug.WriteLine($"Service: {service.Name} -- {service.Id}");
                    var characteristics = await service.GetCharacteristicsAsync();
                    if (characteristics == null || !characteristics.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("No characteristics found for this service.");
                        continue;
                    }

                    foreach (var characteristic in characteristics)
                    {
                        System.Diagnostics.Debug.WriteLine($"Characteristic: {characteristic.Uuid}");
                        var result = await characteristic.ReadAsync();
                        if (result.data.Length > 0)
                        {
                            var value = BitConverter.ToString(result.data).Replace("-", " ");
                            System.Diagnostics.Debug.WriteLine($"Value: {value}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("No value returned for this characteristic.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting services and characteristics: {ex.Message}");
            }
        }

    }
}
