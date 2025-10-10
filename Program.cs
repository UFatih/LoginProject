using Business.Interface;
using Business;
using Entities;
using LoginProject;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;
using Serilog;
using DocumentFormat.OpenXml.Bibliography;

var builder = WebApplication.CreateBuilder(args);

// Serilog 
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
     .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();



// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();

builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

// Hangfire
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");       
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization(); 

// Hangfire Dashboard (localhost:7053/hangfire)  
app.UseHangfireDashboard();

// Background service 
RecurringJob.AddOrUpdate<IUserService>(
    "clear-old-logs",
    service => service.ClearOldLogs(),
    Cron.Hourly);

RecurringJob.AddOrUpdate<IUserService>(
    "reset-locked-users",
    service => service.ResetLockedUsers(),
    Cron.Minutely);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Loginn}/{id?}");


app.Run();
