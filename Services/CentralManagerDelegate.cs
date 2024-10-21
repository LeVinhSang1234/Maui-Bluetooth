using Bluetooth.Services;
using CoreBluetooth;
using Foundation;

public class CentralManagerDelegate : CBCentralManagerDelegate
{
    private readonly BluetoothServiceManager _bluetoothServiceManager;

    public CentralManagerDelegate(BluetoothServiceManager bluetoothServiceManager)
    {
        _bluetoothServiceManager = bluetoothServiceManager;
    }

    public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
    {
        if (advertisementData.ContainsKey(CBAdvertisement.IsConnectable))
        {
            if (peripheral.Name != null)
            {
                _bluetoothServiceManager.AddDevice(new BluetoothDevice { cBPeripheral = peripheral, Name = peripheral.Name ?? string.Empty, Rssi = RSSI.FloatValue });
            }
        }
    }

    public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
    {
        _bluetoothServiceManager.Connected(peripheral);
        peripheral.Delegate = new PeripheralDelegate();
        peripheral.DiscoverServices();
    }

    public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError? error)
    {
        _bluetoothServiceManager.ConnecteFail(peripheral);
    }

    public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError? error)
    {
        _bluetoothServiceManager.Disconnected(peripheral);
    }

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
            case CBManagerState.Unsupported:
                System.Diagnostics.Debug.WriteLine("Bluetooth is not supported on this device.");
                break;
            case CBManagerState.Unauthorized:
                System.Diagnostics.Debug.WriteLine("Bluetooth access is unauthorized.");
                break;
            case CBManagerState.Resetting:
                System.Diagnostics.Debug.WriteLine("Bluetooth is resetting.");
                break;
            case CBManagerState.Unknown:
                System.Diagnostics.Debug.WriteLine("Bluetooth state is unknown.");
                break;
        }
    }
}


public class PeripheralDelegate : CBPeripheralDelegate
{
    // Khi tìm thấy dịch vụ
    public override void DiscoveredService(CBPeripheral peripheral, NSError? error)
    {
        if (error != null || peripheral.Services == null)
        {
            System.Diagnostics.Debug.WriteLine($"Error discovering services: {error?.LocalizedDescription}");
            return;
        }

        foreach (var service in peripheral.Services!)
        {
            System.Diagnostics.Debug.WriteLine($"Discovered service: {service.UUID}");
            peripheral.DiscoverCharacteristics(service);
        }
    }

    public override void DiscoveredCharacteristics(CBPeripheral peripheral, CBService service, NSError? error)
    {
        if (error != null || peripheral.Services == null)
        {
            System.Diagnostics.Debug.WriteLine($"Error discovering characteristics: {error?.LocalizedDescription}");
            return;
        }

        foreach (var characteristic in service.Characteristics!)
        {
            if (characteristic.Properties.HasFlag(CBCharacteristicProperties.Notify))
            {
                peripheral.SetNotifyValue(true, characteristic);
                System.Diagnostics.Debug.WriteLine($"Characteristic {characteristic.UUID} {characteristic.Value} support Notify.");
            }
            else if (characteristic.Properties.HasFlag(CBCharacteristicProperties.Read))
            {
                peripheral.ReadValue(characteristic);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Characteristic {characteristic.UUID} does not support read.");
            }
        }
    }

    public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
    {
        if (error != null)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading characteristic value: {error.LocalizedDescription}");
            return;
        }

        var data = characteristic.Value;
        if (data != null)
        {
            var dataString = NSString.FromData(data, NSStringEncoding.UTF8);
            System.Diagnostics.Debug.WriteLine($"Characteristic value: {characteristic.UUID} => {dataString}");
        }
    }

    public override void UpdatedValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError? error)
    {
        if (error == null)
        {
            var descriptorValue = descriptor.Value;
            System.Diagnostics.Debug.WriteLine($"Descriptor value: {descriptorValue}");

            if (descriptorValue != null)
            {
                var stringValue = descriptorValue.ToString();
                System.Diagnostics.Debug.WriteLine($"Received descriptor string: {stringValue}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Error reading descriptor value: {error.LocalizedDescription}");
        }
    }
}
