FROM --platform=${BUILDPLATFORM} \
	mcr.microsoft.com/dotnet/sdk:7.0.202-alpine3.17 AS build-env
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet build -c Release -o out
FROM mcr.microsoft.com/dotnet/runtime:7.0.4-alpine3.17
LABEL com.centurylinklabs.watchtower.enable true
WORKDIR /app
COPY --from=build-env /app/out .
RUN apk add bash openssh redis icu-libs --no-cache
RUN mkdir ~/.ssh \
	&& echo StrictHostKeyChecking no > ~/.ssh/config \
	&& touch ~/.ssh/id_rsa
ENTRYPOINT ["dotnet", "MechanicalMilkshake.dll"]
