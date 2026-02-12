using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Register Preferences Service
builder.Services.AddScoped<PreferencesService>();

builder.Services.AddHostedService<WeatherSyncBackgroundService>();
// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register HTTP Client for Weather API
builder.Services.AddHttpClient<IWeatherService, OpenWeatherService>();

// Register SyncService
builder.Services.AddScoped<SyncService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();