const API_URL = ''; // Пустой, потому что запросы идут на тот же хост
// Если API на другом порту, укажите: const API_URL = 'https://localhost:7000';

document.addEventListener('DOMContentLoaded', () => {
    loadMovies();
});

// Загрузка списка фильмов
async function loadMovies() {
    try {
        const response = await fetch(`${API_URL}/api/movies`);
        if (!response.ok) {
            throw new Error('Ошибка загрузки фильмов');
        }

        const movies = await response.json();
        displayMovies(movies);
    } catch (error) {
        console.error('Error loading movies:', error);
        showError('Не удалось загрузить список фильмов');
    }
}

// Отображение фильмов
function displayMovies(movies) {
    const grid = document.getElementById('moviesGrid');

    if (!movies || movies.length === 0) {
        grid.innerHTML = '<div class="loading">Фильмы не найдены</div>';
        return;
    }

    grid.innerHTML = movies.map(movie => `
        <div class="movie-card" onclick="selectMovie(${movie.id})">
            <img 
                src="${movie.posterUrl || '/images/placeholder.jpg'}" 
                alt="${movie.title}"
                class="movie-poster"
                onerror="this.src='/images/placeholder.jpg'"
            >
            <div class="movie-info">
                <div class="movie-title">${movie.title}</div>
                <div class="movie-year">${movie.year || 'Неизвестно'}</div>
                <div class="movie-rating">⭐ ${movie.rating || '0'}</div>
                <span class="movie-genre">${movie.genre || 'Без жанра'}</span>
            </div>
        </div>
    `).join('');
}

// Выбор фильма для просмотра
function selectMovie(movieId) {
    // Сохраняем ID фильма в sessionStorage
    sessionStorage.setItem('selectedMovieId', movieId);

    // Переходим на страницу плеера
    window.location.href = '/player.html';
}

// Показать ошибку
function showError(message) {
    const grid = document.getElementById('moviesGrid');
    grid.innerHTML = `<div class="error-message" style="display:block;">${message}</div>`;
}

// Загрузка изображения-заглушки (если нужно)
function createPlaceholder() {
    // Этот код создаст простую заглушку, если её нет
    const canvas = document.createElement('canvas');
    canvas.width = 300;
    canvas.height = 450;
    const ctx = canvas.getContext('2d');

    // Рисуем градиент
    const gradient = ctx.createLinearGradient(0, 0, 300, 450);
    gradient.addColorStop(0, '#667eea');
    gradient.addColorStop(1, '#764ba2');

    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, 300, 450);

    ctx.fillStyle = 'white';
    ctx.font = 'bold 24px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('🎬', 150, 225);
    ctx.font = '16px Arial';
    ctx.fillText('No Poster', 150, 280);
}