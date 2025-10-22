using EnvimixWebsite.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDomainServices();
builder.Services.AddWebServices(builder.Configuration);
builder.Services.AddCacheServices();
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    foreach (var knownProxy in builder.Configuration.GetSection("KnownProxies").Get<string[]>() ?? [])
    {
        if (IPAddress.TryParse(knownProxy, out var ipAddress))
        {
            options.KnownProxies.Add(ipAddress);
            continue;
        }

        foreach (var hostIpAddress in Dns.GetHostAddresses(knownProxy))
        {
            options.KnownProxies.Add(hostIpAddress);
        }
    }
});

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseMiddleware();

app.Run();
