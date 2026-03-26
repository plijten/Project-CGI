using Application.Interfaces;
using Application.Services;
using Infrastructure.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IWebhookSignatureValidator, WebhookSignatureValidator>();
builder.Services.AddScoped<IPushEventService, PushEventService>();
builder.Services.AddScoped<ICgiQuestionGenerator, CgiQuestionGenerator>();
builder.Services.AddScoped<INotificationService, ConsoleEmailNotificationService>();
builder.Services.AddScoped<RelevancyService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
