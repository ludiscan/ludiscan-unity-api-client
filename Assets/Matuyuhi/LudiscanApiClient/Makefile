gen:
	openapi-generator-cli generate -i http://localhost:3211/swagger/api/v0/json -g csharp -o Temp/Api -c api-generate-config.json
	cd Temp/Api && dotnet build -c Release && cd ../../
	mkdir -p Runtime/Plugins/Matuyuhi.LudiscanApi.Client/
	cp -rf Temp/Api/src/Matuyuhi.LudiscanApi.Client/bin/Release/netstandard2.1/Matuyuhi.LudiscanApi.Client.dll Runtime/Plugins/Matuyuhi.LudiscanApi.Client/
	#rm -rf Temp/Api