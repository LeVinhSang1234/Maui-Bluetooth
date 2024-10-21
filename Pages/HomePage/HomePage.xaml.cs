using Bluetooth.Services;

namespace Bluetooth.Pages
{
    public partial class HomePage : ContentPage
    {
        public BluetoothManager? bluetoothManager;

        public HomePage()
        {
            InitializeComponent();
            try
            {
                bluetoothManager = new BluetoothManager();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Message => {e.Message}");
            }
            BindingContext = bluetoothManager;
        }

        private void DevicesListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item == null) return;
            BluetoothDevice? selectedDevice = e.Item as BluetoothDevice;
            if (selectedDevice != null)
            {
                _ = bluetoothManager!.ConnectToDeviceAsync(selectedDevice!);
            }
        }
    }

}
