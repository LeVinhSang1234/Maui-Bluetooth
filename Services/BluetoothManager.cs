using System.Collections.ObjectModel;
using System.ComponentModel;
using CoreBluetooth;
using Foundation;

namespace Bluetooth.Services
{
    public class BluetoothServiceManager : INotifyPropertyChanged
    {
        private CBCentralManager _centralManager;
        private CentralManagerDelegate _delegate;
        private readonly HashSet<BluetoothDevice> _DevicesScan = new HashSet<BluetoothDevice>();

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<BluetoothDevice> Devices { get; set; } = new ObservableCollection<BluetoothDevice>();
        public bool IsGetting { get; set; }

        public async void InitializeBluetooth()
        {
            await Task.Delay(1000);
            _delegate = new CentralManagerDelegate(this);
            _centralManager = new CBCentralManager(_delegate, null);
            StartScanDevice();
        }

        public async void StartScanDevice()
        {
            while (true)
            {
                if (_centralManager.State == CBManagerState.PoweredOn)
                {
                    IsGetting = true;
                    OnPropertyChanged(nameof(IsGetting));
                    _centralManager.ScanForPeripherals(peripheralUuids: new CBUUID[0]);
                    await Task.Delay(2000);
                    IsGetting = false;
                    OnPropertyChanged(nameof(IsGetting));
                    StopScanning();
                    await Task.Delay(2000);
                } else await Task.Delay(1000);
            }
        }

        public void AddDevice(BluetoothDevice device)
        {
            if (_DevicesScan.Add(device) && !Contains(device.cBPeripheral))
            {
                Devices.Add(device);
                OnPropertyChanged(nameof(Devices));
            }
        }

        public bool Contains(CBPeripheral cBPeripheral)
        {
            return Devices.Any(device => device.cBPeripheral.Name == cBPeripheral.Name);
        }


        public bool IsRemove(CBPeripheral cBPeripheral)
        {
            return !_DevicesScan.Any(device => device.cBPeripheral.Name == cBPeripheral.Name);
        }

        public void Connect(BluetoothDevice device)
        {
            var _device = Devices.FirstOrDefault(d => d.cBPeripheral?.Identifier == device.cBPeripheral.Identifier);
            if (_device != null)
            {
                DisconnectAllExcept();
                _device.IsConnecting = true;
                _centralManager.ConnectPeripheral(_device.cBPeripheral);
                OnPropertyChanged(nameof(Devices));
            }
        }

        public void DisconnectAllExcept()
        {
            foreach (var device in Devices)
            {
                if (device.IsConnected || device.IsConnecting)
                {
                    device.IsConnecting = false;
                    _centralManager.CancelPeripheralConnection(device.cBPeripheral);
                }
            }
        }

        public void Disconnected(CBPeripheral cBPeripheral)
        {
            var _device = Devices.FirstOrDefault(d => d.cBPeripheral?.Identifier == cBPeripheral.Identifier);
            if(_device != null)
            {
                _device.IsConnected = false;
                OnPropertyChanged(nameof(Devices));
            }
        }


        public void Connected(CBPeripheral cBPeripheral)
        {
            var _device = Devices.FirstOrDefault(d => d.cBPeripheral?.Identifier == cBPeripheral.Identifier);
            if (_device != null)
            {
                _device.IsConnecting = false;
                _device.IsConnected = true;
                OnPropertyChanged(nameof(Devices));
            }
        }

        private void UpdateDevices()
        {
            var devicesToRemove = Devices.Where(d => IsRemove(d.cBPeripheral)).ToList();
            foreach (var device in devicesToRemove)
            {
                Devices.Remove(device);
            }

            foreach (var scanDevice in _DevicesScan)
            {
                if (!Contains(scanDevice.cBPeripheral))
                {
                    Devices.Add(scanDevice);
                }
            }
            OnPropertyChanged(nameof(Devices));
            _DevicesScan.Clear();
        }

        public void StopScanning()
        {
            try
            {
                _centralManager.StopScan();
                UpdateDevices();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
