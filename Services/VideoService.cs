using MovieService.Models;

namespace MovieService.Services {
    public class VideoService : IVideoService {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<VideoService> _logger;
        private readonly string _videosPath;
        private List<Movie>? _moviesCache;

        public VideoService(IWebHostEnvironment env, ILogger<VideoService> logger) {
            _env = env;
            _logger = logger;
            _videosPath = Path.Combine(_env.ContentRootPath, "Videos");

            // Создаем папку для видео, если её нет
            if (!Directory.Exists(_videosPath)) {
                Directory.CreateDirectory(_videosPath);
            }
        }

        // Получить все фильмы (в реальном проекте тут была бы БД, но я не хочу настраивать)
        public async Task<List<Movie>> GetAllMoviesAsync() {
            if (_moviesCache != null)
                return _moviesCache;

            // Для демо создаем тестовые данные
            _moviesCache = new List<Movie>
            {
                new Movie
                {
                    Id = 1,
                    Title = "Inception",
                    Description = "A thief who steals corporate secrets through the use of dream-sharing technology is given the inverse task of planting an idea into the mind of a C.E.O.",
                    Year = 2010,
                    Genre = "Sci-Fi",
                    PosterUrl = "https://avatars.mds.yandex.net/get-mpic/11760083/2a0000018b434df3e14d215cc210268a4173/orig",
                    HlsFolder = "inception",
                    Rating = 8.8,
                    Duration = 148
                },
                new Movie
                {
                    Id = 2,
                    Title = "The Matrix",
                    Description = "A computer programmer discovers that reality as he knows it is a simulation created by machines, and joins a rebellion to break free.",
                    Year = 1999,
                    Genre = "Sci-Fi",
                    PosterUrl = "https://kassa.rambler.ru/s/StaticContent/P/Aimg/2206/10/220610170702508.jpg",
                    HlsFolder = "matrix",
                    Rating = 8.7,
                    Duration = 136
                },
                new Movie
                {
                    Id = 3,
                    Title = "Interstellar",
                    Description = "A team of explorers travel through a wormhole in space in an attempt to ensure humanity's survival.",
                    Year = 2014,
                    Genre = "Sci-Fi",
                    PosterUrl = "https://avatars.mds.yandex.net/get-mpic/12219175/2a0000018fd8832b16588278b17517a79ff8/orig",
                    HlsFolder = "interstellar",
                    Rating = 8.6,
                    Duration = 169
                }
            };

            return await Task.FromResult(_moviesCache);
        }

        // Получить фильм по ID
        public async Task<Movie?> GetMovieAsync(int id) {
            var movies = await GetAllMoviesAsync();
            return movies.FirstOrDefault(m => m.Id == id);
        }

        // Получить информацию для стриминга
        public async Task<StreamingInfo?> GetStreamingInfoAsync(int movieId) {
            var movie = await GetMovieAsync(movieId);
            if (movie == null)
                return null;

            var movieFolder = Path.Combine(_videosPath, movie.HlsFolder);
            var masterPath = Path.Combine(movieFolder, "master.m3u8");

            if (!File.Exists(masterPath)) {
                _logger.LogWarning($"Master playlist not found for movie {movieId} at {masterPath}");
                return null;
            }

            // Создаем информацию для плеера
            var info = new StreamingInfo {
                MovieId = movie.Id,
                Title = movie.Title,
                MasterPlaylistUrl = $"/api/stream/{movieId}/master.m3u8",
                Qualities = new List<VideoQuality>(),
                ThumbnailUrl = movie.PosterUrl
            };

            // Определяем доступные качества (по папкам)
            var qualityFolders = Directory.GetDirectories(movieFolder)
                .Select(Path.GetFileName)
                .Where(d => d.StartsWith("v"))
                .OrderBy(d => d)
                .ToList();

            foreach (var qualityFolder in qualityFolders) {
                var playlistPath = Path.Combine(movieFolder, qualityFolder, "playlist.m3u8");
                if (File.Exists(playlistPath)) {
                    var quality = qualityFolder switch {
                        "v0" => new VideoQuality { Name = "1080p", Width = 1920, Height = 1080, Bitrate = 5000 },
                        "v1" => new VideoQuality { Name = "720p", Width = 1280, Height = 720, Bitrate = 2500 },
                        "v2" => new VideoQuality { Name = "480p", Width = 854, Height = 480, Bitrate = 1000 },
                        _ => new VideoQuality { Name = qualityFolder, Width = 0, Height = 0, Bitrate = 0 }
                    };

                    quality.PlaylistUrl = $"/api/stream/{movieId}/{qualityFolder}/playlist.m3u8";
                    info.Qualities.Add(quality);
                }
            }

            return info;
        }

        // Проверить существование видео
        public bool VideoExists(int movieId) {
            var movie = GetMovieAsync(movieId).Result;
            if (movie == null) return false;

            var movieFolder = Path.Combine(_videosPath, movie.HlsFolder);
            return Directory.Exists(movieFolder) &&
                   File.Exists(Path.Combine(movieFolder, "master.m3u8"));
        }

        // Получить доступные качества
        public string[] GetVideoQualities(int movieId) {
            var movie = GetMovieAsync(movieId).Result;
            if (movie == null) return Array.Empty<string>();

            var movieFolder = Path.Combine(_videosPath, movie.HlsFolder);
            if (!Directory.Exists(movieFolder)) return Array.Empty<string>();

            return Directory.GetDirectories(movieFolder)
                .Select(Path.GetFileName)
                .Where(d => d.StartsWith("v"))
                .ToArray()!;
        }
    }
}
