using Bluetooth.Models;
using Bluetooth.Services;

namespace Bluetooth.Pages
{
    public partial class HomePage : ContentPage
    {
        public BluetoothService? bluetoothService;

        public HomePage()
        {
            InitializeComponent();
            try
            {
                bluetoothService = new BluetoothService();
                BindingContext = bluetoothService;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Message => {e.Message}");
            }
        }

        private void DevicesListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item == null) return;
            BluetoothDeviceModel Item = (BluetoothDeviceModel)e.Item;
            _ = bluetoothService!.ConnectSocketToDevice((Item));
        }
    }

}
