dotnet pack -c Release /p:version=$(git tag | sort -hr | head -1) Enyim.Caching
