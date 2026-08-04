# Container image for the API.
#
# Docker rather than Render's native .NET builder: the runtime here is .NET 10, and a
# managed builder that lags a release leaves you debugging someone else's base image.
# This pins the exact SDK and runtime.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, restored on their own. Docker caches this layer, so editing source
# does not re-download every NuGet package - which is most of the build time.
COPY UpcsgWeb.Api/UpcsgWeb.Api.csproj                 UpcsgWeb.Api/
COPY UpcsgWeb.Application/UpcsgWeb.Application.csproj UpcsgWeb.Application/
COPY UpcsgWeb.Domain/UpcsgWeb.Domain.csproj           UpcsgWeb.Domain/
COPY UpcsgWeb.Infrastructure/UpcsgWeb.Infrastructure.csproj UpcsgWeb.Infrastructure/
COPY UpcsgWeb.Shared/UpcsgWeb.Shared.csproj           UpcsgWeb.Shared/

RUN dotnet restore UpcsgWeb.Api/UpcsgWeb.Api.csproj

# The frontend and test projects are excluded by .dockerignore: the API does not reference
# them, and pulling in a WebAssembly build would add minutes for nothing.
COPY UpcsgWeb.Api/          UpcsgWeb.Api/
COPY UpcsgWeb.Application/  UpcsgWeb.Application/
COPY UpcsgWeb.Domain/       UpcsgWeb.Domain/
COPY UpcsgWeb.Infrastructure/ UpcsgWeb.Infrastructure/
COPY UpcsgWeb.Shared/       UpcsgWeb.Shared/

RUN dotnet publish UpcsgWeb.Api/UpcsgWeb.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Npgsql probes for GSSAPI when Postgres offers it during authentication. Without the
# Kerberos library it still connects - it just logs "Cannot load library
# libgssapi_krb5.so.2" on every new physical connection, which reads like a failure in
# production logs and is not one. Installing it costs a few hundred KB and removes a
# recurring false alarm.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

# LocalMediaStore creates wwwroot/media at construction, and /app belongs to root while
# the process does not. Without this the API dies at startup with "Access to the path
# '/app/wwwroot' is denied" whenever no bucket is configured.
#
# It only reproduces from a clean checkout: UpcsgWeb.Api/wwwroot/media is git-ignored, so
# a developer machine that has uploaded an image locally already has the directory and
# copies it into the image, hiding the problem.
RUN mkdir -p /app/wwwroot/media && chown -R $APP_UID:$APP_UID /app/wwwroot

# Render assigns a port per service and passes it as $PORT; a container listening on a
# fixed port is reported as unhealthy and the deploy fails with nothing obviously wrong in
# the logs. 8080 is only the fallback for running this image locally.
ENV ASPNETCORE_HTTP_PORTS=""
EXPOSE 8080

# Not root. A compromised process should not be able to write to its own image.
USER $APP_UID

# Shell form so $PORT is expanded at run time rather than baked in at build.
CMD ASPNETCORE_URLS="http://0.0.0.0:${PORT:-8080}" dotnet UpcsgWeb.Api.dll
