using Microsoft.AspNetCore.Mvc;
using MovieService.Models;
using MovieService.Services;

namespace MovieService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase {
        private readonly IVideoService _videoService;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(IVideoService videoService, ILogger<MoviesController> logger) {
            _videoService = videoService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Movie>>> GetMovies() {
            try {
                var movies = await _videoService.GetAllMoviesAsync();
                return Ok(movies);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error getting movies");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Movie>> GetMovie(int id) {
            try {
                var movie = await _videoService.GetMovieAsync(id);
                if (movie == null)
                    return NotFound($"Movie with ID {id} not found");

                return Ok(movie);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error getting movie {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/stream")]
        public async Task<ActionResult<StreamingInfo>> GetStreamingInfo(int id) {
            try {
                var movie = await _videoService.GetMovieAsync(id);
                if (movie == null)
                    return NotFound($"Movie with ID {id} not found");

                // Проверяем наличие видео
                if (!_videoService.VideoExists(id)) {
                    return NotFound("Video files not found for this movie");
                }

                var streamingInfo = await _videoService.GetStreamingInfoAsync(id);
                if (streamingInfo == null)
                    return NotFound("Streaming information not available");

                return Ok(streamingInfo);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error getting streaming info for movie {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
