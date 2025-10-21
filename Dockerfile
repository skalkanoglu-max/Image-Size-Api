# 1️⃣ Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Projeyi kopyala ve derle
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# 2️⃣ Runtime aşaması
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app ./
ENTRYPOINT ["dotnet", "ImageOptimizeApi.dll"]
