using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Android.Bluetooth;
using Android.Content;
using Bluetooth.Models;
using Java.Util;

namespace Bluetooth.Services
{
    public class BluetoothService : INotifyPropertyChanged
    {
        private readonly BluetoothAdapter _adapter;
        private ObservableCollection<BluetoothDeviceModel> DevicesScan;
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
            foreach (var device in devicesToRemove)
            {
                Devices.Remove(device);
            }
            await Task.Delay(1000);
            OnPropertyChanged(nameof(Devices));
            DevicesScan.Clear();
        }

        // connect with socket. If devices support.
        public async Task ConnectSocketToDevice(BluetoothDeviceModel device)
        {
            DeviceConnected = device;
            OnPropertyChanged(nameof(IsConnecting));
            try
            {
                // List bluetooth connected.
                var pairedDevices = _adapter.BondedDevices;
                if (pairedDevices != null && pairedDevices.Count > 0)
                {
                    foreach (BluetoothDevice d in pairedDevices)
                    {
                        System.Diagnostics.Debug.WriteLine($"pair {d.Name}");
                    }
                }
                var serviceUuid = UUID.FromString("0000110B-0000-1000-8000-00805F9B34FB"); // Any service device support.
                var characteristicUuid = UUID.FromString("00002a00-0000-1000-8000-00805f9b34fb"); // Any characteristic device support.
                var uuids = DeviceConnected.Device.GetUuids();
                if (uuids != null)
                {
                    foreach (var uuid in uuids)
                    {
                        System.Diagnostics.Debug.WriteLine(uuid);
                    }
                }
                else System.Diagnostics.Debug.WriteLine("null lllll");

                _socket = DeviceConnected.Device.CreateRfcommSocketToServiceRecord(serviceUuid);
                if (BluetoothAdapter.DefaultAdapter.IsDiscovering)
                {
                    BluetoothAdapter.DefaultAdapter.CancelDiscovery();
                }
                DeviceConnected.IsConnecting = true;
                OnPropertyChanged(nameof(DeviceConnected));
                try
                {
                    await _socket.ConnectAsync();

                    DeviceConnected.IsConnecting = false;
                    DeviceConnected.IsConnected = true;

                    _bluetoothGatt = DeviceConnected.Device.ConnectGatt(Android.App.Application.Context, true, new GattCallback());
                    var service = _bluetoothGatt.GetService(serviceUuid);
                    if (service != null)
                    {
                        var characteristic = service.GetCharacteristic(characteristicUuid);
                        if (characteristic != null)
                        {
                            var value = characteristic.GetValue();
                            System.Diagnostics.Debug.WriteLine($"Characteristic Value: {Encoding.UTF8.GetString(value)}");
                        }
                    }

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

        public void Disconnect()
        {
            if (_socket != null && _socket.IsConnected)
            {
                _socket.Close();
                _socket.Dispose();
                System.Diagnostics.Debug.WriteLine("Disconnect");
            }
        }

        private async void StartListening()
        {
            var buffer = new byte[1024];
            while (_socket.IsConnected)
            {
                try
                {
                    int bytesRead = await _socket.InputStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        System.Diagnostics.Debug.WriteLine($"Received data: {data}");
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Error reading from socket: {ex.Message}");
                    break;
                }
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
            if (device != null && !string.IsNullOrEmpty(device.Name?.Trim()))
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

public class GattCallback : BluetoothGattCallback
{
    public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
    {
        base.OnConnectionStateChange(gatt, status, newState);
        if (newState == ProfileState.Connected)
        {
            gatt.DiscoverServices();
        }
    }

    public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
    {
        base.OnServicesDiscovered(gatt, status);
        if (status == GattStatus.Success)
        {
            foreach (var service in gatt.Services)
            {
                Console.WriteLine("Service: " + service.Uuid);

                foreach (var characteristic in service.Characteristics)
                {
                    Console.WriteLine("Characteristic: " + characteristic.Uuid);
                    gatt.ReadCharacteristic(characteristic);
                }
            }
        }
    }

    public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
    {
        base.OnCharacteristicRead(gatt, characteristic, status);
        if (status == GattStatus.Success)
        {
            var value = characteristic.GetStringValue(0);
            Console.WriteLine("Characteristic Value: " + value);
        }
    }
}