using Android.Bluetooth;
using Android.Content;
using Bluetooth.Models;
using Bluetooth.Services;
using Java.Util;

namespace Bluetooth.Services
{
    public class BluetoothService
    {
        private readonly BluetoothAdapter _adapter;
        private BluetoothSocket _socket;
        private BluetoothGatt _bluetoothGatt;

        public event Action<BluetoothDeviceModel>? OnMyDeviceAdded;
        public event Action<BluetoothDeviceModel?>? OnDeviceConnecting;
        public event Action<BluetoothDevice>? OnDeviceConnected;
        public event Action<BluetoothDevice?, string>? OnDeviceConnectFail;
        public event Action<int>? OnBatteryLevel;
        public event Action<BluetoothDeviceModel>? OnDeviceScan;
        public event Action? OnEndDeviceScan;
        public event Action? OnStartDeviceScan;

        public bool isScanning { get; private set; } = false;
        public bool IsBluetoothEnabled => _adapter.IsEnabled;


        public BluetoothService()
        {
            _adapter = BluetoothAdapter.DefaultAdapter;
            _ = getDevices();
        }

        public async Task getDevices()
        {
            await RequestPermission();
            var pairedDevices = _adapter.BondedDevices;
            if (pairedDevices != null && pairedDevices.Count > 0)
            {
                foreach (var device in pairedDevices)
                {
                    var _device = new BluetoothDeviceModel()
                    {
                        Device = device
                    };
                    OnMyDeviceAdded?.Invoke(_device);
                }
            }
            _ = StartScanAsync();
        }

        public async Task StartScanAsync()
        {
            while (true)
            {
                bool allowBluetooth = await RequestPermission();
                if (!allowBluetooth)
                {
                    await Task.Delay(1000);
                    continue;
                };
                var _receiver = new BluetoothDeviceReceiver(this);
                Platform.CurrentActivity!.RegisterReceiver(_receiver, new IntentFilter(BluetoothDevice.ActionFound));
                _adapter.StartDiscovery();
                OnStartDeviceScan?.Invoke();
                await Task.Delay(10000);
                _adapter.CancelDiscovery();
                Platform.CurrentActivity.UnregisterReceiver(_receiver);
                OnEndDeviceScan?.Invoke();
                await Task.Delay(1000);
            }
        }

        public void DeviceScan(BluetoothDeviceModel device)
        {
            OnDeviceScan?.Invoke(device);
        }

        public void UpdateBatteryLevel(int batteryLevel)
        {
            OnBatteryLevel?.Invoke(batteryLevel);
        }

        public void Connected(BluetoothDevice device)
        {
            OnDeviceConnected?.Invoke(device);
        }

        public void ConnectGatFail(BluetoothDevice device, string message)
        {
            OnDeviceConnectFail?.Invoke(device, message);
        }

        public void ConnectSocketToDevice(BluetoothDeviceModel device)
        {
            OnDeviceConnecting?.Invoke(device);
            try
            {
                device.IsConnecting = true;
                OnDeviceConnecting?.Invoke(device);
                try
                {
                    _bluetoothGatt = device.Device.ConnectGatt(Android.App.Application.Context, false, new GattCallback(this));
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    ConnectGatFail(device.Device, e.Message);
                    OnDeviceConnecting?.Invoke(null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error connecting to device: " + ex.Message);
                _socket?.Close();
            }
        }

        private async Task<bool> RequestPermission()
        {
            await Task.Delay(2000);
            if (_adapter == null || !_adapter.IsEnabled) return false;
            var status = await Permissions.RequestAsync<Permissions.Bluetooth>();
            if (status == PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            return status == PermissionStatus.Granted;
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
            _bluetoothService.Connected(gatt.Device!);
        }
        else _bluetoothService.ConnectGatFail(gatt.Device!, $"Status Fail: {status}");
    }

    public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
    {
        if (status == GattStatus.Success)
        {
            var batteryService = gatt.GetService(UUID.FromString("0000180f-0000-1000-8000-00805f9b34fb"));  // Battery Service
            if (batteryService != null)
            {
                var batteryLevelCharacteristic = batteryService.GetCharacteristic(UUID.FromString("00002a19-0000-1000-8000-00805f9b34fb")); // Battery Level
                if (batteryLevelCharacteristic != null)
                {
                    gatt.ReadCharacteristic(batteryLevelCharacteristic);
                } else System.Diagnostics.Debug.WriteLine("Battery Level characteristic not found.");
            } else System.Diagnostics.Debug.WriteLine("Battery Service characteristic not found.");
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

public class BluetoothDeviceReceiver : BroadcastReceiver
{
    private readonly BluetoothService bluetoothService;
    public BluetoothDeviceReceiver(BluetoothService _bluetoothService)
    {
        bluetoothService = _bluetoothService;
    }
    public override void OnReceive(Context context, Intent intent)
    {
        var action = intent?.Action;
        if (action == BluetoothDevice.ActionFound)
        {
            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
            if (device != null)
            {
                //var rssi = intent.GetShortExtra(BluetoothDevice.ExtraRssi, 0);
                var _device = new BluetoothDeviceModel()
                {
                    Device = device,
                    //Rssi = rssi,
                };
                bluetoothService.DeviceScan(_device);
            }
        }
    }
}