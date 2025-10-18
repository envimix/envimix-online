using EnvimixWebAPI.Models;
using EnvimixWebAPI.Models.Envimania;
using System.Text.Json.Serialization;

namespace EnvimixWebAPI;

[JsonSerializable(typeof(EnvimaniaMapInfo))]
[JsonSerializable(typeof(EnvimaniaRecordFilter))]
[JsonSerializable(typeof(EnvimaniaRecordInfo))]
[JsonSerializable(typeof(EnvimaniaRecordsResponse))]
[JsonSerializable(typeof(EnvimaniaSessionRecordResponse))]
[JsonSerializable(typeof(EnvimaniaSessionResponse))]
[JsonSerializable(typeof(EnvimaniaSessionUser))]
[JsonSerializable(typeof(RatingServerResponse))]
[JsonSerializable(typeof(UserInfo))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
internal partial class IngameJsonSerializerContext : JsonSerializerContext;