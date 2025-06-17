# 1. Встановлюємо середовище .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# 2. Копіюємо проєкт
COPY . ./
RUN dotnet publish -c Release -o out

# 3. Реліз-образ з Runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out ./

# 4. Команда запуску
ENTRYPOINT ["dotnet", "курсач.dll"]