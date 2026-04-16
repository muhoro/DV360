using System.Text.Json.Serialization;
using Suss.Dv360.Api.Middleware;
using Suss.Dv360.Client.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDv360Client(options =>
{
    var cfg = builder.Configuration.GetSection("Dv360");
    options.AuthMode              = Enum.Parse<AuthMode>(cfg["AuthMode"] ?? "ServiceAccount");
    options.ServiceAccountKeyPath = cfg["ServiceAccountKeyPath"];
    options.OAuthClientId         = cfg["OAuthClientId"];
    options.OAuthClientSecret     = cfg["OAuthClientSecret"];
    options.TokenStorePath        = cfg["TokenStorePath"] ?? "tokens";
    options.ApplicationName       = cfg["ApplicationName"] ?? "Suss.Dv360.Api";
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Suss DV360 API",
        Version     = "v1",
        Description = "REST wrapper for the Google Display & Video 360 API"
    });

    var clientXml = Path.Combine(AppContext.BaseDirectory, "Suss.Dv360.Client.xml");
    if (File.Exists(clientXml))
        c.IncludeXmlComments(clientXml);

    var apiXml = Path.Combine(AppContext.BaseDirectory, "Suss.Dv360.Api.xml");
    if (File.Exists(apiXml))
        c.IncludeXmlComments(apiXml);
});

var app = builder.Build();

app.UseMiddleware<Dv360ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Suss DV360 API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();
