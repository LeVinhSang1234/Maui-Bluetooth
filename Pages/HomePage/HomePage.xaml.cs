using Bluetooth.Services;

namespace Bluetooth.Pages
{
    public partial class HomePage : ContentPage
    {
        private BluetoothServiceManager bluetoothServiceManager = new BluetoothServiceManager();

        public HomePage()
        {
            InitializeComponent();
            try
            {
                bluetoothServiceManager.InitializeBluetooth();
                BindingContext = bluetoothServiceManager;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Message => {e.Message}");
            }
        }

        private void DevicesListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item == null) return;
            BluetoothDevice? selectedDevice = e.Item as BluetoothDevice;
            if (selectedDevice != null)
            {
                System.Diagnostics.Debug.WriteLine($"selectedDevice {selectedDevice.Name}");
                bluetoothServiceManager.Connect(selectedDevice);
            }
        }
    }

}
