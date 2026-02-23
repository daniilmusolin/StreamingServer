using StreamingServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрируем наши сервисы
builder.Services.AddSingleton<IVideoService, VideoService>();

// Добавляем CORS для доступа с любого клиента
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Настройка Kestrel для больших файлов
builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxRequestBodySize = null;
    options.Limits.MaxRequestBufferSize = null;
});

var app = builder.Build();

// Конфигурируем pipeline
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Важно: порядок middleware имеет значение
app.UseCors("AllowAll");

app.UseStaticFiles(); // для wwwroot папки

app.UseAuthorization();

app.MapControllers();

// Создаем папку для видео, если её нет
var videosPath = Path.Combine(app.Environment.ContentRootPath, "Videos");
if (!Directory.Exists(videosPath)) {
    Directory.CreateDirectory(videosPath);
}

// Логируем пути для отладки
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Content Root Path: {Path}", app.Environment.ContentRootPath);
logger.LogInformation("Videos Path: {Path}", videosPath);
logger.LogInformation("Web Root Path: {Path}", app.Environment.WebRootPath);

app.Run();
