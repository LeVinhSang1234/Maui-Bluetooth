public class BluetoothNotOnException : Exception
{
    public BluetoothNotOnException() : base("Bluetooth is not enabled")
    {
    }

    public BluetoothNotOnException(string message) : base(message)
    {
    }

    public BluetoothNotOnException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
