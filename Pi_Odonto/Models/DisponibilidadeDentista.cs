using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pi_Odonto.Models
{
    public class DisponibilidadeDentista
    {
        [Key]
        [Column("id_disponibilidade")]
        public int Id { get; set; }

        [Required]
        [Column("id_dentista")]
        [Display(Name = "Dentista")]
        public int IdDentista { get; set; }

        [Required]
        [StringLength(20)]
        [Column("dia_semana")]
        [Display(Name = "Dia da Semana")]
        public string DiaSemana { get; set; } = string.Empty;

        [Required]
        [Column("hora_inicio")]
        [Display(Name = "Hora de Início")]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        [Column("hora_fim")]
        [Display(Name = "Hora de Fim")]
        public TimeSpan HoraFim { get; set; }

        [Column("ativo")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Column("data_cadastro")]
        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        // Navigation property para o relacionamento com Dentista
        [ForeignKey("IdDentista")]
        public virtual Dentista? Dentista { get; set; }
    }
}
