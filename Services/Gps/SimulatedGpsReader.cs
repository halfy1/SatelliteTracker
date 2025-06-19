using Microsoft.Extensions.Options;
using SatelliteTracker.Backend.Services.Gps;
using System.Globalization;
using System.IO.Ports;

public class SimulatedGpsReader : IGpsReader
{
    private readonly GpsSettings _settings;
    private readonly Random _random = new();
    private SerialPort _serialPort;  // ������ ��� ������ � COM-������
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

    // ������ ������ ������
    public void StartReading(string portName, int baudRate = 9600)
    {
        // ������ � ����������� SerialPort
        _serialPort = new SerialPort(portName, baudRate)
        {
            Parity = Parity.None,     // ��� �������� ��������
            DataBits = 8,            // 8 ��� ������
            StopBits = StopBits.One,  // 1 ����-���
            Handshake = Handshake.None // ��� ���������� �������
        };

        // ������������� �� ������� ��������� ������
        _serialPort.DataReceived += SerialPortDataReceived;

        // ��������� ����
        _serialPort.Open();
    }

    // ���������� ������� ��������� ������
    private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        // ������ ������ �� ����� (NMEA-��������� ������������� \r\n)
        _sentence = _serialPort.ReadLine();

        // ������� � ������� (����� �������� �� ������ � �� ��� �������� � ���-���������)
        //Console.WriteLine(_sentence);
    }

    // ��������� ������ � ������������ ��������
    public void StopReading()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();     // ��������� ����
            _serialPort.Dispose();    // ����������� �������
        }
    }


}
