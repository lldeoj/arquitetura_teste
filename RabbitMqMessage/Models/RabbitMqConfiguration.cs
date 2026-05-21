using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Xsl;

namespace RabbitMqMessage.Models
{
    public class RabbitMqConfiguration
    {
        public ConnectionSettings Connection { get; set; } = new();
        public QueueSettings Queues { get; set; } = new();
        public SslSettings Ssl { get; set; } = new();
        public RetrySettings Retry { get; set; } = new();
    }

    public class ConnectionSettings
    {
        public string Host { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    public class SslSettings
    {
        public bool Enabled { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public bool AcceptInvalidCertificates { get; set; }
    }

    public class RetrySettings
    {
        public bool AutomaticRecoveryEnabled { get; set; }
        public bool TopologyRecoveryEnabled { get; set; }
        public int NetworkRecoveryIntervalSeconds { get; set; } = 15;
    }
}
