dotnet publish .\PluginInterface\PluginBase.csproj -c Release /p:RuntimeIdentifierOverride=win-x86 -o publish

dotnet publish .\A1111Img2ImgPlugin\A1111Img2ImgPlugin.csproj -c Release /p:RuntimeIdentifierOverride=win-x86 -o publish
dotnet publish .\A1111TxtToImgPlugin\A1111TxtToImgPlugin.csproj  -c Release /p:RuntimeIdentifierOverride=win-x86 -o publish
dotnet publish .\CroppedImagePlugin\CroppedImagePlugin.csproj -c Release /p:RuntimeIdentifierOverride=win-x86 -o publish
dotnet publish .\OpenAiTxtToImgPlugin\OpenAiTxtToImgPlugin.csproj -c Release /p:RuntimeIdentifierOverride=win-x86 -o publish
dotnet publish .\StabilityAiImgToVidPlugin\StabilityAiImgToVidPlugin.csproj -c Release /p:RuntimeIdentifierOverride=win-x86 -o publish
dotnet publish .\StabilityAiTxtToImgPlugin\StabilityAiTxtToImgPlugin.csproj -c Release /p:RuntimeIdentifierOverride=win-x86 -o publish