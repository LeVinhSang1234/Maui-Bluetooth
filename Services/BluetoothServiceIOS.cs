using System.Text;
using Bluetooth.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace Bluetooth.Services
{
    public class BluetoothServiceIOS
    {
        private IAdapter _adapter;

        public Action<BluetoothDeviceModelIOS>? OnDeviceDiscovered;
        public Action<BluetoothDeviceModelIOS>? OnMyDeviceAdded;
        public Action<BluetoothDeviceModelIOS>? OnConnecting;
        public Action<BluetoothDeviceModelIOS>? OnConnected;
        public Action<string?>? OnMessage;
        public Action? OnEndScan;

        public BluetoothServiceIOS()
        {
            _adapter = CrossBluetoothLE.Current.Adapter;
            _adapter.DeviceDiscovered += (s, a) =>
            {
                OnDeviceDiscovered?.Invoke(new BluetoothDeviceModelIOS()
                {
                    Device = a.Device
                });
            };
            ScanForDevices();
        }

        public async void ScanForDevices()
        {
            while (true)
            {
                await Task.Delay(1000);
                var pairedDevices = _adapter.GetSystemConnectedOrPairedDevices(new Guid[0]);
                if (pairedDevices != null && pairedDevices.Count > 0)
                {
                    foreach(var device in pairedDevices)
                    {
                        OnMyDeviceAdded?.Invoke(new BluetoothDeviceModelIOS()
                        {
                            Device = device
                        });
                    }
                }
                await _adapter.StartScanningForDevicesAsync();
                await Task.Delay(10000);
                await _adapter.StopScanningForDevicesAsync();
                OnEndScan?.Invoke();
            }
        }

        public async Task DisConnectToDevice(BluetoothDeviceModelIOS device)
        {
            try
            {
                await _adapter.DisconnectDeviceAsync(device.Device);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error connecting to device: {ex.Message}");
            }
        }

        public async void ConnectToDevice(BluetoothDeviceModelIOS device)
        {
            try
            {
                device.IsConnecting = true;
                OnConnecting?.Invoke(device);
                await _adapter.ConnectToDeviceAsync(device.Device);
                device.IsConnecting = false;
                device.IsConnected = true;
                OnConnected?.Invoke(device);
                OnMessage?.Invoke("Connected");
                //var services = await device.Device.GetServicesAsync();
                //foreach (var service in services)
                //{
                //    System.Diagnostics.Debug.WriteLine($"Service: {service.Id}");
                //}
                var message = await GetSerialNumber(device);
                if (!string.IsNullOrEmpty(message))
                {
                    OnMessage?.Invoke($"Serial Number {message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error connecting to device: {ex.Message}");
            }
        }

        public async Task<string> GetSerialNumber(BluetoothDeviceModelIOS device)
        {
            var _deviceInfoService = await device.Device.GetServiceAsync(Guid.Parse("0000180a-0000-1000-8000-00805f9b34fb"));
            if (_deviceInfoService == null) return string.Empty;
            var _serialNumberCharacteristic = await _deviceInfoService.GetCharacteristicAsync(Guid.Parse("00002A25-0000-1000-8000-00805f9b34fb"));
            if (_serialNumberCharacteristic == null) return string.Empty;

            var serialNumberBytes = await _serialNumberCharacteristic.ReadAsync();
            var serialNumber = Encoding.UTF8.GetString(serialNumberBytes.data);
            return serialNumber ?? string.Empty;
        }
    }
}

