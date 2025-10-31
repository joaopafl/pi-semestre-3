using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pi_Odonto.Models
{
    public class EscalaTrabalho
    {
        [Key]
        [Column("id_escala")]
        public int IdEscala { get; set; }

        [Required]
        [StringLength(20)]
        [Column("dt_disponivel")]
        [Display(Name = "Dia da Semana")]
        public string DtDisponivel { get; set; } = string.Empty;

        [Required]
        [Column("hr_inicio")]
        [Display(Name = "Horário de Início")]
        public int HrInicio { get; set; }

        [Required]
        [Column("hr_fim")]
        [Display(Name = "Horário de Fim")]
        public int HrFim { get; set; }

        // Navigation property para o relacionamento com Dentista
        public virtual ICollection<Dentista> Dentistas { get; set; } = new List<Dentista>();
    }
}
