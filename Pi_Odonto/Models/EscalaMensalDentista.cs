using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pi_Odonto.Models
{
    /// <summary>
    /// Representa um bloco de 1 hora de escala de um dentista em uma data específica
    /// </summary>
    public class EscalaMensalDentista
    {
        [Key]
        [Column("id_escala_mensal")]
        public int Id { get; set; }

        [Required]
        [Column("id_dentista")]
        [Display(Name = "Dentista")]
        public int IdDentista { get; set; }

        [Required]
        [Column("data_escala", TypeName = "date")]
        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateTime DataEscala { get; set; }

        [Required]
        [Column("hora_inicio", TypeName = "time")]
        [Display(Name = "Hora de Início")]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        [Column("hora_fim", TypeName = "time")]
        [Display(Name = "Hora de Fim")]
        public TimeSpan HoraFim { get; set; }

        [Column("ativo")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Column("data_cadastro")]
        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("IdDentista")]
        public virtual Dentista? Dentista { get; set; }
    }
}

