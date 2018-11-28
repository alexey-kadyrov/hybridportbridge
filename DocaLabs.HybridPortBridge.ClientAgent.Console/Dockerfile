FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY DocaLabs.HybridPortBridge.ClientAgent.Console/DocaLabs.HybridPortBridge.ClientAgent.Console.csproj DocaLabs.HybridPortBridge.ClientAgent.Console/
COPY DocaLabs.HybridPortBridge.ClientAgent/DocaLabs.HybridPortBridge.ClientAgent.csproj DocaLabs.HybridPortBridge.ClientAgent/
COPY DocaLabs.HybridPortBridge/DocaLabs.HybridPortBridge.csproj DocaLabs.HybridPortBridge/
RUN dotnet restore DocaLabs.HybridPortBridge.ClientAgent.Console/DocaLabs.HybridPortBridge.ClientAgent.Console.csproj
COPY . .
WORKDIR /src/DocaLabs.HybridPortBridge.ClientAgent.Console
RUN dotnet build DocaLabs.HybridPortBridge.ClientAgent.Console.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish DocaLabs.HybridPortBridge.ClientAgent.Console.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DocaLabs.HybridPortBridge.ClientAgent.Console.dll"]
