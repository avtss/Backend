using System;
using Oms.Config;
using Oms.Consumer.Clients;
using Oms.Consumer.Consumers;
using Oms.Jobs;
using Oms.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection(nameof(RabbitMqSettings)));

builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddHostedService<OmsOrderCreatedConsumer>();
builder.Services.AddHostedService<OrderGenerator>();
builder.Services.AddHttpClient<OmsClient>(c => c.BaseAddress = new Uri(builder.Configuration["HttpClient:Oms:BaseAddress"]));

var app = builder.Build();

await app.RunAsync();

