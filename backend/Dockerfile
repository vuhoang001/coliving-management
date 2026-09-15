# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj và restore trước để tận dụng layer cache
COPY Coliving.sln ./
COPY src/Coliving.Domain/Coliving.Domain.csproj src/Coliving.Domain/
COPY src/Coliving.Application/Coliving.Application.csproj src/Coliving.Application/
COPY src/Coliving.Infrastructure/Coliving.Infrastructure.csproj src/Coliving.Infrastructure/
COPY src/Coliving.API/Coliving.API.csproj src/Coliving.API/
RUN dotnet restore src/Coliving.API/Coliving.API.csproj

# Copy toàn bộ source và publish
COPY . .
RUN dotnet publish src/Coliving.API/Coliving.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Coliving.API.dll"]
