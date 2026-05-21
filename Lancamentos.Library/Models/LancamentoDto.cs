namespace Lancamentos.Library.Models
{
    public class LancamentoDto
    {
        public Guid Id { get; set; }

        public decimal Valor { get; set; }

        public bool IsCredito { get; set; }

        public string AgenciaOrigem { get; set; } = string.Empty;

        public string ContaOrigem { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string Usuario { get; set; } = string.Empty;

        public DateTime DataHora { get; set; }
    }

    public class LancamentoFilterDto
    {
        public string? Usuario { get; set; }
        public string? ContaOrigem { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public bool? IsCredito { get; set; }
    }

    public class CreateLancamentoDto
    {
        public Guid Id { get; set; }
        public decimal Valor { get; set; }
        public bool IsCredito { get; set; }
        public string AgenciaOrigem { get; set; } = string.Empty;
        public string ContaOrigem { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public DateTime? DataHora { get; set; }
    }

    public class UpdateLancamentoDto
    {
        public Guid Id { get; set; }
        public decimal Valor { get; set; }
        public bool IsCredito { get; set; }
        public string AgenciaOrigem { get; set; } = string.Empty;
        public string ContaOrigem { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
    }
}
