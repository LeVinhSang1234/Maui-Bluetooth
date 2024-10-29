using System.ComponentModel;
#if ANDROID
using Android.Bluetooth;
#endif
using Bluetooth.Models;
using Bluetooth.Services;

namespace Bluetooth.Pages
{
    public partial class HomePage : ContentPage, INotifyPropertyChanged
    {
#if IOS
        public BluetoothServiceIOS? bluetoothServiceIOS;
#endif
        public bool IsAndroid { get; set; }
        public bool IsIOS { get; set; }

#if ANDROID
        public BluetoothService? bluetoothService;
        public List<BluetoothDeviceModel> DevicesScan;
        public List<BluetoothDeviceModel> Devices;
        public List<BluetoothDeviceModel> MyDevices;
        public BluetoothDeviceModel? DeviceConnected { get; set; }
        public bool IsDeviceConnected { get; set; }
#endif
#if IOS
        public List<BluetoothDeviceModelIOS> DevicesScanIOS;
        public List<BluetoothDeviceModelIOS> DevicesIOS;
        public List<BluetoothDeviceModelIOS> MyDevicesIOS;
        public BluetoothDeviceModelIOS? DeviceConnectedIOS { get; set; }
        public bool IsDeviceConnectedIOS { get; set; }
#endif

        public HomePage()
        {
            InitializeComponent();
#if ANDROID
            DevicesScan = new List<BluetoothDeviceModel>();
            Devices = new List<BluetoothDeviceModel>();
            MyDevices = new List<BluetoothDeviceModel>();

            IsAndroid = true;
            System.Diagnostics.Debug.WriteLine(DeviceInfo.Platform);
            bluetoothService = new BluetoothService();
            bluetoothService.OnMyDeviceAdded += MyDeviceAdded;
            bluetoothService.OnDeviceConnecting += OnDeviceConnecting;
            bluetoothService.OnDeviceConnected += OnDeviceConnected;
            bluetoothService.OnDeviceConnectFail += OnDeviceConnectFail;
            bluetoothService.OnMessage += OnMessage;
            bluetoothService.OnDeviceScan += OnDeviceScan;
            bluetoothService.OnEndDeviceScan += OnEndDeviceScan;
#endif
#if IOS
                DevicesScanIOS = new List<BluetoothDeviceModelIOS>();
                DevicesIOS = new List<BluetoothDeviceModelIOS>();
                MyDevicesIOS = new List<BluetoothDeviceModelIOS>();
                IsIOS = true;
                bluetoothServiceIOS = new BluetoothServiceIOS();
                bluetoothServiceIOS.OnMyDeviceAdded += MyDeviceIOSAdded;
                bluetoothServiceIOS.OnDeviceDiscovered += OnDeviceDiscovered;
                bluetoothServiceIOS.OnEndScan += OnEndScanIOS;
                bluetoothServiceIOS.OnConnecting += OnConnectingIOS;
                bluetoothServiceIOS.OnConnected += OnConnectedIOS;
                bluetoothServiceIOS.OnMessage += OnMessageIOS;
#endif
            BindingContext = this;
        }

        // ------------------------------ IOS ------------------------------ //
#if IOS
        private void OnDeviceDiscovered(BluetoothDeviceModelIOS device)
        {
            var _deviceScan = DevicesScanIOS.FirstOrDefault(d => d.Device.Id == device.Device.Id);
            var isMyDevice = MyDevicesIOS.FirstOrDefault(d => d.Device.Id == device.Device.Id);
            if (_deviceScan == null && isMyDevice == null && !string.IsNullOrEmpty(device.Device.Name))
            {
                DeviceIOSAdded(device);
            }
        }

        private void DeviceIOSAdded(BluetoothDeviceModelIOS device)
        {
            if (!DevicesIOS.Any(d => d.Device.Id == device.Device.Id))
            {
                DeviceView.Children.Add(new Label
                {
                    Text = device.DisplayName,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Start,
                    Margin = new Thickness(0, 8, 8, 0),
                    GestureRecognizers =
                    {
                        new TapGestureRecognizer
                        {
                            Command = new Command(() =>
                            {
                                OnSelectDeviceIOS(device);
                            })
                        }
                    }
                });
                DevicesScanIOS.Add(device);
                DevicesIOS.Add(device);
            }

        }

        private void OnEndScanIOS()
        {
            for (var i = 0; i < DevicesIOS.Count; i++)
            {
                var _device = DevicesIOS[i];
                if (!DevicesScanIOS.Any(e => e.Device.Id == _device.Device.Id))
                {
                    DevicesIOS.RemoveAt(i);
                    DeviceView.Children.RemoveAt(i);
                }
            }
            DevicesScanIOS.Clear();
        }

        private void MyDeviceIOSAdded(BluetoothDeviceModelIOS device)
        {
            if (MyDevicesIOS.Any(d => d.Device.Id == device.Device.Id)) return;
            MyDevicesIOS.Add(device);
            MyDeviceView.Children.Add(new Label
            {
                Text = device.DisplayName,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 8, 8, 0),
                GestureRecognizers =
                {
                    new TapGestureRecognizer
                    {
                        Command = new Command(() =>
                        {
                            OnSelectDeviceIOS(device);
                        })
                    }
                }
            });
        }

        private void OnConnectingIOS(BluetoothDeviceModelIOS device)
        {
            IsDeviceConnectedIOS = true;
            DeviceConnectedIOS = device;
            DeviceConnectedIOS.IsConnected = false;
            OnPropertyChanged(nameof(IsDeviceConnectedIOS));
            OnPropertyChanged(nameof(DeviceConnectedIOS));
        }


        private void OnConnectedIOS(BluetoothDeviceModelIOS device)
        {
            IsDeviceConnectedIOS = true;
            DeviceConnectedIOS = device;
            OnPropertyChanged(nameof(IsDeviceConnectedIOS));
            OnPropertyChanged(nameof(DeviceConnectedIOS));
        }

        private void OnMessageIOS(string? message)
        {
            if (DeviceConnectedIOS == null) return;
            if (!string.IsNullOrEmpty(message))
            {
                DeviceConnectedIOS.Result = message;
            }
            OnPropertyChanged(nameof(IsDeviceConnectedIOS));
            OnPropertyChanged(nameof(DeviceConnectedIOS));
        }

        private async void OnSelectDeviceIOS(BluetoothDeviceModelIOS device)
        {
            if (bluetoothServiceIOS == null) return;
            if (DeviceConnectedIOS != null)
            {
                IsDeviceConnectedIOS = false;
                OnPropertyChanged(nameof(IsDeviceConnectedIOS));
                await bluetoothServiceIOS!.DisConnectToDevice(DeviceConnectedIOS);
            }
            bluetoothServiceIOS.ConnectToDevice(device);
        }
        // ------------------------------ IOS ------------------------------ //
#endif
        // ------------------------------ ANDROID ------------------------------ //
#if ANDROID
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
            if (DeviceConnected != null)
            {
                DeviceConnected.IsConnected = false;
                DeviceConnected.Result = string.Empty;
            }
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
            if (string.IsNullOrEmpty(message))
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

        private void MyDeviceAdded(BluetoothDeviceModel device)
        {
            if (MyDevices.Any(d => d.Device.Address == device.Device.Address)) return;
            MyDevices.Add(device);
            MyDeviceView.Children.Add(new Label
            {
                Text = device.DisplayName,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 8, 8, 0),
                GestureRecognizers =
                {
                    new TapGestureRecognizer
                    {
                        Command = new Command(() =>
                        {
                            OnSelectDevice(device);
                        })
                    }
                }
            });
        }

        private void DeviceAdded(BluetoothDeviceModel myDevice)
        {
            if (!Devices.Any(d => d.Device.Address == myDevice.Device.Address))
            {
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
            bluetoothService!.ConnectToDevice(device);
        }
#endif
        // ------------------------------ ANDROID ------------------------------ //
    }
}
