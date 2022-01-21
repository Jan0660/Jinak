FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Jinak/Jinak.csproj", "Jinak/"]
COPY ["Ozse.Net/Ozse.Net.csproj", "Ozse.Net/"]
RUN dotnet restore "Jinak/Jinak.csproj"
COPY . .
WORKDIR "/src/Jinak"
RUN dotnet build "Jinak.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Jinak.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Jinak.dll"]
