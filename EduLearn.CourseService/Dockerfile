FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EduLearn.CourseService/EduLearn.CourseService.csproj", "EduLearn.CourseService/"]
COPY ["EduLearn.SharedLib/EduLearn.SharedLib.csproj", "EduLearn.SharedLib/"]
RUN dotnet restore "EduLearn.CourseService/EduLearn.CourseService.csproj"
COPY . .
WORKDIR "/src/EduLearn.CourseService"
RUN dotnet build "EduLearn.CourseService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduLearn.CourseService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduLearn.CourseService.dll"]
