const API_URL = ''; 
let hls = null;
let currentMovie = null;

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', async () => {
    // Получаем ID фильма из sessionStorage
    const movieId = sessionStorage.getItem('selectedMovieId');

    if (!movieId) {
        showNoMovie();
        return;
    }

    await loadMovie(movieId);
});

// Загрузка информации о фильме
async function loadMovie(movieId) {
    try {
        showLoading(true);

        // Получаем информацию о фильме
        const movieResponse = await fetch(`${API_URL}/api/movies/${movieId}`);
        if (!movieResponse.ok) throw new Error('Фильм не найден');

        const movie = await movieResponse.json();

        // Получаем информацию для стриминга
        const streamResponse = await fetch(`${API_URL}/api/movies/${movieId}/stream`);
        if (!streamResponse.ok) throw new Error('Видео не доступно');

        const streamInfo = await streamResponse.json();

        // Отображаем информацию
        displayMovieInfo(movie, streamInfo);

        // Инициализируем плеер
        initPlayer(streamInfo);

    } catch (error) {
        console.error('Error loading movie:', error);
        showError(error.message);
    } finally {
        showLoading(false);
    }
}

// Отображение информации о фильме
function displayMovieInfo(movie, streamInfo) {
    document.getElementById('movieTitle').textContent = movie.title;
    document.getElementById('playerContainer').style.display = 'block';
    document.getElementById('noPlayer').style.display = 'none';

    // Обновляем заголовок страницы
    document.title = `${movie.title} - Movie Player`;
}

// Инициализация HLS плеера
function initPlayer(streamInfo) {
    const video = document.getElementById('videoPlayer');
    const playlistUrl = streamInfo.masterPlaylistUrl.startsWith('http')
        ? streamInfo.masterPlaylistUrl
        : `${API_URL}${streamInfo.masterPlaylistUrl}`;

    // Уничтожаем предыдущий экземпляр HLS
    if (hls) {
        hls.destroy();
    }

    // Заполняем селектор качества
    updateQualitySelector(streamInfo.qualities);

    if (Hls.isSupported()) {
        // Создаем новый экземпляр HLS
        hls = new Hls({
            maxLoadingDelay: 4,
            minAutoBitrate: 0,
            lowLatencyMode: true,
            debug: false // Включите true для отладки
        });

        // Загружаем источник
        hls.loadSource(playlistUrl);
        hls.attachMedia(video);

        // События HLS
        hls.on(Hls.Events.MANIFEST_PARSED, () => {
            console.log('HLS manifest parsed');
            video.play().catch(e => console.log('Autoplay prevented:', e));
        });

        hls.on(Hls.Events.LEVEL_SWITCHED, (event, data) => {
            const level = hls.levels[data.level];
            updateStats(level);
        });

        hls.on(Hls.Events.ERROR, (event, data) => {
            if (data.fatal) {
                console.error('HLS fatal error:', data);
                switch (data.type) {
                    case Hls.ErrorTypes.NETWORK_ERROR:
                        hls.startLoad();
                        break;
                    case Hls.ErrorTypes.MEDIA_ERROR:
                        hls.recoverMediaError();
                        break;
                    default:
                        showError('Ошибка воспроизведения видео');
                        break;
                }
            }
        });

        // Мониторинг буфера
        video.addEventListener('waiting', () => {
            showLoading(true);
        });

        video.addEventListener('playing', () => {
            showLoading(false);
        });

        video.addEventListener('canplay', () => {
            showLoading(false);
        });

    } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
        // Для Safari
        video.src = playlistUrl;
        video.addEventListener('loadedmetadata', () => {
            video.play().catch(e => console.log('Autoplay prevented:', e));
        });
    } else {
        showError('HLS не поддерживается вашим браузером');
    }

    // Добавляем обработчики для видео
    setupVideoListeners(video);
}

// Настройка слушателей событий видео
function setupVideoListeners(video) {
    let statsInterval;

    video.addEventListener('play', () => {
        // Обновляем статистику каждую секунду
        statsInterval = setInterval(() => {
            if (hls) {
                updateBufferStats(video);
            }
        }, 1000);
    });

    video.addEventListener('pause', () => {
        clearInterval(statsInterval);
    });

    video.addEventListener('ended', () => {
        clearInterval(statsInterval);
    });
}

// Обновление селектора качества
function updateQualitySelector(qualities) {
    const select = document.getElementById('qualitySelect');
    select.innerHTML = '<option value="auto">Авто</option>';

    if (qualities && qualities.length > 0) {
        qualities.forEach((quality, index) => {
            const option = document.createElement('option');
            option.value = index;
            option.textContent = `${quality.name} (${quality.bitrate} kbps)`;
            select.appendChild(option);
        });
    }
}

// Смена качества
window.changeQuality = (levelIndex) => {
    if (!hls) return;

    if (levelIndex === 'auto') {
        hls.currentLevel = -1; // Авто
    } else {
        hls.currentLevel = parseInt(levelIndex);
    }
};

// Обновление статистики
function updateStats(level) {
    if (!level) return;

    document.getElementById('bitrate').textContent =
        `${Math.round(level.bitrate / 1000)} kbps`;
    document.getElementById('resolution').textContent =
        `${level.width}x${level.height}`;
}

// Обновление статистики буфера
function updateBufferStats(video) {
    if (video.buffered.length > 0) {
        const bufferedEnd = video.buffered.end(video.buffered.length - 1);
        const duration = video.duration;
        const bufferedPercent = Math.round((bufferedEnd / duration) * 100);

        document.getElementById('buffer').textContent =
            `${bufferedPercent}% (${Math.round(bufferedEnd)}/${Math.round(duration)} сек)`;
    }
}

// Показать сообщение об ошибке
function showError(message) {
    const errorDiv = document.getElementById('errorMessage');
    errorDiv.textContent = message;
    errorDiv.style.display = 'block';

    setTimeout(() => {
        errorDiv.style.display = 'none';
    }, 5000);
}

// Показать/скрыть загрузку
function showLoading(show) {
    const overlay = document.getElementById('loadingOverlay');
    overlay.style.display = show ? 'flex' : 'none';
}

// Показать сообщение "нет фильма"
function showNoMovie() {
    document.getElementById('playerContainer').style.display = 'none';
    document.getElementById('noPlayer').style.display = 'block';
}

// Обработка клавиш
document.addEventListener('keydown', (e) => {
    const video = document.getElementById('videoPlayer');
    if (!video) return;

    switch (e.key.toLowerCase()) {
        case ' ':
            e.preventDefault();
            if (video.paused) {
                video.play();
            } else {
                video.pause();
            }
            break;
        case 'arrowright':
            e.preventDefault();
            video.currentTime += 10;
            break;
        case 'arrowleft':
            e.preventDefault();
            video.currentTime -= 10;
            break;
        case 'arrowup':
            e.preventDefault();
            video.volume = Math.min(video.volume + 0.1, 1);
            break;
        case 'arrowdown':
            e.preventDefault();
            video.volume = Math.max(video.volume - 0.1, 0);
            break;
        case 'f':
            e.preventDefault();
            if (document.fullscreenElement) {
                document.exitFullscreen();
            } else {
                video.requestFullscreen();
            }
            break;
    }
});