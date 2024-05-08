#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# 将用户设置为 root
USER root

# 安装 curl 和 wget
RUN apt-get update && apt-get install -y curl wget

# 将用户设置回默认用户
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
EXPOSE 7777/udp

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GreeControlAPI/GreeControlAPI.csproj", "GreeControlAPI/"]
RUN dotnet restore "./GreeControlAPI/GreeControlAPI.csproj"
COPY . .
WORKDIR "/src/GreeControlAPI"
RUN dotnet build "./GreeControlAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GreeControlAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GreeControlAPI.dll"]