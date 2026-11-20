# API APP Aluguel (.NET 9) — imagem de produção para Render / containers
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props Aluguel.sln ./
COPY src/Aluguel.Domain/Aluguel.Domain.csproj src/Aluguel.Domain/
COPY src/Aluguel.Application/Aluguel.Application.csproj src/Aluguel.Application/
COPY src/Aluguel.Infrastructure/Aluguel.Infrastructure.csproj src/Aluguel.Infrastructure/
COPY src/Aluguel.Api/Aluguel.Api.csproj src/Aluguel.Api/

RUN dotnet restore src/Aluguel.Api/Aluguel.Api.csproj

COPY src/ src/
RUN dotnet publish src/Aluguel.Api/Aluguel.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Render injeta PORT; default 8080 para outros hosts
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .
EXPOSE 8080

# Respeita PORT do Render quando presente
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} && exec dotnet Aluguel.Api.dll"]
