FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY task2.csproj ./
RUN dotnet restore "task2.csproj"

COPY . .
RUN dotnet publish "task2.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "task2.dll"]