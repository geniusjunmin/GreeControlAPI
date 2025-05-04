# 使用 ASP.NET 作为基础镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root

# 安装 curl 和 wget
RUN apt-get update && apt-get install -y curl wget

# 设置默认用户为 app
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
EXPOSE 7777/udp

# 使用 SDK 镜像来进行构建
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 复制项目文件
COPY . .

# 恢复项目依赖
RUN dotnet restore

# 构建项目
WORKDIR "/src"
RUN dotnet build "./GreeControlAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 发布项目
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GreeControlAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 最终镜像
FROM base AS final
WORKDIR /app

# 将发布的文件复制到最终镜像
COPY --from=publish /app/publish ./

# 设置容器启动时执行的命令
ENTRYPOINT ["dotnet", "GreeControlAPI.dll"]
