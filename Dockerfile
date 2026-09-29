# BlogEngine.NET on .NET 10 (ASP.NET Core)
# Build:  docker build -t blogengine .
# Run:    docker run -d -p 8080:8080 -v blogengine-data:/app/App_Data -v blogengine-custom:/app/Custom blogengine

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# BlogEngine.Core references lib/blogml/BlogML.dll by relative path
COPY lib/blogml/ lib/blogml/
COPY BlogEngine/ BlogEngine/
RUN dotnet publish BlogEngine/BlogEngine.NET/BlogEngine.NET.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build --chown=app:app /app .
# App_Data (blog data, uploads, Data Protection keys) and Custom (themes, widgets, extension settings)
# are written at runtime: mount them as volumes to keep data across container recreation
VOLUME ["/app/App_Data", "/app/Custom"]
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "BlogEngine.NET.dll"]
