dotnet msbuild connector\Connector.csproj /t:Restore /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\Connector /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
dotnet msbuild connector\Connector.csproj /t:Rebuild /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\Connector /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
dotnet msbuild connector\Connector.csproj /t:Publish /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\Connector /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
if not exist C:\RDPShare\C-API\Connector mkdir C:\RDPShare\C-API\Connector

del C:\RDPShare\C-API\Connector\appsettings.*
del C:\RDPShare\C-API\Connector\appsettings.*.json
del C:\RDPShare\C-API\Connector\web.config


dotnet msbuild Plug-ins\DD-APIV2\DD-APIV2.csproj /t:Restore /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\DD-APIV2 /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
dotnet msbuild Plug-ins\DD-APIV2\DD-APIV2.csproj /t:Rebuild /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\DD-APIV2 /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
dotnet msbuild Plug-ins\DD-APIV2\DD-APIV2.csproj /t:Publish /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\DD-APIV2 /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
if not exist C:\RDPShare\C-API\DD-APIV2 mkdir C:\RDPShare\C-API\DD-APIV2

del C:\RDPShare\C-API\DD-APIV2\appsettings.*
del C:\RDPShare\C-API\DD-APIV2\appsettings.*.json
del C:\RDPShare\C-API\DD-APIV2\web.config


dotnet msbuild Plug-ins\DD-ECO-APIV2\DD-ECO-APIV2.csproj /t:Restore /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\DD-ECO-APIV2 /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
dotnet msbuild Plug-ins\DD-ECO-APIV2\DD-ECO-APIV2.csproj /t:Rebuild /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\DD-ECO-APIV2 /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
dotnet msbuild Plug-ins\DD-ECO-APIV2\DD-ECO-APIV2.csproj /t:Publish /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\DD-ECO-APIV2 /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
if not exist C:\RDPShare\C-API\DD-ECO-APIV2 mkdir C:\RDPShare\C-API\DD-ECO-APIV2

del C:\RDPShare\C-API\DD-ECO-APIV2\appsettings.*
del C:\RDPShare\C-API\DD-ECO-APIV2\appsettings.*.json
del C:\RDPShare\C-API\DD-ECO-APIV2\web.config


dotnet msbuild Plug-ins\Z-Info\Z-Info.csproj /t:Restore /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\Z-Info /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
dotnet msbuild Plug-ins\Z-Info\Z-Info.csproj /t:Rebuild /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\Z-Info /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
dotnet msbuild Plug-ins\Z-Info\Z-Info.csproj /t:Publish /p:PublishSingleFile=False /p:SelfContained=False /p:PublishProtocol=FileSystem /p:Configuration=Release /p:Platform=x64 /p:TargetFrameworks=net6.0 /p:PublishDir=C:\RDPShare\C-API\Z-Info /p:RuntimeIdentifier=win-x64 /p:PublishReadyToRun=False /p:PublishTrimmed=False
if not exist C:\RDPShare\C-API\Z-Info mkdir C:\RDPShare\C-API\Z-Info

del C:\RDPShare\C-API\Z-Info\appsettings.*
del C:\RDPShare\C-API\Z-Info\appsettings.*.json
del C:\RDPShare\C-API\Z-Info\web.config
