using NotificationService.Core.Interfaces;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Handlers;
using NotificationService.Workers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Notification Service API", Version = "v1" });
});

// Core services
builder.Services.AddSingleton<IMessageQueue, InMemoryMessageQueue>();
builder.Services.AddSingleton<INotificationRepository, InMemoryNotificationRepository>();

// Channel handlers
builder.Services.AddScoped<INotificationHandler, EmailNotificationHandler>();
builder.Services.AddScoped<INotificationHandler, SmsNotificationHandler>();
builder.Services.AddScoped<INotificationHandler, PushNotificationHandler>();

// Dispatcher
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

// Background worker
builder.Services.AddHostedService<NotificationWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();

Log.Information("Notification Service started on {Env}", app.Environment.EnvironmentName);
app.Run();
