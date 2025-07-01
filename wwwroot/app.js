// Global variables
let map;
let pathCoordinates = [];
let placemark;
let satelliteChart;
let socket;
let playbackData = [];
let playbackInterval;
let currentPlaybackIndex = 0;
let isPlaying = false;
let playbackSpeed = 1;
const constellationFilters = {
    gps: true,
    glonass: true,
    galileo: true,
    beidou: true
};

const config = {
    satelliteIconColors: {
        'gps': '#3b82f6',
        'glonass': '#ef4444',
        'galileo': '#10b981',
        'beidou': '#f59e0b',
        'default': '#8b5cf6'
    }
};

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    initializeMap();
    initializeCompass();
    initializeTabs();
    initializeChart();
    initializePlaybackControls();
    initializeConstellationFilters();
    setupWebSocketControls();
});

// Initialize Yandex Map
function initializeMap() {
    ymaps.ready(function () {
        map = new ymaps.Map('map', {
            center: [55.7558, 37.6176],
            zoom: 15,
            controls: ['zoomControl', 'typeSelector']
        });

        // Создаем кастомный пресет для иконок спутников
        ymaps.option.presetStorage.add('satelliteIcon', {
            iconLayout: 'default#imageWithContent',
            iconImageHref: 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0iY3VycmVudENvbG9yIiBkPSJNMTIsMkM2LjQ4LDIgMiw2LjQ4IDIsMTJzNC40OCwxMCAxMCwxMCAxMC00LjQ4IDEwLTEwUzE3LjUyLDIgMTIsMnogTTEyLDIwYy00LjQyLDAtOC0zLjU4LTgtOHMzLjU4LTggOC04IDgsMy41OCA4LDhTMTYuNDIsMjAgMTIsMjB6Ii8+PC9zdmc+',
            iconImageSize: [24, 24],
            iconImageOffset: [-12, -12]
        });

        pathCoordinates = new ymaps.Polyline([], {}, {
            strokeColor: '#0000FF',
            strokeWidth: 4,
            strokeOpacity: 0.5
        });
        map.geoObjects.add(pathCoordinates);

        placemark = new ymaps.Placemark([0, 0], {}, {
            preset: 'satelliteIcon',
            iconColor: config.satelliteIconColors.default
        });
        map.geoObjects.add(placemark);
    });
}

// Initialize compass markers
function initializeCompass() {
    const compassMarkers = document.getElementById('compass-markers');
    compassMarkers.innerHTML = ''; // Очищаем перед созданием новых маркеров

    // Создаем маркеры для основных направлений (каждые 45 градусов)
    for (let i = 0; i < 360; i += 45) {
        const marker = document.createElement('div');
        marker.className = 'compass-marker';
        marker.style.transform = `rotate(${i}deg)`;

        // Делаем основные направления (N, E, S, W) более заметными
        if (i % 90 === 0) {
            marker.style.height = '15px';
            marker.style.backgroundColor = '#ef4444';

            const label = document.createElement('div');
            label.className = 'absolute text-xs font-bold text-white';
            label.style.transform = 'translateX(-50%) rotate(' + (-i) + 'deg)';
            label.style.color = 'black';
            label.style.left = '50%';
            label.style.top = '20px';

            let direction;
            if (i === 0) direction = 'С'; // Север
            else if (i === 90) direction = 'В'; // Восток
            else if (i === 180) direction = 'Ю'; // Юг
            else if (i === 270) direction = 'З'; // Запад

            label.textContent = direction;
            marker.appendChild(label);
        }
        compassMarkers.appendChild(marker);
    }
}

// Обновленная секция времени в processData()
if (data.Timestamp) {
    const date = new Date(data.Timestamp);
    const timeElement = document.getElementById('gps-time');
    const dateElement = document.getElementById('gps-date');

    // Форматируем время с секундами
    timeElement.textContent = date.toLocaleTimeString('ru-RU', {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    });

    // Форматируем дату
    dateElement.textContent = date.toLocaleDateString('ru-RU');

    // Добавляем блок с координатами и высотой
    const locationElement = document.createElement('div');
    locationElement.className = 'text-gray-600 text-sm mt-2';

    if (data.Latitude && data.Longitude) {
        const lat = parseFloat(data.Latitude);
        const lng = parseFloat(data.Longitude);
        locationElement.innerHTML = `
            <div>Широта: ${Math.abs(lat).toFixed(4)}° ${lat >= 0 ? 'N' : 'S'}</div>
            <div>Долгота: ${Math.abs(lng).toFixed(4)}° ${lng >= 0 ? 'E' : 'W'}</div>
        `;
    }

    if (data.Altitude) {
        locationElement.innerHTML += `<div>Высота: ${parseFloat(data.Altitude).toFixed(1)} м</div>`;
    }

    // Удаляем старые данные, если есть
    const oldLocation = dateElement.nextElementSibling;
    if (oldLocation && oldLocation.classList.contains('location-data')) {
        oldLocation.remove();
    }

    locationElement.classList.add('location-data');
    dateElement.after(locationElement);
}

// Initialize tab functionality
function initializeTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');

    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            // Remove active class from all buttons and content
            tabButtons.forEach(btn => btn.classList.remove('text-blue-600', 'border-b-2', 'border-blue-600'));
            document.querySelectorAll('.tab-content').forEach(content => {
                content.classList.remove('active');
            });

            // Add active class to clicked button and corresponding content
            button.classList.add('text-blue-600', 'border-b-2', 'border-blue-600');
            const tabId = button.getAttribute('data-tab');
            document.getElementById(tabId).classList.add('active');
        });
    });
}

// Initialize satellite chart
function initializeChart() {
    const ctx = document.getElementById('satelliteChart').getContext('2d');
    satelliteChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: [],
            datasets: [{
                label: 'SNR (dB)',
                data: [],
                backgroundColor: '#3b82f6',
                borderColor: '#1d4ed8',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            scales: {
                y: {
                    beginAtZero: true,
                    max: 50,
                    title: {
                        display: true,
                        text: 'SNR (dB)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Satellite ID'
                    }
                }
            }
        }
    });
}

// Initialize playback controls
function initializePlaybackControls() {
    const playBtn = document.getElementById('play-btn');
    const pauseBtn = document.getElementById('pause-btn');
    const stopBtn = document.getElementById('stop-btn');
    const playbackProgress = document.getElementById('playback-progress');
    const playbackSpeedControl = document.getElementById('playback-speed');
    const speedFactor = document.getElementById('speed-factor');

    playBtn.addEventListener('click', startPlayback);
    pauseBtn.addEventListener('click', pausePlayback);
    stopBtn.addEventListener('click', stopPlayback);

    playbackProgress.addEventListener('input', function () {
        if (!isPlaying) {
            currentPlaybackIndex = parseInt(this.value);
            if (playbackData.length > 0) {
                processData(playbackData[currentPlaybackIndex]);
            }
        }
    });

    playbackSpeedControl.addEventListener('input', function () {
        playbackSpeed = parseFloat(this.value);
        speedFactor.textContent = playbackSpeed.toFixed(1) + 'x';

        if (isPlaying) {
            stopPlayback();
            startPlayback();
        }
    });
}

// Initialize constellation filters
function initializeConstellationFilters() {
    const filterButtons = document.querySelectorAll('.constellation-btn');

    filterButtons.forEach(button => {
        button.addEventListener('click', function () {
            const constellation = this.getAttribute('data-constellation');
            constellationFilters[constellation] = !constellationFilters[constellation];

            if (constellationFilters[constellation]) {
                this.classList.add('active');
            } else {
                this.classList.remove('active');
            }

            // Refresh satellite display
            if (playbackData.length > 0) {
                updateSatelliteTable(playbackData[currentPlaybackIndex].Satellites);
                updateSkyPlot(playbackData[currentPlaybackIndex].Satellites);
            }
        });
    });
}

// Setup WebSocket controls
function setupWebSocketControls() {
    const connectBtn = document.getElementById('connect-btn');
    const urlInput = document.getElementById('websocket-url');
    const statusIndicator = document.getElementById('connection-status');

    function updateStatus(status) {
        statusIndicator.textContent = {
            connecting: 'Подключение...',
            connected: 'Подключено',
            disconnected: 'Отключено',
            error: 'Ошибка'
        }[status];

        statusIndicator.className = 'px-3 py-1 rounded text-white ' + {
            connecting: 'bg-yellow-500',
            connected: 'bg-green-500',
            disconnected: 'bg-red-500',
            error: 'bg-red-700'
        }[status];
    }

    function connect(url) {
        updateStatus('connecting');

        socket = new WebSocket(url);

        socket.onopen = function () {
            updateStatus('connected');
            connectBtn.textContent = 'Отключиться';
            playbackData = [];
            currentPlaybackIndex = 0;
        };

        socket.onclose = function () {
            updateStatus('disconnected');
            connectBtn.textContent = 'Подключиться';
            stopPlayback();
            // Авто переподключение через 5 сек
            setTimeout(() => connect(url), 5000);
        };

        socket.onerror = function () {
            updateStatus('error');
        };

        socket.onmessage = function (event) {
            try {
                const data = JSON.parse(event.data);
                playbackData.push(data);
                document.getElementById('playback-progress').max = playbackData.length - 1;

                if (!isPlaying) {
                    currentPlaybackIndex = playbackData.length - 1;
                    document.getElementById('playback-progress').value = currentPlaybackIndex;
                    processData(data);
                }

                // Обновляем счетчик спутников
                if (data.Satellites) {
                    const count = data.Satellites.length;
                    document.getElementById('satellite-count').textContent =
                        `${count} ${getRussianPlural(count, ['спутник', 'спутника', 'спутников'])}`;
                }
            } catch (e) {
                console.error('Ошибка разбора данных:', e);
            }
        };
    }

    connectBtn.addEventListener('click', function () {
        if (socket && socket.readyState === WebSocket.OPEN) {
            socket.close();
            return;
        }

        const url = urlInput.value || `ws://${window.location.host}/ws`;
        connect(url);
    });
}

// Start playback
function startPlayback() {
    if (playbackData.length === 0) return;

    isPlaying = true;
    clearInterval(playbackInterval);

    playbackInterval = setInterval(() => {
        if (currentPlaybackIndex >= playbackData.length - 1) {
            currentPlaybackIndex = 0;
        } else {
            currentPlaybackIndex++;
        }

        document.getElementById('playback-progress').value = currentPlaybackIndex;
        processData(playbackData[currentPlaybackIndex]);
    }, 1000 / playbackSpeed);
}

// Pause playback
function pausePlayback() {
    isPlaying = false;
    clearInterval(playbackInterval);
}

// Stop playback
function stopPlayback() {
    isPlaying = false;
    clearInterval(playbackInterval);
    currentPlaybackIndex = 0;
    document.getElementById('playback-progress').value = 0;

    if (playbackData.length > 0) {
        processData(playbackData[0]);
    }
}

// Clear raw data button
document.getElementById('clear-raw').addEventListener('click', function () {
    document.getElementById('raw-data').textContent = '';
});

// Process incoming data
function processData(data) {
    // Update raw data display
    const rawDataElement = document.getElementById('raw-data');
    rawDataElement.textContent += JSON.stringify(data, null, 2) + '\n';
    rawDataElement.scrollTop = rawDataElement.scrollHeight;

    // Update map if we have coordinates
    if (data.Latitude && data.Longitude) {
        const lat = parseFloat(data.Latitude);
        const lng = parseFloat(data.Longitude);

        // Update coordinates display
        document.getElementById('coordinates').textContent =
            `${Math.abs(lat).toFixed(4)}° ${lat >= 0 ? 'N' : 'S'}, ${Math.abs(lng).toFixed(4)}° ${lng >= 0 ? 'E' : 'W'}`;

        // Update marker position
        placemark.geometry.setCoordinates([lat, lng]);

        // Add to path and update
        pathCoordinates.geometry.setCoordinates([...pathCoordinates.geometry.getCoordinates(), [lat, lng]]);

        // Adjust map view
        if (pathCoordinates.geometry.getCoordinates().length === 1) {
            map.setCenter([lat, lng], 15);
        } else {
            map.setBounds(pathCoordinates.geometry.getBounds());
        }
    }

    // Update altitude
    if (data.Altitude) {
        document.getElementById('altitude').textContent = `Высота: ${parseFloat(data.Altitude).toFixed(1)} м`;
    }

    // Update satellite information
    if (data.Satellites) {
        updateSatelliteTable(data.Satellites);
        updateSkyPlot(data.Satellites);
        updateSatelliteChart(data.Satellites);
    }

    // Update movement information
    if (data.Speed !== undefined) {
        const speedKmh = parseFloat(data.Speed) * 1.852; // Convert knots to km/h
        document.getElementById('speed-value').textContent = speedKmh.toFixed(1);
    }

    if (data.Direction !== undefined) {
        const direction = parseFloat(data.Direction);
        document.getElementById('compass-arrow').style.transform = `translateX(-50%) rotate(${direction}deg)`;

        // Convert direction to cardinal
        const directions = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'];
        const index = Math.round(direction / 45) % 8;
        document.getElementById('direction-text').textContent = `${direction.toFixed(0)}° ${directions[index]}`;
    }

    // Update time
    if (data.Timestamp) {
        const date = new Date(data.Timestamp);
        const timeElement = document.getElementById('gps-time');
        const dateElement = document.getElementById('gps-date');

        // Форматируем время с секундами
        timeElement.textContent = date.toLocaleTimeString('ru-RU', {
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });

        // Форматируем дату
        dateElement.textContent = date.toLocaleDateString('ru-RU');

        // Добавляем блок с координатами и высотой
        const locationElement = document.createElement('div');
        locationElement.className = 'text-gray-600 text-sm mt-2';

        if (data.Latitude && data.Longitude) {
            const lat = parseFloat(data.Latitude);
            const lng = parseFloat(data.Longitude);
            locationElement.innerHTML = `
            <div>Широта: ${Math.abs(lat).toFixed(4)}° ${lat >= 0 ? 'N' : 'S'}</div>
            <div>Долгота: ${Math.abs(lng).toFixed(4)}° ${lng >= 0 ? 'E' : 'W'}</div>
        `;
        }

        if (data.Altitude) {
            locationElement.innerHTML += `<div>Высота: ${parseFloat(data.Altitude).toFixed(1)} м</div>`;
        }

        // Удаляем старые данные, если есть
        const oldLocation = dateElement.nextElementSibling;
        if (oldLocation && oldLocation.classList.contains('location-data')) {
            oldLocation.remove();
        }

        locationElement.classList.add('location-data');
        dateElement.after(locationElement);
    }
}

// Update satellite table
function updateSatelliteTable(satellites) {
    const tableBody = document.getElementById('satellite-table');
    tableBody.innerHTML = '';

    satellites.forEach(sat => {
        // Skip if constellation is filtered out
        if (!constellationFilters[sat.System]) return;

        const row = document.createElement('tr');
        row.title = createTooltip(sat);

        const idCell = document.createElement('td');
        idCell.className = 'px-4 py-2 whitespace-nowrap text-sm font-medium text-gray-900';
        idCell.textContent = sat.Id;

        const systemCell = document.createElement('td');
        systemCell.className = 'px-4 py-2 whitespace-nowrap text-sm text-gray-500';
        systemCell.textContent = sat.System ? sat.System.toUpperCase() : '--';

        const azimuthCell = document.createElement('td');
        azimuthCell.className = 'px-4 py-2 whitespace-nowrap text-sm text-gray-500';
        azimuthCell.textContent = sat.Azimuth ? `${sat.Azimuth}°` : '--';

        const elevationCell = document.createElement('td');
        elevationCell.className = 'px-4 py-2 whitespace-nowrap text-sm text-gray-500';
        elevationCell.textContent = sat.Elevation ? `${sat.Elevation}°` : '--';

        const snrCell = document.createElement('td');
        snrCell.className = 'px-4 py-2 whitespace-nowrap text-sm text-gray-500';
        snrCell.textContent = sat.SNR ? `${sat.SNR} dB` : '--';

        const usedCell = document.createElement('td');
        usedCell.className = 'px-4 py-2 whitespace-nowrap text-sm text-gray-500';
        usedCell.textContent = sat.Used ? '✓' : '✗';

        row.appendChild(idCell);
        row.appendChild(systemCell);
        row.appendChild(azimuthCell);
        row.appendChild(elevationCell);
        row.appendChild(snrCell);
        row.appendChild(usedCell);

        tableBody.appendChild(row);
    });
}

function createTooltip(data) {
    return `<b>${data.System || 'UNKNOWN'} #${data.Id}</b><br>
        Азимут: ${data.Azimuth || 'N/A'}°<br>
        Высота: ${data.Elevation || 'N/A'}°<br>
        SNR: ${data.SNR || 'N/A'} dB<br>
        ${data.Used ? 'Используется' : 'Не используется'}`;
}

function getRussianPlural(number, titles) {
    const cases = [2, 0, 1, 1, 1, 2];
    return titles[
        number % 100 > 4 && number % 100 < 20
            ? 2
            : cases[Math.min(number % 10, 5)]
    ];
}

// Update sky plot
function updateSkyPlot(satellites) {
    const skyplot = document.getElementById('skyplot');
    skyplot.innerHTML = '';

    // Draw concentric circles for elevation (90° in center, 0° at edge)
    for (let elevation = 0; elevation <= 90; elevation += 30) {
        if (elevation === 0) continue;

        const radius = (90 - elevation) / 90 * 150;
        const circle = document.createElement('div');
        circle.style.position = 'absolute';
        circle.style.width = `${radius * 2}px`;
        circle.style.height = `${radius * 2}px`;
        circle.style.borderRadius = '50%';
        circle.style.border = '1px solid rgba(255, 255, 255, 0.3)';
        circle.style.top = '50%';
        circle.style.left = '50%';
        circle.style.transform = 'translate(-50%, -50%)';
        skyplot.appendChild(circle);

        // Add elevation label
        if (elevation > 0) {
            const label = document.createElement('div');
            label.textContent = `${elevation}°`;
            label.style.position = 'absolute';
            label.style.color = 'white';
            label.style.fontSize = '10px';
            label.style.top = '50%';
            label.style.left = '50%';
            label.style.transform = `translate(calc(-50% + ${radius}px), -50%)`;
            skyplot.appendChild(label);
        }
    }

    // Add cardinal directions
    const directions = ['N', 'E', 'S', 'W'];
    directions.forEach((dir, i) => {
        const angle = i * 90;
        const label = document.createElement('div');
        label.textContent = dir;
        label.style.position = 'absolute';
        label.style.color = 'white';
        label.style.fontWeight = 'bold';
        label.style.top = '50%';
        label.style.left = '50%';
        label.style.transform = `translate(-50%, -50%) rotate(${angle}deg) translateY(-140px) rotate(${-angle}deg)`;
        skyplot.appendChild(label);
    });

    // Add satellite dots
    satellites.forEach(sat => {
        // Skip if constellation is filtered out
        if (!constellationFilters[sat.System]) return;

        if (sat.Azimuth && sat.Elevation) {
            const radius = (90 - sat.Elevation) / 90 * 150;
            const angle = (sat.Azimuth - 90) * Math.PI / 180; // Convert to radians and adjust for 0° at top

            const x = 150 + radius * Math.cos(angle);
            const y = 150 + radius * Math.sin(angle);

            const dot = document.createElement('div');
            dot.className = 'satellite-dot';
            dot.style.left = `${x}px`;
            dot.style.top = `${y}px`;

            // Color based on constellation
            if (sat.System === 'gps') dot.style.backgroundColor = '#3b82f6';
            else if (sat.System === 'glonass') dot.style.backgroundColor = '#ef4444';
            else if (sat.System === 'galileo') dot.style.backgroundColor = '#10b981';
            else if (sat.System === 'beidou') dot.style.backgroundColor = '#f59e0b';
            else dot.style.backgroundColor = '#8b5cf6'; // Default color for unknown systems

            // Highlight if used in fix
            if (sat.Used) {
                dot.style.border = '2px solid white';
            }

            dot.textContent = sat.Id;
            skyplot.appendChild(dot);
        }
    });
}

// Update satellite chart
function updateSatelliteChart(satellites) {
    const filteredSatellites = satellites.filter(sat => constellationFilters[sat.System]);

    const labels = filteredSatellites.map(sat => sat.Id);
    const data = filteredSatellites.map(sat => sat.SNR || 0);

    satelliteChart.data.labels = labels;
    satelliteChart.data.datasets[0].data = data;
    satelliteChart.update();
}