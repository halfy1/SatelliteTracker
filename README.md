# 🛰️ SatelliteTracker

**SatelliteTracker** — это серверное приложение на ASP.NET Core, предназначенное для приёма, обработки и трансляции GPS/NMEA-данных со спутниковых приёмников в реальном времени через WebSocket.

## 🛰️ Симуляция GPS-данных (Simulation Mode)

Приложение поддерживает два режима получения GPS-данных:
- ✅ **Реальный режим** — данные читаются с COM-порта (`SimulatedGpsReader`)
- 🧪 **Режим симуляции** — данные читаются из текстового файла с NMEA-строками (`FileGpsReader`)

## Технологии

- .NET 7 / C#
- ASP.NET Core WebAPI
- Entity Framework Core с PostgreSQL
- NMEA парсер с поддержкой нескольких типов сообщений
- Логирование через Microsoft.Extensions.Logging
- Возможность работы с реальным COM-портом или симуляция с тестовыми данными

### ⚙️ Настройка симуляции

Для включения симуляции установите флаг `SimulationMode` в `true` в файле `appsettings.json`:

```json
"GpsSettings": {
  "SimulationMode": true,
  "SimulationDataFilePath": "Nmea_src/output.nmea",
  "UpdateIntervalMs": 1000,
  "PortName": "COM3",
  "BaudRate": 9600,
  "DataBits": 8,
  "Parity": "None",
  "StopBits": "One",
  "WebSocketPath": "/ws"
}
```

## QuicStart

1. Клонировать репозиторий и открыть в IDE
```bash
git clone https://github.com/halfy1/SatelliteTracker
```
2. Настроить строку подключения к базе PostgreSQL в `appsettings.json`
	Обязательно заполнить поля `Username` , `Password`
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=SatelliteTracker;Username=___;Password=___"
}
```
3. Выполнить миграции для создания таблиц:
```bash
dotnet ef database update
```
4. Запустить проект:
```bash
dotnet run
```
5. В режиме симуляции данные NMEA будут подгружаться из файла (или COM-порта при отключении симуляции)

## Основные компоненты

- `SimulatedGpsReader` — источник GPS данных (симуляция или COM-порт)
- `NmeaParserService` — сервис для парсинга и сохранения NMEA сообщений
- `SatelliteDataRepository` — репозиторий для работы с базой данных
- `AppDbContext` — контекст базы с моделью `SatelliteData`

## TODO / планы

 - [ ] Поддержка дополнительных типов NMEA сообщений (GPGSA, GPRMC и другие)
 - [ ] Визуализация положения спутников и треков на карте 
 - [ ] Поддержка нескольких спутниковых систем (GLONASS, Galileo и т.д.)