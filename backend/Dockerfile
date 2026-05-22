# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + project files first (layer caching for NuGet restore)
COPY ETicketLedger.sln ./
COPY ETicketLedger.API/ETicketLedger.API.csproj         ETicketLedger.API/
COPY ETicketLedger.Tests/ETicketLedger.Tests.csproj     ETicketLedger.Tests/

RUN dotnet restore

# Copy source and build
COPY . .
RUN dotnet build  -c Release --no-restore
RUN dotnet test   -c Release --no-build --verbosity normal
RUN dotnet publish ETicketLedger.API/ETicketLedger.API.csproj \
    -c Release --no-build -o /app/publish

# ── Stage 2: Runtime ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000
ENTRYPOINT ["dotnet", "ETicketLedger.API.dll"]
