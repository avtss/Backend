FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Shop.sln ./
COPY Oms/Oms.csproj Oms/
COPY WebApi/WebApi.csproj WebApi/
COPY Models/Models.csproj Models/
COPY Common/Common.csproj Common/
COPY Messages/Messages.csproj Messages/

RUN dotnet restore "Oms/Oms.csproj"

COPY . .
RUN dotnet publish "Oms/Oms.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Oms.dll"]
