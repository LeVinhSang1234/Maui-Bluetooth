using System.Collections.ObjectModel;
using System.ComponentModel;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace Bluetooth.Services
{
    public class BluetoothManager : INotifyPropertyChanged
    {
        private readonly IBluetoothLE _bluetoothLe;
        private readonly IAdapter _adapter;
        private ObservableCollection<BluetoothDevice> DevicesScan;

        public event PropertyChangedEventHandler? PropertyChanged;
        public bool IsDeviceConnected { get; set; } = false;
        public ObservableCollection<BluetoothDevice> Devices { get; set; }
        public BluetoothDevice? DeviceConnected { get; set; }

        public BluetoothManager()
        {
            _bluetoothLe = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
            Devices = new ObservableCollection<BluetoothDevice>();
            DevicesScan = new ObservableCollection<BluetoothDevice>();

            _adapter.DeviceDiscovered += (s, a) =>
            {
                if (!DevicesScan.Any(d => d.Device.Id.ToString() == a.Device.Id.ToString()) && !string.IsNullOrEmpty(a.Device.Name))
                {
                    DevicesScan.Add(new BluetoothDevice { Name = a.Device.Name, Device = a.Device, Rssi = a.Device.Rssi });
                }
                if (!Devices.Any(d => d.Device.Id.ToString() == a.Device.Id.ToString()) && !string.IsNullOrEmpty(a.Device.Name) && DeviceConnected?.Name != a.Device.Name)
                {
                    Devices.Add(new BluetoothDevice { Name = a.Device.Name, Device = a.Device, Rssi = a.Device.Rssi });
                }
            };

            _adapter.DeviceConnected += (s, a) =>
            {
                if (DeviceConnected?.Name != a.Device.Name)
                {
                    DeviceConnected = new BluetoothDevice { Name = a.Device.Name, Device = a.Device, Rssi = a.Device.Rssi };
                }
                DeviceConnected.IsConnecting = false;
                DeviceConnected.IsConnected = true;
                OnPropertyChanged(nameof(DeviceConnected));
            };

            _adapter.DeviceDisconnected += (s, a) =>
            {
                if (a.Device.Name == DeviceConnected?.Name) DeviceConnected = null;
            };
            _ = ScanForDevicesAsync();
        }

        public async Task ScanForDevicesAsync()
        {
            while (true)
            {
                if (_bluetoothLe.IsOn)
                {
                    await _adapter.StartScanningForDevicesAsync();
                    await Task.Delay(5000);
                    await _adapter.StopScanningForDevicesAsync();
                    UpdateDevices();
                    await Task.Delay(100);
                }
                else await Task.Delay(1000);
            }
        }

        public async Task ConnectToDeviceAsync(BluetoothDevice device)
        {
            if (device.Name == DeviceConnected?.Name)
            {
                await DisConnectToDeviceAsync(device);
            }
            else
            {
                IsDeviceConnected = true;
                OnPropertyChanged(nameof(IsDeviceConnected));
                if(DeviceConnected != null)
                {
                    Devices.Add(DeviceConnected);
                }
                DeviceConnected = device;
                DeviceConnected.IsConnecting = true;
                DeviceConnected.IsConnected = false;
                Devices.Remove(DeviceConnected);
                OnPropertyChanged(nameof(DeviceConnected));
                await _adapter.ConnectToDeviceAsync(device.Device);
            }
        }

        public async Task DisConnectToDeviceAsync(BluetoothDevice device)
        {
            IsDeviceConnected = false;
            OnPropertyChanged(nameof(IsDeviceConnected));
            Devices.Add(device);
            await _adapter.DisconnectDeviceAsync(device.Device);
        }

        private void UpdateDevices()
        {
            var devicesToRemove = Devices.Where(d => IsRemove(d.Device)).ToList();
            foreach (var device in devicesToRemove)
            {
                Devices.Remove(device);
            }
            foreach (var scanDevice in DevicesScan)
            {
                if (!Contains(scanDevice.Device) && scanDevice.Name != DeviceConnected?.Name)
                {
                    Devices.Add(scanDevice);
                }
            }
            DevicesScan.Clear();
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

        private bool Contains(IDevice device)
        {
            return Devices.Any(d => d.Device.Name == device.Name);
        }

        private bool IsRemove(IDevice device)
        {
            return !DevicesScan.Any(d => d.Device.Name == device.Name);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
