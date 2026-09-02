FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY VocabBotWeb.Api/*.csproj VocabBotWeb.Api/
RUN dotnet restore VocabBotWeb.Api/VocabBotWeb.Api.csproj
COPY VocabBotWeb.Api/ VocabBotWeb.Api/
RUN dotnet publish VocabBotWeb.Api/VocabBotWeb.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# ВАЖНО: Docker ENV не умеет подставлять переменные в рантайме контейнера —
# Render передаёт свой порт через переменную окружения PORT только при
# старте контейнера, поэтому биндим адрес в самом ENTRYPOINT (shell-форма),
# а не через ENV ASPNETCORE_URLS.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet VocabBotWeb.Api.dll"]
