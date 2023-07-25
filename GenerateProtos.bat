ECHO OFF
SET PROTO_BIN_PATH=C:\Users\tatas\Documents\dev\git_repos\bitbucket\csharp-projects\CodingExercises\Millennium\protoc-23.4-win64\bin

ECHO MarketDataService...
%PROTO_BIN_PATH%\protoc.exe -I=MarketDataService\ProtoFiles --csharp_out=MarketDataService\Messages             MarketDataService\ProtoFiles\MarketDataMessage.proto
%PROTO_BIN_PATH%\protoc.exe -I=MarketDataService\ProtoFiles --csharp_out=MarketDataDistributionService\Messages MarketDataService\ProtoFiles\MarketDataMessage.proto


ECHO MarketDataDistributionService...
%PROTO_BIN_PATH%\protoc.exe -I=MarketDataDistributionService\ProtoFiles --csharp_out=MarketDataDistributionService\Messages MarketDataDistributionService\ProtoFiles\ParameterSetUpdateMessage.proto
%PROTO_BIN_PATH%\protoc.exe -I=MarketDataDistributionService\ProtoFiles --csharp_out=CalcEngineService\Messages             MarketDataDistributionService\ProtoFiles\ParameterSetUpdateMessage.proto
%PROTO_BIN_PATH%\protoc.exe -I=MarketDataDistributionService\ProtoFiles --csharp_out=QuoteEngineService\Messages            MarketDataDistributionService\ProtoFiles\ParameterSetUpdateMessage.proto


ECHO CalcEngineService...
%PROTO_BIN_PATH%\protoc.exe -I=CalcEngineService\ProtoFiles --csharp_out=CalcEngineService\Messages  CalcEngineService\ProtoFiles\ParameterSetMeshUpdateMessage.proto
%PROTO_BIN_PATH%\protoc.exe -I=CalcEngineService\ProtoFiles --csharp_out=QuoteEngineService\Messages CalcEngineService\ProtoFiles\ParameterSetMeshUpdateMessage.proto

