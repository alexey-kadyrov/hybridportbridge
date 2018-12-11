FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY DocaLabs.HybridPortBridge.ServiceAgent.Console/DocaLabs.HybridPortBridge.ServiceAgent.Console.csproj DocaLabs.HybridPortBridge.ServiceAgent.Console/
COPY DocaLabs.HybridPortBridge.ServiceAgent/DocaLabs.HybridPortBridge.ServiceAgent.csproj DocaLabs.HybridPortBridge.ServiceAgent/
COPY DocaLabs.HybridPortBridge/DocaLabs.HybridPortBridge.csproj DocaLabs.HybridPortBridge/
RUN dotnet restore DocaLabs.HybridPortBridge.ServiceAgent.Console/DocaLabs.HybridPortBridge.ServiceAgent.Console.csproj
COPY . .
WORKDIR /src/DocaLabs.HybridPortBridge.ServiceAgent.Console
RUN dotnet build DocaLabs.HybridPortBridge.ServiceAgent.Console.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish DocaLabs.HybridPortBridge.ServiceAgent.Console.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DocaLabs.HybridPortBridge.ServiceAgent.Console.dll"]
