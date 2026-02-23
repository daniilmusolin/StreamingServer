using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using MovieService.Services;

namespace MovieService.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class StreamController : ControllerBase {
        private readonly IVideoService _videoService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<StreamController> _logger;

        public StreamController(
            IVideoService videoService,
            IWebHostEnvironment env,
            ILogger<StreamController> logger) {
            _videoService = videoService;
            _env = env;
            _logger = logger;
        }

        // GET: api/stream/5/master.m3u8
        [HttpGet("{movieId}/master.m3u8")]
        public async Task<IActionResult> GetMasterPlaylist(int movieId) {
            try {
                var movie = await _videoService.GetMovieAsync(movieId);
                if (movie == null)
                    return NotFound();

                var videoPath = Path.Combine(_env.ContentRootPath, "Videos", movie.HlsFolder);
                var masterPath = Path.Combine(videoPath, "master.m3u8");

                if (!System.IO.File.Exists(masterPath)) {
                    _logger.LogWarning($"Master playlist not found: {masterPath}");
                    return NotFound();
                }

                var content = await System.IO.File.ReadAllTextAsync(masterPath);

                // Модифицируем пути в плейлисте для корректных ссылок
                var baseUrl = $"{Request.Scheme}://{Request.Host}/api/stream/{movieId}";
                content = content.Replace("v0/", $"{baseUrl}/v0/");
                content = content.Replace("v1/", $"{baseUrl}/v1/");
                content = content.Replace("v2/", $"{baseUrl}/v2/");

                return Content(content, "application/vnd.apple.mpegurl");
            } catch (Exception ex) {
                _logger.LogError(ex, "Error serving master playlist for movie {MovieId}", movieId);
                return StatusCode(500);
            }
        }

        // GET: api/stream/5/v0/playlist.m3u8
        [HttpGet("{movieId}/v{quality}/playlist.m3u8")]
        public async Task<IActionResult> GetQualityPlaylist(int movieId, int quality) {
            try {
                var movie = await _videoService.GetMovieAsync(movieId);
                if (movie == null)
                    return NotFound();

                var videoPath = Path.Combine(_env.ContentRootPath, "Videos", movie.HlsFolder);
                var playlistPath = Path.Combine(videoPath, $"v{quality}", "playlist.m3u8");

                if (!System.IO.File.Exists(playlistPath)) {
                    _logger.LogWarning($"Quality playlist not found: {playlistPath}");
                    return NotFound();
                }

                var content = await System.IO.File.ReadAllTextAsync(playlistPath);

                // Модифицируем пути к сегментам
                var baseUrl = $"{Request.Scheme}://{Request.Host}/api/stream/{movieId}/v{quality}";
                content = content.Replace("fileSequence", $"{baseUrl}/fileSequence");

                return Content(content, "application/vnd.apple.mpegurl");
            } catch (Exception ex) {
                _logger.LogError(ex, "Error serving quality playlist for movie {MovieId}", movieId);
                return StatusCode(500);
            }
        }

        // GET: api/stream/5/v0/fileSequence0.ts
        [HttpGet("{movieId}/v{quality}/{segment}")]
        public async Task<IActionResult> GetSegment(int movieId, int quality, string segment) {
            try {
                var movie = await _videoService.GetMovieAsync(movieId);
                if (movie == null)
                    return NotFound();

                // Безопасность: проверяем что запрашивают .ts файл
                if (!segment.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)) {
                    return BadRequest("Invalid segment format");
                }

                var segmentPath = Path.Combine(
                    _env.ContentRootPath,
                    "Videos",
                    movie.HlsFolder,
                    $"v{quality}",
                    segment);

                if (!System.IO.File.Exists(segmentPath)) {
                    _logger.LogWarning($"Segment not found: {segmentPath}");
                    return NotFound();
                }

                var fileStream = System.IO.File.OpenRead(segmentPath);

                // Важно: устанавливаем правильный Content-Type
                Response.Headers.Add(HeaderNames.CacheControl, "public, max-age=3600");

                return File(fileStream, "video/MP2T");
            } catch (Exception ex) {
                _logger.LogError(ex, "Error serving segment for movie {MovieId}", movieId);
                return StatusCode(500);
            }
        }

        // GET: api/stream/test/5 - тестовый эндпоинт для проверки наличия видео
        [HttpGet("test/{movieId}")]
        public async Task<IActionResult> TestVideoExists(int movieId) {
            var exists = _videoService.VideoExists(movieId);
            var qualities = _videoService.GetVideoQualities(movieId);

            return Ok(new {
                exists,
                qualities,
                videoPath = Path.Combine(_env.ContentRootPath, "Videos")
            });
        }
    }
}
