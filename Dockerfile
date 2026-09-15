FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build-env
WORKDIR /build
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet build MechanicalMilkshake.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine
LABEL com.centurylinklabs.watchtower.enable="true"
WORKDIR /app
RUN apk add redis icu-libs --no-cache
COPY --from=build-env /build/out .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENTRYPOINT ["dotnet", "MechanicalMilkshake.dll"]
