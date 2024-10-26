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
                BindingContext = new BluetoothService();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Message => {e.Message}");
            }
        }

        private void DevicesListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item == null) return;
        }
    }

}
