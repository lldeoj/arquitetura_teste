using RabbitMqMessage.Interface;
using RabbitMqMessage.Models;
using System.Reflection.Metadata;

namespace RabbitMqMessage
{
    public class RabbitMqMessageRepository : IRabbitMqMessageRepository
    {
        private readonly RabbitMqDataContext _context;
        private readonly RabbitMqConfiguration _settings;

        public RabbitMqMessageRepository(RabbitMqDataContext context, RabbitMqConfiguration settings)
        {
            _context = context;
            _settings = settings;
        }

        public async Task SendQueueMessageAsync<T>(T message, string queueName = "")
        {
            int position = _settings.Queues.Length == 1 ? 0 : Array.FindIndex(_settings.Queues, q => q.Queue == queueName);
            await _context.SendMessageAsync(_settings.Queues[position], message);
        }

        public async Task<T?> ReceiveQueueMessageAsync<T>(string queueName = "")
        {
            int position = _settings.Queues.Length == 1 ? 0 : Array.FindIndex(_settings.Queues, q => q.Queue == queueName);
            return await _context.ReceiveMessageAsync<T>(_settings.Queues[position]);
        }

        public async Task ListenToQueueAsync<T>(Func<T, Task> processMessage)
            => await _context.ListenToQueueAsync(processMessage, _settings.Queues[0], _settings.Queues[0].Queue);

        public async Task ListenToRetryQueueAsync<T>(Func<T, Task> processMessage)
            => await _context.ListenToQueueAsync(processMessage, _settings.Queues[0], _settings.Queues[0].QueueRetry);
    }
}
