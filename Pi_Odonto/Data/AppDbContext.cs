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
        // REMOVIDO: public DbSet<DisponibilidadeDentista> DisponibilidadesDentista { get; set; }
        public DbSet<EscalaMensalDentista> EscalasMensaisDentista { get; set; }
        public DbSet<Agendamento> Agendamentos { get; set; }
        public DbSet<Atendimento> Atendimentos { get; set; }

        // NOVOS DbSets
        public DbSet<SolicitacaoVoluntario> SolicitacoesVoluntario { get; set; }
        public DbSet<Odontograma> Odontogramas { get; set; }
        public DbSet<TratamentoDente> TratamentosDente { get; set; }

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

            // Configuração das tabelas existentes
            modelBuilder.Entity<Responsavel>()
                .ToTable("responsavel");

            modelBuilder.Entity<Crianca>()
                .ToTable("crianca");

            modelBuilder.Entity<Dentista>()
                .ToTable("dentista");

            modelBuilder.Entity<EscalaTrabalho>()
                .ToTable("escala_trabalho");

            // REMOVIDO: modelBuilder.Entity<DisponibilidadeDentista>().ToTable("disponibilidade_dentista");

            modelBuilder.Entity<EscalaMensalDentista>()
                .ToTable("escala_mensal_dentista");

            modelBuilder.Entity<Agendamento>()
                .ToTable("agendamento");

            modelBuilder.Entity<Atendimento>()
                .ToTable("atendimento");

            modelBuilder.Entity<RecuperacaoSenhaToken>()
                .ToTable("RecuperacaoSenhaTokens");

            // NOVAS TABELAS
            modelBuilder.Entity<SolicitacaoVoluntario>()
                .ToTable("solicitacao_voluntario");

            modelBuilder.Entity<Odontograma>()
                .ToTable("odontograma");

            modelBuilder.Entity<TratamentoDente>()
                .ToTable("tratamento_dente");

            // Configuração do relacionamento Dentista -> EscalaTrabalho
            modelBuilder.Entity<Dentista>()
                .HasOne(d => d.EscalaTrabalho)
                .WithMany(e => e.Dentistas)
                .HasForeignKey(d => d.IdEscala)
                .OnDelete(DeleteBehavior.SetNull);

            // REMOVIDO: Configuração do relacionamento Dentista -> DisponibilidadeDentista

            // Configuração do relacionamento Dentista -> EscalaMensalDentista
            modelBuilder.Entity<EscalaMensalDentista>()
                .HasOne(e => e.Dentista)
                .WithMany()
                .HasForeignKey(e => e.IdDentista)
                .OnDelete(DeleteBehavior.Cascade);

            // NOVO: Configuração do relacionamento Odontograma -> Crianca
            modelBuilder.Entity<Odontograma>()
                .HasOne(o => o.Crianca)
                .WithMany()
                .HasForeignKey(o => o.IdCrianca)
                .OnDelete(DeleteBehavior.Cascade);

            // NOVO: Configuração do relacionamento TratamentoDente -> Odontograma
            modelBuilder.Entity<TratamentoDente>()
                .HasOne(t => t.Odontograma)
                .WithMany(o => o.Tratamentos)
                .HasForeignKey(t => t.IdOdontograma)
                .OnDelete(DeleteBehavior.Cascade);

            // NOVO: Configuração do relacionamento TratamentoDente -> Dentista
            modelBuilder.Entity<TratamentoDente>()
                .HasOne(t => t.Dentista)
                .WithMany()
                .HasForeignKey(t => t.IdDentista)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuração dos índices únicos para SolicitacaoVoluntario
            modelBuilder.Entity<SolicitacaoVoluntario>()
                .HasIndex(s => s.Email);

            modelBuilder.Entity<SolicitacaoVoluntario>()
                .HasIndex(s => s.Cpf);

            modelBuilder.Entity<SolicitacaoVoluntario>()
                .HasIndex(s => s.Cro);
        }
    }
}