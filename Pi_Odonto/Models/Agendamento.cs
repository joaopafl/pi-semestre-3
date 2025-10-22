using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace Pi_Odonto.Models
{
    [Table("agendamento")] 
    public class Agendamento
    {
        [Key]
        [Column("id_agenda")]
        public int Id { get; set; }

        [Required]
        [Column("dt_agenda")]
        [DataType(DataType.Date)]
        [Display(Name = "Data do Agendamento")]
        public DateTime DataAgendamento { get; set; } 

        // Mapeamento explícito para o tipo TIME do MySQL, crucial para resolver o erro
        [Required]
        [Column("hr_agenda", TypeName = "time")] 
        [Display(Name = "Hora do Agendamento")]
        public TimeSpan HoraAgendamento { get; set; }

        [Required]
        [Column("id_dentista")]
        [Display(Name = "Dentista")]
        public int IdDentista { get; set; }

        [Required]
        [Column("id_crianca")]
        [Display(Name = "Criança")]
        public int IdCrianca { get; set; }
        
        // --- Navegação ---
        [ForeignKey("IdCrianca")]
        public virtual Crianca? Crianca { get; set; }
    }
}