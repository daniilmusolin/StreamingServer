#!/bin/bash

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Функция для паузы
pause() {
    echo ""
    read -p "Нажмите Enter для продолжения..."
}

# Функция для вывода с таймстампом
log() {
    echo -e "${BLUE}[$(date '+%Y-%m-%d %H:%M:%S')]${NC} $1"
}

# Очищаем экран
clear

echo -e "${GREEN}╔════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║   🚀 Деплой Streaming Server v1.0    ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════╝${NC}"
echo ""

# Проверяем наличие Docker
log "${YELLOW}🔍 Проверка установки Docker...${NC}"
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker не установлен!${NC}"
    echo "Установите Docker: https://docs.docker.com/get-docker/"
    exit 1
else
    DOCKER_VERSION=$(docker --version | cut -d ' ' -f3 | cut -d ',' -f1)
    echo -e "${GREEN}✅ Docker установлен (версия: $DOCKER_VERSION)${NC}"
fi

# Проверяем наличие Docker Compose
log "${YELLOW}🔍 Проверка установки Docker Compose...${NC}"
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}❌ Docker Compose не установлен!${NC}"
    echo "Установите Docker Compose: https://docs.docker.com/compose/install/"
    exit 1
else
    COMPOSE_VERSION=$(docker-compose --version | cut -d ' ' -f4 | cut -d ',' -f1)
    echo -e "${GREEN}✅ Docker Compose установлен (версия: $COMPOSE_VERSION)${NC}"
fi

# Проверяем наличие папки Videos
log "${YELLOW}🔍 Проверка папки с видео...${NC}"
if [ ! -d "Videos" ]; then
    echo -e "${YELLOW}⚠️ Папка Videos не найдена, создаем...${NC}"
    mkdir -p Videos
    echo -e "${GREEN}✅ Папка Videos создана${NC}"
    echo -e "${YELLOW}⚠️ Не забудьте добавить видео в папку Videos!${NC}"
else
    VIDEO_COUNT=$(find Videos -name "*.ts" 2>/dev/null | wc -l)
    if [ "$VIDEO_COUNT" -gt 0 ]; then
        echo -e "${GREEN}✅ Найдено видео: $VIDEO_COUNT сегментов${NC}"
    else
        echo -e "${YELLOW}⚠️ В папке Videos нет видеофайлов${NC}"
    fi
fi

# Проверяем наличие wwwroot
log "${YELLOW}🔍 Проверка папки wwwroot...${NC}"
if [ ! -d "wwwroot" ]; then
    echo -e "${RED}❌ Папка wwwroot не найдена!${NC}"
    echo "Создайте папку wwwroot с файлами сайта"
    exit 1
else
    echo -e "${GREEN}✅ Папка wwwroot найдена${NC}"
fi

echo ""
pause

# Останавливаем старые контейнеры
log "${YELLOW}📦 Останавливаем старые контейнеры...${NC}"
docker-compose down
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Контейнеры остановлены${NC}"
else
    echo -e "${RED}❌ Ошибка при остановке контейнеров${NC}"
    exit 1
fi

echo ""
pause

# Собираем новый образ
log "${YELLOW}🔨 Собираем Docker образ (это может занять несколько минут)...${NC}"
docker-compose build --no-cache
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Docker образ собран успешно${NC}"
else
    echo -e "${RED}❌ Ошибка при сборке образа${NC}"
    exit 1
fi

echo ""
pause

# Запускаем контейнеры
log "${YELLOW}▶️ Запускаем контейнеры...${NC}"
docker-compose up -d
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Контейнеры запущены${NC}"
else
    echo -e "${RED}❌ Ошибка при запуске контейнеров${NC}"
    docker-compose logs
    exit 1
fi

echo ""
log "${YELLOW}⏳ Ожидаем запуск сервера (5 секунд)...${NC}"
sleep 5

# Проверяем статус
log "${YELLOW}📊 Проверяем статус сервера...${NC}"
if curl -s -f http://localhost:8080/health > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Сервер успешно запущен!${NC}"
    echo -e "${GREEN}🌐 Доступен по адресу: http://localhost:8080${NC}"
    
    # Пробуем открыть в браузере
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        open http://localhost:8080
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Linux
        xdg-open http://localhost:8080 2>/dev/null || echo "Откройте браузер и перейдите по адресу http://localhost:8080"
    fi
else
    echo -e "${RED}❌ Ошибка запуска сервера!${NC}"
    echo -e "${YELLOW}📝 Логи сервера:${NC}"
    docker-compose logs --tail=50
    exit 1
fi

echo ""
log "${YELLOW}📝 Последние логи сервера:${NC}"
docker-compose logs --tail=20

echo ""
echo -e "${GREEN}╔════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║   ✅ Деплой успешно завершен!                 ║${NC}"
echo -e "${GREEN}║   🌐 Сайт: http://localhost:8080              ║${NC}"
echo -e "${GREEN}║   📊 Для просмотра логов: docker-compose logs -f║${NC}"
echo -e "${GREEN}║   🛑 Для остановки: docker-compose down       ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════╝${NC}"

echo ""
read -p "Нажмите Enter для выхода..."