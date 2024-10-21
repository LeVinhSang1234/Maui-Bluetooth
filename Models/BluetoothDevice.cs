using System.ComponentModel;
using Plugin.BLE.Abstractions.Contracts;

public class BluetoothDevice
{
    public string Name { get; set; }
    public IDevice Device { get; set; }
    public int Rssi { get; set; }

    public bool? IsConnecting { get; set; }
    public bool? IsConnected { get; set; }
}
