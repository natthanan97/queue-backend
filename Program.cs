using Microsoft.EntityFrameworkCore;
using Npgsql;
using queue_backend.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
    
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    logger.LogInformation("Startup environment: {EnvironmentName}", app.Environment.EnvironmentName);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        logger.LogError("Connection string 'DefaultConnection' was not found or is empty.");
    }
    else
    {
        try
        {
            var csb = new NpgsqlConnectionStringBuilder(connectionString);

            logger.LogInformation(
                "Resolved database connection settings at startup. Host: {Host}; Port: {Port}; Database: {Database}; Username: {Username}; SSL Mode: {SslMode}",
                csb.Host,
                csb.Port,
                csb.Database,
                csb.Username,
                csb.SslMode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DefaultConnection exists but could not be parsed as an Npgsql connection string.");
        }
    }

    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation("Database connection check succeeded at startup.");
        }
        else
        {
            logger.LogError("Database connection check failed at startup. The connection string was loaded, but the provider could not establish a database connection.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database connection check threw an exception at startup.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("LocalFrontend");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
