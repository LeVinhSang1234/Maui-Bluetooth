using Android.Bluetooth;

namespace Bluetooth.Models
{
    public class BluetoothDeviceModel
    {
        public BluetoothDevice Device { get; set; }
        public string DisplayName => string.IsNullOrEmpty(Device.Name) ? $"Unknown" : Device.Name;
        //public short Rssi { get; set; }
        public bool Visible => !string.IsNullOrEmpty(DisplayName);
        public bool IsConnecting { get; set; }
        public bool IsConnected { get; set; }
        public string Result { get; set; }
    }
}

