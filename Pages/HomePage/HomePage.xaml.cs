using System.Collections.ObjectModel;
using System.ComponentModel;
using Android.Bluetooth;
using Bluetooth.Models;
using Bluetooth.Services;

namespace Bluetooth.Pages
{
    public partial class HomePage : ContentPage, INotifyPropertyChanged
    {
        private BluetoothDeviceModel? _deviceConnected;
        public BluetoothService? bluetoothService;
        public List<BluetoothDeviceModel> DevicesScan;
        public List<BluetoothDeviceModel> Devices;
        public bool IsDeviceConnected { get; set; }
        public BluetoothDeviceModel? DeviceConnected { get; set; }


        public HomePage()
        {
            InitializeComponent();
            DevicesScan = new List<BluetoothDeviceModel>();
            Devices = new List<BluetoothDeviceModel>();

            bluetoothService = new BluetoothService();
            bluetoothService.OnMyDeviceAdded += MyDeviceAdded;
            bluetoothService.OnDeviceConnecting += OnDeviceConnecting;
            bluetoothService.OnDeviceConnected += OnDeviceConnected;
            bluetoothService.OnDeviceConnectFail += OnDeviceConnectFail;
            bluetoothService.OnBatteryLevel += OnBatteryLevel;
            bluetoothService.OnDeviceScan += OnDeviceScan;

            BindingContext = this;
        }

        private void OnDeviceScan(BluetoothDeviceModel device)
        {
            var _deviceScan = DevicesScan.FirstOrDefault(d => d.Device.Address == device.Device.Address);
            var isMyDevice = Devices.FirstOrDefault(d => d.Device.Address == device.Device.Address);
            if (_deviceScan == null && isMyDevice == null)
            {
                DeviceAdded(device);
            }
        }

        private void OnDeviceConnecting(BluetoothDeviceModel? device)
        {
            IsDeviceConnected = device != null;
            DeviceConnected = device;
            OnPropertyChanged(nameof(IsDeviceConnected));
            OnPropertyChanged(nameof(DeviceConnected));
        }

        private void OnDeviceConnectFail(BluetoothDevice? device, string message)
        {
            IsDeviceConnected = false;
            DeviceConnected = null;
            OnPropertyChanged(nameof(IsDeviceConnected));
            OnPropertyChanged(nameof(DeviceConnected));
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert("Connect to Gatt failed!", message, "OK");
            });
        }

        private void OnBatteryLevel(int batteryLevel)
        {
            if (DeviceConnected == null) return;
            DeviceConnected.BatteryLevel = $"BatteryLevel {batteryLevel}%";
            OnPropertyChanged(nameof(DeviceConnected));
        }

        private void OnDeviceConnected(BluetoothDevice device)
        {
            if (DeviceConnected == null)
            {
                DeviceConnected = new BluetoothDeviceModel()
                {
                    Device = device
                };
            }
            DeviceConnected.IsConnecting = false;
            DeviceConnected.IsConnected = true;
            OnPropertyChanged(nameof(DeviceConnected));
        }

        private void MyDeviceAdded(BluetoothDeviceModel myDevice)
        {
            MyDeviceView.Children.Add(new Label
            {
                Text = myDevice.DisplayName,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                GestureRecognizers =
                {
                    new TapGestureRecognizer
                    {
                        Command = new Command(() =>
                        {
                            OnSelectDevice(myDevice);
                        })
                    }
                }
            });
        }

        private void DeviceAdded(BluetoothDeviceModel myDevice)
        {
            DevicesScan.Add(myDevice);
            Devices.Add(myDevice);

            DeviceView.Children.Add(new Label
            {
                Text = myDevice.DisplayName,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 8, 8, 0),
                GestureRecognizers =
                {
                    new TapGestureRecognizer
                    {
                        Command = new Command(() =>
                        {
                            OnSelectDevice(myDevice);
                        })
                    }
                }
            }); ;
        }

        private void OnSelectDevice(BluetoothDeviceModel device)
        {
            _ = bluetoothService!.ConnectSocketToDevice(device);
        }
    }

}
