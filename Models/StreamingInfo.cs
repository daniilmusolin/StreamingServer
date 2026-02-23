namespace MovieService.Models {
    public class StreamingInfo {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MasterPlaylistUrl { get; set; } = string.Empty;
        public List<VideoQuality> Qualities { get; set; } = new();
        public string ThumbnailUrl { get; set; } = string.Empty;
    }

    public class VideoQuality {
        public string Name { get; set; } = string.Empty; // 1080p, 720p, 480p
        public int Width { get; set; }
        public int Height { get; set; }
        public int Bitrate { get; set; } // в kbps
        public string PlaylistUrl { get; set; } = string.Empty;
    }
}
