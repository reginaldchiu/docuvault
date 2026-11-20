using DocuVault.Application.Abstractions.Persistence;
using DocuVault.Application.Abstractions.Storage;
using DocuVault.Application.Documents;
using DocuVault.Infrastructure.Persistence;
using DocuVault.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

DocumentUploadOptions uploadOptions = builder.Configuration
    .GetSection("DocumentUpload")
    .Get<DocumentUploadOptions>() ??
    throw new InvalidOperationException("DocumentUpload configuration is missing.");

builder.Services.AddSingleton(uploadOptions);

builder.Services.Configure<LocalDocumentStorageOptions>(
    builder.Configuration.GetSection("DocumentStorage")
);

builder.Services.AddSingleton<IDocumentStorage, LocalDocumentStorage>();

builder.Services.AddScoped<UploadDocumentService>();

// Add services to the container.
string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string is not configured.");

builder.Services.AddDbContext<DocuVaultDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IDocumentWriter, EfDocumentWriter>();
builder.Services.AddScoped<CreateDocumentService>();

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
