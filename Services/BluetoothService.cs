using System.Collections.ObjectModel;
using System.ComponentModel;
using Android.Bluetooth;
using Bluetooth.Models;
using Bluetooth.Services;
using Java.Util;

namespace Bluetooth.Services
{
    public class BluetoothService : INotifyPropertyChanged
    {
        private readonly BluetoothAdapter _adapter;
        private BluetoothSocket _socket;
        private BluetoothGatt _bluetoothGatt;

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<BluetoothDeviceModel> Devices { get; set; }
        public BluetoothDeviceModel? DeviceConnected { get; private set; }
        public bool isScanning { get; private set; } = false;
        public bool IsBluetoothEnabled => _adapter.IsEnabled;
        public bool IsConnecting => DeviceConnected != null;

        public BluetoothService()
        {
            _adapter = BluetoothAdapter.DefaultAdapter;
            OnPropertyChanged(nameof(IsBluetoothEnabled));
            Devices = new ObservableCollection<BluetoothDeviceModel>();
            _ = ConnectToIphone();
        }

        public async Task ConnectToIphone()
        {
            await RequestPermission();
            var pairedDevices = _adapter.BondedDevices;
            if (pairedDevices != null && pairedDevices.Count > 0)
            {
                var device = pairedDevices.FirstOrDefault(d => d.Address == "88:64:40:2A:4B:E7");
                if (device != null)
                {
                    Devices.Add(new BluetoothDeviceModel()
                    {
                        Device = device
                    });

                }
            }
        }

        public void UpdateBatteryLevel(int batteryLevel)
        {
            if(DeviceConnected != null)
            {
                DeviceConnected.IsConnecting = false;
                DeviceConnected.IsConnected = true;
                DeviceConnected.BatteryLevel = $"BatteryLevel {batteryLevel}%";
                OnPropertyChanged(nameof(DeviceConnected));
            }
        }


        public void ConnectGatFail(GattStatus status)
        {
            string message = $"Status Fail: {status}";
            DeviceConnected = null;
            OnPropertyChanged(nameof(DeviceConnected));
            OnPropertyChanged(nameof(IsConnecting));
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert("Connect to Gatt failed!", $"Status Fail {status}", "OK");
            });
        }

        public async Task ConnectSocketToDevice(BluetoothDeviceModel device)
        {
            if(_bluetoothGatt != null)
            {
                _bluetoothGatt.Disconnect();
            }
            DeviceConnected = device;
            OnPropertyChanged(nameof(IsConnecting));
            try
            {
                var uuids = device.Device.GetUuids();
                foreach(var uuid in uuids)
                {
                    System.Diagnostics.Debug.WriteLine($"uuid {uuid}");
                }

                DeviceConnected.IsConnecting = true;
                OnPropertyChanged(nameof(DeviceConnected));

                try
                {
                    _bluetoothGatt = device.Device.ConnectGatt(Android.App.Application.Context, false, new GattCallback(this));
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    await Application.Current.MainPage.DisplayAlert("Connect failed!", e.Message, "OK");
                    DeviceConnected = null;
                }
                OnPropertyChanged(nameof(DeviceConnected));
                OnPropertyChanged(nameof(IsConnecting));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error connecting to device: " + ex.Message);
                _socket?.Close();
            }
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


public class GattCallback : BluetoothGattCallback
{
    private BluetoothService _bluetoothService;

    public GattCallback(BluetoothService bluetoothService)
    {
        _bluetoothService = bluetoothService;
    }

    public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
    {
        if (status == GattStatus.Success && newState == ProfileState.Connected)
        {
            gatt.DiscoverServices(); 
        } else _bluetoothService.ConnectGatFail(status);
    }

    public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
    {
        if (status == GattStatus.Success)
        {
            var batteryService = gatt.GetService(UUID.FromString("0000180F-0000-1000-8000-00805f9b34fb"));  // Battery Service
            if (batteryService != null)
            {
                var batteryLevelCharacteristic = batteryService.GetCharacteristic(UUID.FromString("00002A19-0000-1000-8000-00805f9b34fb")); // Battery Level
                if (batteryLevelCharacteristic != null)
                {
                    gatt.ReadCharacteristic(batteryLevelCharacteristic);
                }
            }
        }
    }

    public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
    {
        if (status == GattStatus.Success)
        {
            var batteryLevel = characteristic.GetValue();
            if (batteryLevel != null && batteryLevel.Length > 0)
            {
                var batteryValue = batteryLevel[0];
                _bluetoothService.UpdateBatteryLevel(batteryValue);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Battery Level characteristic is empty.");
            }
        }
    }
}