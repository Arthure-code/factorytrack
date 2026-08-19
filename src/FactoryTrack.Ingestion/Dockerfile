FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY src/FactoryTrack.Domain/*.csproj src/FactoryTrack.Domain/
COPY src/FactoryTrack.Contracts/*.csproj src/FactoryTrack.Contracts/
COPY src/FactoryTrack.Infrastructure/*.csproj src/FactoryTrack.Infrastructure/
COPY src/FactoryTrack.Positioning/*.csproj src/FactoryTrack.Positioning/
COPY src/FactoryTrack.Ingestion/*.csproj src/FactoryTrack.Ingestion/
RUN dotnet restore src/FactoryTrack.Ingestion/FactoryTrack.Ingestion.csproj

COPY src/ src/
RUN dotnet publish src/FactoryTrack.Ingestion/FactoryTrack.Ingestion.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

RUN adduser --disabled-password --gecos "" --uid 10001 factorytrack
USER factorytrack

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FactoryTrack.Ingestion.dll"]
