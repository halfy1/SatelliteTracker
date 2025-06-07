namespace SatelliteTracker.Backend.Services.Gps
{
    public class GpsSettings
    {
        public string PortName { get; set; }  // Имя порта для GPS устройства
        public int BaudRate { get; set; }     // Скорость передачи данных (например, 9600)
        public int DataBits { get; set; }     // Количество бит данных
        public string Parity { get; set; }    // Паритет
        public string StopBits { get; set; }  // Стоп-биты
        public bool SimulationMode { get; set; }  // Режим симуляции
        public string WebSocketPath { get; set; }  // Путь для WebSocket
        public int UpdateIntervalMs { get; set; }  // Интервал обновления в миллисекундах
    }
}
