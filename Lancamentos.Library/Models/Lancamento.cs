using System;
using System.ComponentModel.DataAnnotations;

namespace Lancamentos.Library.Models
{
    public class Lancamento
    {
        [Key]
        public Guid Id { get; set; }

        public decimal Valor { get; set; }

        public bool IsCredito { get; set; }

        public string AgenciaOrigem { get; set; } = string.Empty;

        public string ContaOrigem { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public string Usuario { get; set; } = string.Empty;

        public DateTime DataHora { get; set; }
    }
}
