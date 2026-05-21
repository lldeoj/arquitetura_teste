using Lancamentos.Library.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceLancamentos.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Lancamento> Lancamentos { get; set; }
    }
}
