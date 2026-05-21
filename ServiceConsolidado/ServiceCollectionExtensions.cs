using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMqMessage;
using RabbitMqMessage.Models;
using RabbitMqMessage.Interface;
using Lancamentos.Library.Interface;
using Lancamentos.Library.Service;
using Lancamentos.Library.Repository;
using ServiceConsolidado.Interface;
using ServiceConsolidado.Service;

namespace ServiceConsolidado
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind RabbitMQ settings
            var rabbitConf = new RabbitMqConfiguration();
            configuration.GetSection("RabbitMqConfiguration").Bind(rabbitConf);
            services.AddSingleton(rabbitConf);

            // RabbitMQ connection factory
            services.AddSingleton<IConnectionFactory>(sp =>
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

            // RabbitMQ context and repository
            services.AddSingleton<RabbitMqDataContext>();
            services.AddSingleton<IRabbitMqMessageRepository, RabbitMqMessageRepository>();

            // Application services
            services.AddScoped<ILancamentoRepository, LancamentoRepository>();
            services.AddScoped<ILancamentoService, Lancamentos.Library.Service.LancamentoService>();
            services.AddScoped<IConsolidadoService, ConsolidadoService>();
            services.AddScoped<IConsolidadoProcessService, ConsolidadoProcessService>();

            return services;
        }
    }
}
