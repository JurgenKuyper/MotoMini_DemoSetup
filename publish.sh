cd ../SmartPendantSDK/csharp/ || exit
msbuild SDK.csproj /t:build
cd ../../MotoMini_DemoSetup/ || exit
dotnet publish -r linux-arm --self-contained true
