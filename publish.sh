cd ../Jurgen\'s-SmartPendantSDK/csharp/
msbuild SDK.csproj /t:build
cd ../../MotoMini_DemoSetup/
dotnet publish -r linux-arm --self-contained true
