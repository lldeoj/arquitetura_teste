# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Esta fase é usada para compilar o projeto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Lancamentos.API/Lancamentos.API.csproj", "Lancamentos.API/"]
COPY ["RabbitMqMessage/RabbitMqMessage.csproj", "RabbitMqMessage/"]
COPY ["ServiceConsolidado/ServiceConsolidado.csproj", "ServiceConsolidado/"]
COPY ["Lancamentos.Library/Lancamentos.Library.csproj", "Lancamentos.Library/"]
RUN dotnet restore "./Lancamentos.API/Lancamentos.API.csproj"
COPY . .
# Remove unnecessary config files from referenced projects
RUN rm -f /src/ServiceConsolidado/appsettings.json /src/ServiceConsolidado/appsettings.Development.json
WORKDIR "/src/Lancamentos.API"
RUN dotnet build "./Lancamentos.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Lancamentos.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal (padrão quando não está usando a configuração de Depuração)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lancamentos.API.dll"]