#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["QueueTicker.Bot/QueueTicker.Bot.csproj", "QueueTicker.Bot/"]
COPY ["QueueTicker.Core/QueueTicker.Core.csproj", "QueueTicker.Core/"]
RUN dotnet restore "QueueTicker.Bot/QueueTicker.Bot.csproj"
COPY . .
WORKDIR "/src/QueueTicker.Bot"
RUN dotnet build "QueueTicker.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "QueueTicker.Bot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QueueTicker.Bot.dll", "-m", "-s"]
