namespace SatelliteTracker.Backend.Services.Gps
{
    public class GpsSettings
    {
        public string PortName { get; set; }  // ��� ����� ��� GPS ����������
        public int BaudRate { get; set; }     // �������� �������� ������ (��������, 9600)
        public int DataBits { get; set; }     // ���������� ��� ������
        public string Parity { get; set; }    // �������
        public string StopBits { get; set; }  // ����-����
        public bool SimulationMode { get; set; }  // ����� ���������
        public string WebSocketPath { get; set; }  // ���� ��� WebSocket
        public int UpdateIntervalMs { get; set; }  // �������� ���������� � �������������
    }
}
