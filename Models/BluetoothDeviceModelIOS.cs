using Plugin.BLE.Abstractions.Contracts;

namespace Bluetooth.Models
{
	public class BluetoothDeviceModelIOS
	{
        public IDevice Device { get; set; }
        public string DisplayName => string.IsNullOrEmpty(Device.Name) ? $"Unknown" : Device.Name;
        //public short Rssi { get; set; }
        public bool IsConnecting { get; set; }
        public bool IsConnected { get; set; }
        public string Result { get; set; }
    }
}

