using EnvimixWebsite.Configuration;
using Microsoft.AspNetCore.HttpOverrides;

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
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseForwardedHeaders();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseForwardedHeaders();
    app.UseHsts();
}

app.UseMiddleware();

app.Run();
