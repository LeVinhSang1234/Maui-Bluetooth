using System.ComponentModel;
using CoreBluetooth;

public class BluetoothDevice : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _isConnecting;
    private bool _isConnected;
    public event PropertyChangedEventHandler? PropertyChanged;

    public required CBPeripheral cBPeripheral { get; set; }
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }
    }

    public float Rssi { get; set; }

    public bool IsConnecting
    {
        get => _isConnecting;
        set
        {
            if (_isConnecting != value)
            {
                _isConnecting = value;
                OnPropertyChanged(nameof(IsConnecting));
            }
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

