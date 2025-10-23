using Microsoft.OpenApi.Models;  // <- Add this using statement
using SemanticKernelDocumentQA.Extensions;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() {
        Title = "Semantic Kernel Document Q&A API",
        Version = "v1",
        Description = "An API for document-based question answering using Semantic Kernel"
    });

    // Add support for file uploads in Swagger UI
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add HttpClient
builder.Services.AddHttpClient();

// Add Semantic Kernel and Elasticsearch services
builder.Services.AddSemanticKernelServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Semantic Kernel Document Q&A API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Add a simple root endpoint
app.MapGet("/", () => new
{
    Application = "Semantic Kernel Document Q&A API",
    Version = "1.0.0",
    Status = "Running",
    Endpoints = new
    {
        Documentation = "/swagger",
        Chat = "/api/chat",
        Documents = "/api/documents"
    }
});

app.Run();