// Конфигурация
const config = {
    mapCenter: [55.76, 37.64], // Центр карты (Москва)
    mapZoom: 5,
    websocketUrl: `ws://${window.location.host}/ws`,
    satelliteIconColors: {
        'GPS': '#3498db',
        'GLONASS': '#e74c3c',
        'Galileo': '#2ecc71',
        'default': '#9b59b6'
    }
};

// Состояние приложения
const state = {
    map: null,
    satellites: new Map(), // Хранит все спутники (id -> {data, placemark})
    connectionStatus: 'disconnected'
};

// Инициализация карты
ymaps.ready(() => {
    state.map = new ymaps.Map('map', {
        center: config.mapCenter,
        zoom: config.mapZoom,
        controls: ['zoomControl', 'typeSelector']
    });

    // Создаем свой пресет для иконок спутников
    ymaps.option.presetStorage.add('satelliteIcon', {
        iconLayout: 'default#imageWithContent',
        iconImageHref: 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0iY3VycmVudENvbG9yIiBkPSJNMTIsMkM2LjQ4LDIgMiw2LjQ4IDIsMTJzNC40OCwxMCAxMCwxMCAxMC00LjQ4IDEwLTEwUzE3LjUyLDIgMTIsMnogTTEyLDIwYy00LjQyLDAtOC0zLjU4LTgtOHMzLjU4LTggOC04IDgsMy41OCA4LDhTMTYuNDIsMjAgMTIsMjB6Ii8+PC9zdmc+',
        iconImageSize: [24, 24],
        iconImageOffset: [-12, -12]
    });

    connectWebSocket();
    addTestData(); // Для тестирования
});


// WebSocket подключение
function connectWebSocket() {
    const socket = new WebSocket(config.websocketUrl);

    socket.onopen = () => {
        updateStatus('connected');
        console.log('WebSocket connected');
    };

    socket.onclose = () => {
        updateStatus('disconnected');
        console.log('WebSocket disconnected');
        setTimeout(connectWebSocket, 5000);
    };

    socket.onerror = (error) => {
        console.error('WebSocket error:', error);
        updateStatus('error');
    };

    socket.onmessage = (event) => {
        try {
            const data = JSON.parse(event.data);
            console.log('Данные спутника:', data);

            if (data.Latitude && data.Longitude) {
                updateSatellite({
                    SatelliteId: data.Id, // исправлено с data.Id
                    SatelliteSystem: data.SatelliteSystem || 'Unknown', // исправлено с data.SatelliteSystem
                    Latitude: data.Latitude,
                    Longitude: data.Longitude,
                    Altitude: data.Altitude,
                    Elevation: data.Elevation,
                    Azimuth: data.Azimuth,
                    SignalToNoiseRatio: data.SignalToNoiseRatio, // исправлено с data.SignalToNoiseRatio
                    Timestamp: data.Timestamp,
                    UsedInFix: data.UsedInFix
                });

                // Обновляем данные GPS в панели
                document.getElementById('gps-data').textContent = JSON.stringify(data, null, 2);
            }
        } catch (e) {
            console.error('Ошибка парсинга:', e, 'Данные:', event.data);
        }
    };
}

// Обновление/добавление спутника на карте
function updateSatellite(data) {
    const id = `${data.SatelliteSystem}-${data.SatelliteId}`;
    const coords = [data.Latitude, data.Longitude];
    
    if (!state.satellites.has(id)) {
        // Создаем новую метку
        const placemark = new ymaps.Placemark(
            coords,
            {
                hintContent: createTooltip(data),
                balloonContent: createBalloon(data)
            },
            {
                preset: 'satelliteIcon',
                iconColor: config.satelliteIconColors[data.SatelliteSystem] || 
                          config.satelliteIconColors.default
            }
        );
        
        state.map.geoObjects.add(placemark);
        state.satellites.set(id, { data, placemark });
        console.log(`Added satellite ${id}`);
    } else {
        // Обновляем существующую метку
        const satellite = state.satellites.get(id);
        satellite.placemark.geometry.setCoordinates(coords);
        satellite.data = data;
    }
    
    updateCounter();
}

// Создание содержимого подсказки
function createTooltip(data) {
    return `
        <b>${data.SatelliteSystem} #${data.SatelliteId}</b><br>
        Координаты: ${data.Latitude.toFixed(4)}, ${data.Longitude.toFixed(4)}<br>
        Высота: ${data.Altitude || 'N/A'} м
    `;
}

// Создание содержимого балуна
function createBalloon(data) {
    return `
        <div class="satellite-info">
            <h3>${data.SatelliteSystem} #${data.SatelliteId}</h3>
            <p><b>Координаты:</b> ${data.Latitude.toFixed(6)}, ${data.Longitude.toFixed(6)}</p>
            <p><b>Высота:</b> ${data.Altitude || 'N/A'} м</p>
            <p><b>Элевация:</b> ${data.Elevation || 'N/A'}°</p>
            <p><b>Азимут:</b> ${data.Azimuth || 'N/A'}°</p>
            <p><b>SNR:</b> ${data.SignalToNoiseRatio || 'N/A'} dB</p>
            <p><b>Время:</b> ${new Date(data.Timestamp).toLocaleString()}</p>
        </div>
    `;
}

// Обновление статуса подключения
function updateStatus(status) {
    state.connectionStatus = status;
    const el = document.getElementById('connection-status');
    el.textContent = {
        connected: 'Подключено',
        disconnected: 'Отключено',
        error: 'Ошибка'
    }[status];
    el.className = status;
}

// Обновление счетчика спутников
function updateCounter() {
    const count = state.satellites.size;
    const el = document.getElementById('satellite-count');
    el.textContent = `${count} ${getRussianPlural(count, ['спутник', 'спутника', 'спутников'])}`;
}

// Вспомогательная функция для склонения
function getRussianPlural(number, titles) {
    const cases = [2, 0, 1, 1, 1, 2];
    return titles[
        number % 100 > 4 && number % 100 < 20 ? 2 : cases[Math.min(number % 10, 5)]
    ];
}

// Тестовые данные для проверки
function addTestData() {
    // Тестовая метка позиции
    const positionMarker = new ymaps.Placemark(
        config.mapCenter,
        {
            hintContent: 'Тестовая позиция',
            balloonContent: 'Москва, Кремль'
        },
        {
            preset: 'islands#redDotIcon'
        }
    );
    state.map.geoObjects.add(positionMarker);
    
    // Тестовый спутник
    setTimeout(() => {
        const testSatellite = {
            SatelliteId: 99,
            SatelliteSystem: "GPS",
            Latitude: 55.7558,
            Longitude: 37.6176,
            Altitude: 150,
            Elevation: 45,
            Azimuth: 120,
            SignalToNoiseRatio: 35,
            Timestamp: new Date().toISOString(),
            UsedInFix: true
        };
        updateSatellite(testSatellite);
        console.log("Тестовый спутник добавлен");
    }, 3000);
}