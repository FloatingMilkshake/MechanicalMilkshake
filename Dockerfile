FROM --platform=${BUILDPLATFORM} \
	mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet build -c Release -o out
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine
LABEL com.centurylinklabs.watchtower.enable="true"
WORKDIR /app
COPY --from=build-env /app/out .
RUN apk add bash openssh redis icu-libs --no-cache
RUN mkdir ~/.ssh \
	&& echo StrictHostKeyChecking no > ~/.ssh/config \
	&& touch ~/.ssh/id_ed25519
ENTRYPOINT ["dotnet", "MechanicalMilkshake.dll"]
