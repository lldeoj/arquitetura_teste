using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqMessage.Interface
{
    public interface IRabbitMqMessageRepository
    {
        Task SendQueueMessageAsync<T>(T messagem, string queueNme = "");
        Task<T?> ReceiveQueueMessageAsync<T>(string queueNme = "");
        Task ListenToQueueAsync<T>(Func<T, Task> processMessage);
        Task ListenToRetryQueueAsync<T>(Func<T, Task> processMessage);
    }
}
