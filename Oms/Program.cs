using Oms.Config;
using Oms.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(nameof(RabbitMqSettings)));

builder.Services.AddSingleton<RabbitMqService>();

builder.Services.AddScoped<OrderService>();

var app = builder.Build();

