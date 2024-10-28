using System.ComponentModel;
using Android.Bluetooth;
using Bluetooth.Models;
using Bluetooth.Services;

namespace Bluetooth.Pages
{
    public partial class HomePage : ContentPage, INotifyPropertyChanged
    {
        public BluetoothService? bluetoothService;
        public List<BluetoothDeviceModel> DevicesScan;
        public List<BluetoothDeviceModel> Devices;
        public List<BluetoothDeviceModel> MyDevices;

        public BluetoothDeviceModel? DeviceConnected { get; set; }
        public bool IsDeviceConnected { get; set; }

        public HomePage()
        {
            InitializeComponent();
            DevicesScan = new List<BluetoothDeviceModel>();
            Devices = new List<BluetoothDeviceModel>();
            MyDevices = new List<BluetoothDeviceModel>();

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                bluetoothService = new BluetoothService();
                bluetoothService.OnMyDeviceAdded += MyDeviceAdded;
                bluetoothService.OnDeviceConnecting += OnDeviceConnecting;
                bluetoothService.OnDeviceConnected += OnDeviceConnected;
                //bluetoothService.OnDisconnecting += OnDisconnecting;
                bluetoothService.OnDeviceConnectFail += OnDeviceConnectFail;
                bluetoothService.OnMessage += OnMessage;
                bluetoothService.OnDeviceScan += OnDeviceScan;
                bluetoothService.OnEndDeviceScan += OnEndDeviceScan;
            } else
            {

            }
            BindingContext = this;
        }

        private void OnDeviceScan(BluetoothDeviceModel device)
        {
            var _deviceScan = DevicesScan.FirstOrDefault(d => d.Device.Address == device.Device.Address);
            var isMyDevice = MyDevices.FirstOrDefault(d => d.Device.Address == device.Device.Address);
            if (_deviceScan == null && isMyDevice == null && !string.IsNullOrEmpty(device.Device.Name))
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

        private void OnMessage(string? message)
        {
            if (DeviceConnected == null) return;
            if(string.IsNullOrEmpty(message))
            {
                DeviceConnected.IsConnected = false;
            }
            DeviceConnected.Result = message ?? "";
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
            DeviceConnected.Result = "Connected";
            OnPropertyChanged(nameof(DeviceConnected));
        }

        private void MyDeviceAdded(BluetoothDeviceModel myDevice)
        {
            MyDevices.Add(myDevice);
            MyDeviceView.Children.Add(new Label
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
            });
        }

        private void DeviceAdded(BluetoothDeviceModel myDevice)
        {
            if(!Devices.Any(d => d.Device.Address == myDevice.Device.Address)) {
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
                });
            }
            DevicesScan.Add(myDevice);
            Devices.Add(myDevice);

        }

        private void OnEndDeviceScan()
        {
            for (var i = 0; i < Devices.Count; i++)
            {
                var _device = Devices[i];
                if (!DevicesScan.Any(e => e.Device.Address == _device.Device.Address))
                {
                    Devices.RemoveAt(i);
                    DeviceView.Children.RemoveAt(i);
                }
            }
            DevicesScan.Clear();
        }

        private void OnSelectDevice(BluetoothDeviceModel device)
        {
            bluetoothService!.ConnectSocketToDevice(device);
        }
    }

}
