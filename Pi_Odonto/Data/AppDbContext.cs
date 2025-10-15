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
        public DbSet<EscalaTrabalho> EscalaTrabalho { get; set; }
        public DbSet<DisponibilidadeDentista> DisponibilidadesDentista { get; set; }
        public DbSet<Agendamento> Agendamentos { get; set; }
        public DbSet<Atendimento> Atendimentos { get; set; }

        // Configurações adicionais do relacionamento
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração do relacionamento Responsavel -> Crianca
            modelBuilder.Entity<Crianca>()
                .HasOne(c => c.Responsavel)
                .WithMany(r => r.Criancas)
                .HasForeignKey(c => c.IdResponsavel)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuração das tabelas
            modelBuilder.Entity<Responsavel>()
                .ToTable("responsavel");

            modelBuilder.Entity<Crianca>()
                .ToTable("crianca");

            modelBuilder.Entity<Dentista>()
                .ToTable("dentista");

            modelBuilder.Entity<EscalaTrabalho>()
                .ToTable("escala_trabalho");

            modelBuilder.Entity<DisponibilidadeDentista>()
                .ToTable("disponibilidade_dentista");

            modelBuilder.Entity<Agendamento>()
                .ToTable("agendamento");

            modelBuilder.Entity<Atendimento>()
                .ToTable("atendimento");

            modelBuilder.Entity<RecuperacaoSenhaToken>()
                .ToTable("RecuperacaoSenhaTokens");

            // Configuração do relacionamento Dentista -> EscalaTrabalho
            modelBuilder.Entity<Dentista>()
                .HasOne(d => d.EscalaTrabalho)
                .WithMany(e => e.Dentistas)
                .HasForeignKey(d => d.IdEscala)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuração do relacionamento Dentista -> DisponibilidadeDentista
            modelBuilder.Entity<DisponibilidadeDentista>()
                .HasOne(d => d.Dentista)
                .WithMany(d => d.Disponibilidades)
                .HasForeignKey(d => d.IdDentista)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
