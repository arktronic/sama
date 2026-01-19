# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG VERSION=0.0.0-dev
WORKDIR /source

# copy csproj and restore as distinct layers
COPY global.json .
COPY *.slnx .
COPY Directory.Build.props .
COPY SAMA.Web/*.csproj SAMA.Web/
COPY SAMA.Data/*.csproj SAMA.Data/
COPY SAMA.Shared/*.csproj SAMA.Shared/
COPY SAMA.Tests.Unit/*.csproj SAMA.Tests.Unit/
COPY SAMA.Tests.Integration/*.csproj SAMA.Tests.Integration/
RUN dotnet restore SAMA.slnx

# copy everything else and build app
COPY . .
WORKDIR /source/SAMA.Web
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt update && apt install -y iputils-ping
ARG VERSION=0.0.0-dev
WORKDIR /app
COPY --from=build /app ./

LABEL org.opencontainers.image.title="SAMA"
LABEL org.opencontainers.image.description="Service Availability Monitoring and Alerting"
LABEL org.opencontainers.image.version="${VERSION}"
LABEL org.opencontainers.image.source="https://github.com/sep/sama"

# Run as non-root user (built into the base image)
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SAMA.Web.dll"]
