using System.IO.Ports;
using SatelliteTracker.Backend.Services.Gps;

public class SerialGpsReader : IGpsReader
{
    private readonly GpsSettings _settings;

    public SerialGpsReader(GpsSettings settings)
    {
        _settings = settings;
    }

    public async Task StartAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        Parity parity = Enum.TryParse(_settings.Parity, out Parity p) ? p : Parity.None;
        StopBits stopBits = Enum.TryParse(_settings.StopBits, out StopBits s) ? s : StopBits.One;

        using var serialPort = new SerialPort(_settings.PortName, _settings.BaudRate,
            parity, _settings.DataBits, stopBits);

        serialPort.Open();
        using var reader = new StreamReader(serialPort.BaseStream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line != null)
            {
                await onMessageReceived(line);
            }
        }
    }
}
