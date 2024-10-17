namespace Bluetooth.Pages
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
            try
            {
                BluetoothServiceManager bluetoothServiceManager = new BluetoothServiceManager();
                bluetoothServiceManager.StartBluetooth();
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
            }
        }
    }

}
