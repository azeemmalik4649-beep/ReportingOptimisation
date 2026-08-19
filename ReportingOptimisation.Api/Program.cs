using Microsoft.EntityFrameworkCore;
using ReportingOptimisation.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));

    // This is the key learning tool for this whole project:
    // it prints every SQL query EF Core generates to the console.
    // We'll use this constantly to SEE the N+1 problem happening.
    options.LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging(); // shows parameter values too (dev only!)
});

var app = builder.Build();

// Auto-migrate + seed on startup (fine for a learning project; not for real prod)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    SeedData.EnsureSeeded(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
