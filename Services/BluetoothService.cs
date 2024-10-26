using System.Collections.ObjectModel;
using System.ComponentModel;
using Android.Bluetooth;
using Android.Content;
using Bluetooth.Models;

namespace Bluetooth.Services
{
    public class BluetoothService : INotifyPropertyChanged
    {
        private readonly BluetoothAdapter _adapter;
        private ObservableCollection<BluetoothDeviceModel> DevicesScan;

        public event PropertyChangedEventHandler? PropertyChanged;
        public bool IsBluetoothEnabled => _adapter.IsEnabled;
        public ObservableCollection<BluetoothDeviceModel> Devices { get; set; }
        public bool isScanning { get; private set; } = false;

        public BluetoothService()
        {
            _adapter = BluetoothAdapter.DefaultAdapter;
            OnPropertyChanged(nameof(IsBluetoothEnabled));
            Devices = new ObservableCollection<BluetoothDeviceModel>();
            DevicesScan = new ObservableCollection<BluetoothDeviceModel>();
            _ = StartScanAsync();
        }


        public async Task StartScanAsync()
        {
            while (true)
            {
                OnPropertyChanged(nameof(IsBluetoothEnabled));
                bool allowBluetooth = await RequestPermission();
                if (!allowBluetooth)
                {
                    await Task.Delay(1000);
                    continue;
                };
                if (!isScanning)
                {
                    isScanning = true;
                    OnPropertyChanged(nameof(isScanning));
                }
                var receiver = new BluetoothDeviceReceiver(Devices);
                var _receiver = new BluetoothDeviceReceiver(DevicesScan);

                Platform.CurrentActivity!.RegisterReceiver(receiver, new IntentFilter(BluetoothDevice.ActionFound));
                Platform.CurrentActivity!.RegisterReceiver(_receiver, new IntentFilter(BluetoothDevice.ActionFound));

                _adapter.StartDiscovery();
                await Task.Delay(10000);
                _adapter.CancelDiscovery();
                Platform.CurrentActivity.UnregisterReceiver(receiver);
                Platform.CurrentActivity.UnregisterReceiver(_receiver);

                await UpdateDeviceList();
                await Task.Delay(1000);
            }
        }

        private async Task UpdateDeviceList()
        {
            var devicesToRemove = Devices.Where(device => !DevicesScan.Any(d => d.Device.Address == device.Device.Address)).ToList();
            var _deviceUpdates = Devices;
            foreach (var device in devicesToRemove)
            {
                _deviceUpdates.Remove(device);
            }
            Devices = _deviceUpdates;
            await Task.Delay(1000);
            OnPropertyChanged(nameof(Devices));
            DevicesScan.Clear();
        }


        private async Task<bool> RequestPermission()
        {
            if (_adapter == null || !_adapter.IsEnabled) return false;
            await Task.Delay(1000);
            var status = await Permissions.RequestAsync<Permissions.Bluetooth>();
            if (status == PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            return status == PermissionStatus.Granted;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}


public class BluetoothDeviceReceiver : BroadcastReceiver
{
    private readonly ObservableCollection<BluetoothDeviceModel> _foundDevices;

    public BluetoothDeviceReceiver(ObservableCollection<BluetoothDeviceModel> foundDevices)
    {
        _foundDevices = foundDevices;
    }


    public override void OnReceive(Context context, Intent intent)
    {
        var action = intent?.Action;

        if (action == BluetoothDevice.ActionFound)
        {
            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
            if (device != null)
            {
                var rssi = intent.GetShortExtra(BluetoothDevice.ExtraRssi, 0);
                var existingDevice = _foundDevices.FirstOrDefault(d => d.Device.Address == device.Address);
                if (existingDevice != null)
                {
                    existingDevice.Rssi = rssi;
                }
                else
                {
                    _foundDevices.Add(new BluetoothDeviceModel()
                    {
                        Device = device,
                        Rssi = rssi,
                    });
                }
            }
        }
    }
}
