using System;
using Oms.Config;
using Oms.Consumer.Clients;
using Oms.Consumer.Consumers;
using Oms.Jobs;
using Oms.Services;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddHostedService<BatchOmsOrderCreatedConsumer>();
builder.Services.AddHostedService<BatchOmsOrderStatusChangedConsumer>();
builder.Services.AddHostedService<OrderGenerator>();
builder.Services.AddHttpClient<OmsClient>(c => c.BaseAddress = new Uri(builder.Configuration["HttpClient:Oms:BaseAddress"]));

builder.Services.Configure<HostOptions>(options =>
{
    options.ServicesStartConcurrently = true;
    options.ServicesStopConcurrently = true;
});

var app = builder.Build();

await app.RunAsync();
