namespace EnvimixWebAPI.Models;

public enum SessionReplaySubmissionResult
{
    Submitted,
    Unauthorized,
    SessionNotFound,
    AlreadySubmitted,
    InvalidReplay
}
