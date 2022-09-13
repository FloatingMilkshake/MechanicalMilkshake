FROM --platform=${BUILDPLATFORM} \
	mcr.microsoft.com/dotnet/sdk:6.0.401-alpine3.16 AS build-env
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet build -c Release -o out
FROM mcr.microsoft.com/dotnet/runtime:6.0.9-alpine3.16
LABEL com.centurylinklabs.watchtower.enable true
WORKDIR /app
COPY --from=build-env /app/out .
RUN apk add bash openssh redis icu-libs --no-cache
RUN mkdir ~/.ssh \
	&& echo StrictHostKeyChecking no > ~/.ssh/config \
	&& touch ~/.ssh/id_rsa
ENTRYPOINT ["dotnet", "MechanicalMilkshake.dll"]
