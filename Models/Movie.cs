namespace MovieService.Models {
    public class Movie {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Genre { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public string HlsFolder { get; set; } = string.Empty; // путь к папке с HLS файлами
        public double Rating { get; set; }
        public int Duration { get; set; } // в минутах
    }
}
