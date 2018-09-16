FROM microsoft/dotnet:2.1.302-sdk AS build-env
WORKDIR /app
 
# Copy csproj and restore as distinct layers
COPY I18n.Example/*.csproj ./I18n.Example/
RUN dotnet restore I18n.Example/I18n.Example.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out I18n.Example/I18n.Example.csproj

# Build runtime image
FROM microsoft/dotnet:2.1.2-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/I18n.Example/out .
EXPOSE 80
ENTRYPOINT ["dotnet", "I18n.Example.dll"]
