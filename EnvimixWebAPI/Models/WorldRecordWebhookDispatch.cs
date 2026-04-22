using EnvimixWebAPI.Entities;

namespace EnvimixWebAPI.Models;

public sealed record WorldRecordWebhookDispatch(RecordEntity NewRecord, RecordEntity? PrevRecord);
