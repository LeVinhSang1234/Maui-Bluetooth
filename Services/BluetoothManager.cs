using CoreBluetooth;

public class BluetoothServiceManager
{
    public void StartBluetooth()
    {
        var centralManager = new CBCentralManager(new CentralManagerDelegate(), null);
        System.Diagnostics.Debug.WriteLine($"Bluetooth started on iOS {centralManager.State}");
    }
}


public class CentralManagerDelegate : CBCentralManagerDelegate
{
    public override void UpdatedState(CBCentralManager central)
    {
        switch (central.State)
        {
            case CBManagerState.PoweredOn:
                System.Diagnostics.Debug.WriteLine("Bluetooth is powered on.");
                break;
            case CBManagerState.PoweredOff:
                System.Diagnostics.Debug.WriteLine("Bluetooth is powered off.");
                break;
            case CBManagerState.Unauthorized:
                System.Diagnostics.Debug.WriteLine("Bluetooth is not authorized.");
                break;
            case CBManagerState.Unsupported:
                System.Diagnostics.Debug.WriteLine("Bluetooth is not supported on this device.");
                break;
            case CBManagerState.Unknown:
                System.Diagnostics.Debug.WriteLine("Bluetooth is Unknown");
                break;
            default:
                System.Diagnostics.Debug.WriteLine("Bluetooth state changed.");
                break;
        }
    }
}
