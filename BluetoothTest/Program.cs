using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;


BluetoothLEDevice? device = null;

var watcher = DeviceInformation.CreateWatcher(
    BluetoothLEDevice.GetDeviceSelectorFromPairingState(false)
);

watcher.Added += Watcher_Added;
watcher.Stopped += Watcher_Stopped;


async void Watcher_Added(DeviceWatcher sender, DeviceInformation info)
{
    Console.WriteLine($"Found {info.Name}: {info.Id}");
    if(device == null)
    {
        watcher.Stop();
        device = await BluetoothLEDevice.FromIdAsync(info.Id);
        await ListServices(device);
    }
}

async Task ListServices(BluetoothLEDevice device)
{
    GattDeviceServicesResult result = await device.GetGattServicesAsync();
    if(result.Status == GattCommunicationStatus.Success)
    {
        var services = result.Services;
        foreach(var s in services)
        {
            Console.WriteLine($"Service {s.Uuid}");
            await ListCharacteristics(s);
        }
    } else
    {
        Console.WriteLine("Failed to get services");
    }
}

async Task ListCharacteristics(GattDeviceService service)
{
    GattCharacteristicsResult result = await service.GetCharacteristicsAsync();
    if (result.Status == GattCommunicationStatus.Success)
    {
        var characteristics = result.Characteristics;
        foreach (var c in characteristics)
        {
            Console.WriteLine($"> Characteristic {c.Uuid}");
            await ReadIfApplicable(c);
        }
    }
    else
    {
        Console.WriteLine("Failed to get characteristics");
    }
}

async Task ReadIfApplicable(GattCharacteristic characteristic) {
    var properties = characteristic.CharacteristicProperties;
    if(properties.HasFlag(GattCharacteristicProperties.Read))
    {
        GattReadResult result = await characteristic.ReadValueAsync();
        if(result.Status == GattCommunicationStatus.Success)
        {
            var reader = DataReader.FromBuffer(result.Value);
            byte[] data = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(data);
            Console.WriteLine($"Read {data.Length} bytes");
        }
        else
        {
            Console.WriteLine($"Failed to read from {characteristic.Uuid}");
        }
    }
    else
    {
        Console.WriteLine($"Cannot read from {characteristic.Uuid}");
    }
}

void Watcher_Stopped(DeviceWatcher sender, object args)
{
    Console.WriteLine("Watcher Stopped");
}

watcher.Start();

Console.WriteLine("Press ENTER to quit");
Console.ReadLine();

device?.Dispose();
