# Entity Framework Core

- Create Entity Framework Core migrations with `dotnet ef migrations add`; do not manually author migration or model-snapshot files.

# Testing Cleanup

- After testing, stop any .NET application process that you started unless the user asks for it to keep running.
- If a .NET application process started by the agent locks build output, stop it, rebuild, and restart it only when continued testing requires it. Do not stop a debugging session started by the user; report the file lock instead.
