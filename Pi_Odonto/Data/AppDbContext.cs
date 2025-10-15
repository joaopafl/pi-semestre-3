using Microsoft.EntityFrameworkCore;
using Pi_Odonto.Models;
namespace Pi_Odonto.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        // DbSet para representar as tabelas no banco
        public DbSet<Responsavel> Responsaveis { get; set; }
        public DbSet<Crianca> Criancas { get; set; }
        public DbSet<RecuperacaoSenhaToken> RecuperacaoSenhaTokens { get; set; }
        public DbSet<Dentista> Dentistas { get; set; }
        public DbSet<Agendamento> Agendamentos { get; set; }
        public DbSet<Atendimento> Atendimentos { get; set; }
        
        // Configurações adicionais do relacionamento (opcional, mas recomendado)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configuração do relacionamento Responsavel -> Crianca
            modelBuilder.Entity<Crianca>()
                .HasOne(c => c.Responsavel)
                .WithMany(r => r.Criancas)
                .HasForeignKey(c => c.IdResponsavel)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Configuração da tabela responsavel
            modelBuilder.Entity<Responsavel>()
                .ToTable("responsavel");
            
            // Configuração da tabela crianca
            modelBuilder.Entity<Crianca>()
                .ToTable("crianca");
            
            // Configuração da tabela dentista
            modelBuilder.Entity<Dentista>()
                .ToTable("dentista");
            
            // Configuração da tabela agendamento
            modelBuilder.Entity<Agendamento>()
                .ToTable("agendamento");
            
            // Configuração da tabela atendimento
            modelBuilder.Entity<Atendimento>()
                .ToTable("atendimento");
            
            // Configuração da tabela de tokens de recuperação
            modelBuilder.Entity<RecuperacaoSenhaToken>()
                .ToTable("RecuperacaoSenhaTokens");
        }
    }
}
