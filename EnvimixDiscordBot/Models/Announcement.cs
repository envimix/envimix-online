using Discord;

namespace EnvimixDiscordBot.Models;

public sealed record Announcement(IUserMessage NewsMessage, IUserMessage StatusMessage);
