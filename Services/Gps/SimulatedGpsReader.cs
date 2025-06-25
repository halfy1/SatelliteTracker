using Microsoft.Extensions.Options;
using SatelliteTracker.Backend.Services.Gps;
using System.Globalization;
using System.IO.Ports;

public class SimulatedGpsReader : IGpsReader
{
    private readonly GpsSettings _settings;
    private readonly Random _random = new();
    private SerialPort _serialPort;  // Объект для работы с COM-портом
    private string _sentence;

    public SimulatedGpsReader(IOptions<GpsSettings> options)
    {
        _settings = options.Value;
    }

    public async Task StartAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            StartReading(_settings.PortName, _settings.BaudRate);

            await onMessageReceived(_sentence);
            await Task.Delay(_settings.UpdateIntervalMs, cancellationToken);
            StopReading();
        }
        
    }

    // Запуск чтения данных
    public void StartReading(string portName, int baudRate = 9600)
    {
        // Создаём и настраиваем SerialPort
        _serialPort = new SerialPort(portName, baudRate)
        {
            Parity = Parity.None,     // Без контроля чётности
            DataBits = 8,            // 8 бит данных
            StopBits = StopBits.One,  // 1 стоп-бит
            Handshake = Handshake.None // Без управления потоком
        };

        // Подписываемся на событие получения данных
        _serialPort.DataReceived += SerialPortDataReceived;

        // Открываем порт
        _serialPort.Open();
    }

    // Обработчик события получения данных
    private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        // Читаем строку из порта (NMEA-сообщение заканчивается \r\n)
        _sentence = _serialPort.ReadLine();

        // Выводим в консоль (можно заменить на запись в БД или отправку в веб-интерфейс)
        //Console.WriteLine(_sentence);
    }

    // Остановка чтения и освобождение ресурсов
    public void StopReading()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();     // Закрываем порт
            _serialPort.Dispose();    // Освобождаем ресурсы
        }
    }


}
