using System.Text.Json.Serialization;

namespace ServiceConsolidado.Models
{
    public class ConsolidadoRequest
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("agencia")] 
        public string Agencia { get; set; } = string.Empty;
        
        [JsonPropertyName("conta")]
        public string Conta { get; set; } = string.Empty;
        
        [JsonPropertyName("dia")]
        public DateTime Dia { get; set; }
    }
}
