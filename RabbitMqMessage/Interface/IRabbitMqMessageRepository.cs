using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqMessage.Interface
{
    public interface IRabbitMqMessageRepository
    {
        Task SendQueueMessageAsync<T>(T message);
        Task<T?> ReceiveQueueMessageAsync<T>();
        Task ListenToQueueAsync<T>(Func<T, Task> processMessage);
        Task ListenToRetryQueueAsync<T>(Func<T, Task> processMessage);
    }
}
