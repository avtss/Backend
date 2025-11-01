FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy solution and project files for restore layer caching
COPY Shop.sln ./
COPY WebApi/WebApi.csproj WebApi/
COPY Oms/Oms.csproj Oms/
COPY Models/Models.csproj Models/
COPY Common/Common.csproj Common/
COPY Messages/Messages.csproj Messages/
COPY Migrations/Migrations.csproj Migrations/

RUN dotnet restore "WebApi/WebApi.csproj"

# copy the remaining sources and publish
COPY . .
RUN dotnet publish "WebApi/WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApi.dll"]
