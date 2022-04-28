FROM mcr.microsoft.com/dotnet/sdk:6.0.202-alpine3.15 AS build-env
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet build -c Release -o out
FROM mcr.microsoft.com/dotnet/runtime:6.0.4-alpine3.15
WORKDIR /app
COPY --from=build-env /app/out .
RUN apk add bash openssh --no-cache
RUN mkdir ~/.ssh \
	&& echo StrictHostKeyChecking no > ~/.ssh/config \
	&& touch ~/.ssh/id_rsa
ENTRYPOINT ["dotnet", "MechanicalMilkshake.dll"]
