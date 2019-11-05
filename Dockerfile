FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["AzureManage.csproj", ""]
RUN dotnet restore "./AzureManage.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "AzureManage.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureManage.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzureManage.dll"]