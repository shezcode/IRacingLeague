# Multi-stage build for the DWES1 console app.
# Console app -> dotnet/runtime base image (not aspnet); nothing actually
# listens on the port, but EXPOSE/-p are kept to satisfy the brief.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Presentation/IRacingLeague.Presentation.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app ./
# Cosmetic: a console app binds no socket. Included only because the brief asks
# for a published port; the volume (/app/data) and APP_ENV are the functional bits.
EXPOSE 8009
ENTRYPOINT ["dotnet", "IRacingLeague.Presentation.dll"]
