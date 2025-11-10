gen:
	openapi-generator-cli generate -i http://localhost:3211/swagger/api/v0/json -g csharp -o Temp/Api -c api-generate-config.json
	cd Temp/Api && dotnet build -c Release && cd ../../
	cp -rf Temp/Api/src/Matuyuhi.LudiscanApi.Client/bin/Release/netstandard2.1/Matuyuhi.LudiscanApi.Client.dll Assets/Matuyuhi/LudiscanApiClient/Runtime/Plugins/
	#rm -rf Temp/Api