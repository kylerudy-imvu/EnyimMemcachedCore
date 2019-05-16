dotnet build -c Release Enyim.Caching
dotnet pack -c Release /p:version=$(git tag --sort=committerdate | tail -1 ) Enyim.Caching

