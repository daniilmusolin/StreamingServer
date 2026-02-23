FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MovieService.csproj", "."]
RUN dotnet restore "MovieService.csproj"

COPY . .
RUN dotnet publish "MovieService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Устанавливаем необходимые пакеты для работы с видео (опционально)
RUN apt-get update && apt-get install -y \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p /app/Videos /app/wwwroot

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "MovieService.dll"]