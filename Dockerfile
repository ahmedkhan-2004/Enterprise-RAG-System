# build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
# make Kestrel listen on 5009
ENV ASPNETCORE_URLS=http://+:5009
EXPOSE 5009
ENTRYPOINT ["dotnet", "SemanticKernelDocumentQA.dll"]
