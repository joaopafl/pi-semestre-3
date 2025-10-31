using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pi_Odonto.Models
{
    [Table("atendimento")]
    public class Atendimento
    {
        [Key]
        [Column("id_atendimento")]
        public int Id { get; set; }

        [Required]
        [Column("dt_atendimento")]
        [Display(Name = "Data do Atendimento")]
        [DataType(DataType.Date)]
        public DateTime DataAtendimento { get; set; } = DateTime.Now;

        [Required]
        [Column("duracao_atendimento")]
        [Display(Name = "Duração do Atendimento (minutos)")]
        public int DuracaoAtendimento { get; set; }

        [Required]
        [Column("hr_atendimento")]
        [Display(Name = "Horário do Atendimento")]
        [DataType(DataType.Time)]
        public TimeSpan HorarioAtendimento { get; set; }

        [Column("observacao")]
        [StringLength(100)] // Baseado no varchar(100) da estrutura do BD
        [Display(Name = "Observações")]
        [DataType(DataType.MultilineText)]
        public string? Observacao { get; set; }

        // Removendo desc_atendimento pois não existe na estrutura do BD

        [Required]
        [Column("id_crianca")]
        [Display(Name = "Criança")]
        public int IdCrianca { get; set; }

        [Required]
        [Column("id_dentista")]
        [Display(Name = "Dentista")]
        public int IdDentista { get; set; }

        [Column("id_agenda")]
        [Display(Name = "Agenda")]
        public int? IdAgenda { get; set; }

        [Column("id_odontograma")]
        [Display(Name = "Odontograma")]
        public int? IdOdontograma { get; set; }

        // Navegação - Relacionamentos
        [ForeignKey("IdCrianca")]
        public virtual Crianca? Crianca { get; set; }

        [ForeignKey("IdDentista")]
        public virtual Dentista? Dentista { get; set; }

        // Relacionamentos comentados temporariamente
        // [ForeignKey("IdAgenda")]
        // public virtual Agenda? Agenda { get; set; }

        // [ForeignKey("IdOdontograma")]
        // public virtual Odontograma? Odontograma { get; set; }
    }
}