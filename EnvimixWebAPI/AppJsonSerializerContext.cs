using EnvimixWebAPI.Models;
using EnvimixWebAPI.Models.Envimania;
using EnvimixWebAPI.Models.ManiaPlanet;
using System.Text.Json.Serialization;

namespace EnvimixWebAPI;

[JsonSerializable(typeof(EnvimaniaSessionRequest))]
[JsonSerializable(typeof(EnvimaniaSessionResponse))]
[JsonSerializable(typeof(EnvimaniaSessionTokenResponse))]
[JsonSerializable(typeof(EnvimaniaRegistrationRequest))]
[JsonSerializable(typeof(EnvimaniaServerSummary[]))]
[JsonSerializable(typeof(EnvimaniaServerInfo))]
[JsonSerializable(typeof(EnvimaniaServerBanRequest))]
[JsonSerializable(typeof(EnvimaniaServerOperationResponse))]
[JsonSerializable(typeof(EnvimaniaBanRequest))]
[JsonSerializable(typeof(EnvimaniaUnbanRequest))]
[JsonSerializable(typeof(EnvimaniaSessionRecordRequest[]))]
[JsonSerializable(typeof(EnvimaniaRecordsResponse))]
[JsonSerializable(typeof(EnvimaniaSessionRecordBulkRequest))]
[JsonSerializable(typeof(EnvimaniaSessionRecordResponse))]
[JsonSerializable(typeof(MapInfo))]
[JsonSerializable(typeof(MapInfoResponse))]
[JsonSerializable(typeof(ManiaPlanetDedicatedServer))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, UseStringEnumConverter = true)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
