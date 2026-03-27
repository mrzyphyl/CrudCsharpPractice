FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CrudCsharpPractice.Api/CrudCsharpPractice.Api.csproj", "src/CrudCsharpPractice.Api/"]
RUN dotnet restore "src/CrudCsharpPractice.Api/CrudCsharpPractice.Api.csproj"
COPY . .
WORKDIR "/src/src/CrudCsharpPractice.Api"
RUN dotnet build "CrudCsharpPractice.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CrudCsharpPractice.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["dotnet", "CrudCsharpPractice.Api.dll"]
