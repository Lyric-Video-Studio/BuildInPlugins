set op=%1

dotnet publish .\PluginInterface\PluginBase.csproj -c Release /p:RuntimeIdentifierOverride=win-x86 -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building CroppedImagePlugin"
dotnet publish .\CroppedImagePlugin\CroppedImagePlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building A1111Img2ImgPlugin"
if NOT "%store%" == "steam" dotnet publish .\A1111Img2ImgPlugin\A1111Img2ImgPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building A1111TxtToImgPlugin"
if NOT "%store%" == "steam" dotnet publish .\A1111TxtToImgPlugin\A1111TxtToImgPlugin.csproj  -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building OpenAiTxtToImgPlugin"
if NOT "%store%" == "steam" dotnet publish .\OpenAiTxtToImgPlugin\OpenAiTxtToImgPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building StabilityAiImgToVidPlugin"
if NOT "%store%" == "steam" dotnet publish .\StabilityAiImgToVidPlugin\StabilityAiImgToVidPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building StabilityAiTxtToImgPlugin"
if NOT "%store%" == "steam" dotnet publish .\StabilityAiTxtToImgPlugin\StabilityAiTxtToImgPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building LumaAiDreamMachinePlugin"
if NOT "%store%" == "steam" dotnet publish .\LumaAiDreamMachine\LumaAiDreamMachinePlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building RunwayMlPlugin"
if NOT "%store%" == "steam" dotnet publish .\RunwayMl\RunwayMlPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building BflAiTxtToImgPlugin"
if NOT "%store%" == "steam" dotnet publish .\Bfl\BflAiTxtToImgPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building MinimaxPlugin"
if NOT "%store%" == "steam" dotnet publish .\Minimax\MinimaxPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building MusicGptPlugin"
if NOT "%store%" == "steam" dotnet publish .\MusicGpt\MusicGptPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building WanPlugin"
if NOT "%store%" == "steam" dotnet publish .\Wan\WanPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building ElevenLabsPlugin"
if NOT "%store%" == "steam" dotnet publish .\ElevenLabs\ElevenLabsPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building MistralAiTxtToImgPlugin"
if NOT "%store%" == "steam" dotnet publish .\MistralAI\MistralAiTxtToImgPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building FalAi"
if NOT "%store%" == "steam" dotnet publish .\FalAi\FalAiPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building Google"
if NOT "%store%" == "steam" dotnet publish .\Google\GooglePlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building LTX"
if NOT "%store%" == "steam" dotnet publish .\LTX\LTXPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%

echo "Building MUAPI"
if NOT "%store%" == "steam" dotnet publish .\MuApi\MuApiPlugin.csproj -c Release -o %op%
if %errorlevel% neq 0 exit /b %errorlevel%