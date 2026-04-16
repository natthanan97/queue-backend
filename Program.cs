using Microsoft.EntityFrameworkCore;
using queue_backend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation("Database connection check succeeded at startup.");
        }
        else
        {
            logger.LogError("Database connection check failed at startup.");
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
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
