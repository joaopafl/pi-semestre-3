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

            // Configuração da tabela de tokens de recuperação
            modelBuilder.Entity<RecuperacaoSenhaToken>()
                .ToTable("RecuperacaoSenhaTokens");

            // Configuração da tabela dentista
            modelBuilder.Entity<Dentista>()
                .ToTable("dentista");

            // Configuração da tabela escala_trabalho
            modelBuilder.Entity<EscalaTrabalho>()
                .ToTable("escala_trabalho");

            // Configuração da tabela disponibilidade_dentista
            modelBuilder.Entity<DisponibilidadeDentista>()
                .ToTable("disponibilidade_dentista");

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