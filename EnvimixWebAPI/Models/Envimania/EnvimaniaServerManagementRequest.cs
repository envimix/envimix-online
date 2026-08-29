namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaServerBanRequest(string Reason);

public sealed record EnvimaniaServerOperationResponse(int DeletedCount);

public sealed record EnvimaniaServerAccess(bool CanDelete, bool CanAdminister);
