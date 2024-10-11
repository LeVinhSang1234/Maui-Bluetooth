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
            _adapter.DeviceConnected += (s, a) =>
            {
                foreach (var dev in Devices)
                {
                    if(dev.Id.ToString() == a.Device.Id.ToString())
                    {
                        dev.IsConnected = true;
                        _deviceConnected = dev;
                    }
                }
            };

            _adapter.DeviceDisconnected += (s, a) =>
            {
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
    }
}

