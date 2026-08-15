using System.Reflection;
using System.Text.Json.Serialization;
using CollisionFlow.Api.ErrorHandling;
using CollisionFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Statuses travel as names ("WaitingOnParts"), not ordinals. The wire format
        // stays readable and does not silently change meaning if the enum is renumbered.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// RFC 7807 responses for anything the pipeline rejects, so every error the client
// can receive has the same shape.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "CollisionFlow API",
        Version = "v1",
        Description = "Repair order status tracking for collision repair centers.",
    });

    // Surface the XML documentation from this file in the generated OpenAPI document,
    // so the published contract explains itself.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// The React build lands in wwwroot, so the SPA and the API are one deployable
// served from one origin. Client-side routes fall back to index.html.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

/// <summary>
/// Exposed so integration tests can boot the real application through
/// <c>WebApplicationFactory</c> instead of re-declaring the service graph.
/// </summary>
public partial class Program
{
}
