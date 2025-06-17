# 1. Встановлюємо середовище .NET 8 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 2. Копіюємо проєкт
COPY . ./
RUN dotnet publish -c Release -o out

# 3. Реліз-образ з .NET 8 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

# 4. Команда запуску (заміни на назву свого .dll)
ENTRYPOINT ["dotnet", "курсач.dll"]
