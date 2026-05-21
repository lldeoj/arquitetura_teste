using System.Text.Json.Serialization;

namespace ServiceConsolidado.Models
{
    public class SaldoDiarioConsolidado
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("agencia")]
        public string Agencia { get; set; } = string.Empty;

        [JsonPropertyName("conta")]
        public string Conta { get; set; } = string.Empty;

        [JsonPropertyName("dia")]
        public DateTime Dia { get; set; }

        [JsonPropertyName("saldoInicial")]
        public decimal SaldoInicial { get; set; }

        [JsonPropertyName("totalCreditos")]
        public decimal TotalCreditos { get; set; }

        [JsonPropertyName("totalDebitos")]
        public decimal TotalDebitos { get; set; }

        [JsonPropertyName("saldoFinal")]
        public decimal SaldoFinal { get; set; }

        [JsonPropertyName("dataProcessamento")]
        public DateTime DataProcessamento { get; set; }
    }
}
