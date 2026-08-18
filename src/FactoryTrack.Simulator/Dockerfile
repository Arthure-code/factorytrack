FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY src/FactoryTrack.Domain/*.csproj src/FactoryTrack.Domain/
COPY src/FactoryTrack.Contracts/*.csproj src/FactoryTrack.Contracts/
COPY src/FactoryTrack.Simulator/*.csproj src/FactoryTrack.Simulator/
RUN dotnet restore src/FactoryTrack.Simulator/FactoryTrack.Simulator.csproj

COPY src/ src/
RUN dotnet publish src/FactoryTrack.Simulator/FactoryTrack.Simulator.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app .

RUN adduser --disabled-password --gecos "" --uid 10001 factorytrack
USER factorytrack

ENTRYPOINT ["dotnet", "FactoryTrack.Simulator.dll"]
