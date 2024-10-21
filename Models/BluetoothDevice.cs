using CoreBluetooth;

public class BluetoothDevice
{
    public required CBPeripheral cBPeripheral { get; set; }
    public required string Name { get; set; }
    public string ManuFacturerName { get; set; }
    public bool IsConnected { get; set; }
    public bool IsConnecting { get; set; }
    public float Rssi { get; set; }
}

