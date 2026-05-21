using RabbitMqMessage;
using RabbitMqMessage.Interface;
using RabbitMqMessage.Models;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Register RabbitMQ services
var rabbitConf = new RabbitMqConfiguration();
builder.Configuration.GetSection("RabbitMqConfiguration").Bind(rabbitConf);
builder.Services.AddSingleton(rabbitConf);

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var conf = sp.GetRequiredService<RabbitMqConfiguration>().Connection;
    return new ConnectionFactory
    {
        HostName = conf.Host,
        UserName = conf.User,
        Password = conf.Password,
        Port = conf.Port
    };
});

builder.Services.AddSingleton<RabbitMqDataContext>();
builder.Services.AddSingleton<IRabbitMqMessageRepository, RabbitMqMessageRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "API Gateway",
        Version = "v1",
        Description = "API Gateway para Lançamentos e Consolidado",
        Contact = new()
        {
            Name = "Arquitetura Teste",
            Url = new Uri("https://github.com/lldeoj/arquitetura_teste")
        }
    });
});

var app = builder.Build();

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
    options.RoutePrefix = string.Empty; // Make Swagger the default page
});

app.MapControllers();

app.Run();