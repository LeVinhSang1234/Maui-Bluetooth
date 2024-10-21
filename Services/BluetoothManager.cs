using System.Collections.ObjectModel;
using System.ComponentModel;
using CoreBluetooth;
using Foundation;
using Microsoft.Maui.Controls;

namespace Bluetooth.Services
{
    public class BluetoothServiceManager : INotifyPropertyChanged
    {
        private CBCentralManager _centralManager;
        private CentralManagerDelegate _delegate;
        private readonly HashSet<BluetoothDevice> _DevicesScan = new HashSet<BluetoothDevice>();

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<BluetoothDevice> Devices { get; set; } = new ObservableCollection<BluetoothDevice>();
        public BluetoothDevice? DeviceConnect { get; set; }
        public bool IsGetting { get; set; }

        public bool IsDeviceConnected => DeviceConnect != null;

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
                    await Task.Delay(5000);
                    StopScanning();
                    await Task.Delay(100);
                }
                else await Task.Delay(1000);
            }
        }

        public void AddDevice(BluetoothDevice device)
        {
            if (_DevicesScan.Add(device) && !Contains(device.cBPeripheral) && device.Name != DeviceConnect?.Name)
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
            var _device = Devices.FirstOrDefault(d => d.cBPeripheral?.Name == device.cBPeripheral.Name);
            if (_device != null)
            {
                DisconnectAllExcept();
                Devices.Remove(_device);
                DeviceConnect = _device;
                DeviceConnect.IsConnecting = true;
                OnPropertyChanged(nameof(IsDeviceConnected));
                OnPropertyChanged(nameof(DeviceConnect));
                _centralManager.ConnectPeripheral(_device.cBPeripheral);
            }
        }

        public void DisconnectAllExcept()
        {
            if (DeviceConnect != null)
            {
                DeviceConnect.IsConnected = false;
                DeviceConnect.IsConnecting = false;
                DeviceConnect.ManuFacturerName = string.Empty;
                Devices.Add(DeviceConnect);
                _centralManager.CancelPeripheralConnection(DeviceConnect.cBPeripheral);
                DeviceConnect = null;
            }
        }

        public void Disconnected(CBPeripheral peripheral)
        {
        }


        public void Connected(CBPeripheral cBPeripheral)
        {
            if (DeviceConnect != null && DeviceConnect.Name == cBPeripheral.Name)
            {
                DeviceConnect.IsConnecting = false;
                DeviceConnect.IsConnected = true;
                OnPropertyChanged(nameof(DeviceConnect));
            }
        }


        public void ConnecteFail(CBPeripheral cBPeripheral)
        {
            if (DeviceConnect != null && DeviceConnect.Name == cBPeripheral.Name)
            {
                DeviceConnect = null;
                OnPropertyChanged(nameof(DeviceConnect));
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
                if (!Contains(scanDevice.cBPeripheral) && scanDevice.Name != DeviceConnect?.Name)
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
