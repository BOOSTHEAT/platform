FROM mcr.microsoft.com/dotnet/runtime:3.1.19-alpine3.14
COPY . /app
WORKDIR /app
ENTRYPOINT ["dotnet", "BOOSTHEAT.Boiler.App.dll"]
