using Microsoft.Extensions.Options;
using SatelliteTracker.Backend.Services.Gps;

public class FileGpsReader : IGpsReader
{
    private readonly GpsSettings _settings;
    private string[] _lines;
    private int _index = 0;

    public FileGpsReader(IOptions<GpsSettings> options)
    {
        _settings = options.Value;
        if (!File.Exists(_settings.SimulationDataFilePath))
            throw new FileNotFoundException("‘айл симул€ции GPS не найден", _settings.SimulationDataFilePath);

        _lines = File.ReadAllLines(_settings.SimulationDataFilePath);
    }

    public async Task StartAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_index >= _lines.Length) _index = 0;

            string line = _lines[_index++];
            await onMessageReceived(line);
            await Task.Delay(_settings.UpdateIntervalMs, cancellationToken);
        }
    }
}
