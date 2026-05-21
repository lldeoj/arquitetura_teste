using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lancamentos.Library.Models
{
    public class RabbitMQRequest
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("valor")]
        public decimal Valor { get; set; }

        [JsonPropertyName("isCredito")]
        public bool IsCredito { get; set; }

        [JsonPropertyName("agenciaOrigem")]
        public string AgenciaOrigem { get; set; } = string.Empty;

        [JsonPropertyName("contaOrigem")]
        public string ContaOrigem { get; set; } = string.Empty;

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("usuario")]
        public string Usuario { get; set; } = string.Empty;

        [JsonPropertyName("dataHora")]
        public DateTime DataHora { get; set; }
    }
}
