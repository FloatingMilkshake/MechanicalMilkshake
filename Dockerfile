FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY bin/Release/net5.0/publish/ app/
WORKDIR /app
ENTRYPOINT ["dotnet", "DiscordBot.dll"]