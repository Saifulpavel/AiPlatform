# Multi-stage — SDK দিয়ে build/publish, ছোট runtime image দিয়ে চালানো।
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# শুধু csproj আগে copy → restore layer cache (dependency না বদলালে re-restore হয় না)
COPY src/Api/AiPlatform.Api.csproj src/Api/
COPY src/Application/AiPlatform.Application.csproj src/Application/
COPY src/Domain/AiPlatform.Domain.csproj src/Domain/
COPY src/Infrastructure/AiPlatform.Infrastructure.csproj src/Infrastructure/
RUN dotnet restore src/Api/AiPlatform.Api.csproj

# বাকি source copy → publish
COPY src/ src/
RUN dotnet publish src/Api/AiPlatform.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AiPlatform.Api.dll"]
