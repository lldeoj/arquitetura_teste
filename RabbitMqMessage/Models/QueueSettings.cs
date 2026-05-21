using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqMessage.Models
{
    public class QueueSettings
    {
        public string Exchange { get; set; } = string.Empty;
        public string Queue { get; set; } = string.Empty;
        public string QueueRetry { get; set; } = string.Empty;
        public string QueueFail { get; set; } = string.Empty;
        public int MaxRetry { get; set; }
    }

}
