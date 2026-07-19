using PartnerIntegration.Api;
using PartnerIntegration.Api.Logging;
using PartnerIntegration.Application;
using PartnerIntegration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationLogging();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseApplicationRequestLogging();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program;
