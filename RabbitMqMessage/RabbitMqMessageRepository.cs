using RabbitMqMessage.Interface;
using RabbitMqMessage.Models;

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

        public async Task SendQueueMessageAsync<T>(T message)
        {
            await _context.SendMessageAsync(_settings.Queues, message);
        }

        public async Task<T?> ReceiveQueueMessageAsync<T>()
        {
            return await _context.ReceiveMessageAsync<T>(_settings.Queues);
        }

        public async Task ListenToQueueAsync<T>(Func<T, Task> processMessage)
            => await _context.ListenToQueueAsync(processMessage, _settings.Queues, _settings.Queues.Queue);

        public async Task ListenToRetryQueueAsync<T>(Func<T, Task> processMessage)
            => await _context.ListenToQueueAsync(processMessage, _settings.Queues, _settings.Queues.QueueRetry);
    }
}
