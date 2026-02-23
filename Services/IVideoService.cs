using MovieService.Models;

namespace MovieService.Services {
    public interface IVideoService {
        Task<List<Movie>> GetAllMoviesAsync();
        Task<Movie?> GetMovieAsync(int id);
        Task<StreamingInfo?> GetStreamingInfoAsync(int movieId);
        bool VideoExists(int movieId);
        string[] GetVideoQualities(int movieId);
    }
}
