using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pi_Odonto.Models
{
    public class Crianca
    {
        [Key]
        [Column("id_crianca")]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Column("nome_crianca")]
        [Display(Name = "Nome da Criança")]
        public string Nome { get; set; }

        [Required]
        [StringLength(11)]
        [Column("cpf_crianca")]
        [Display(Name = "CPF")]
        public string Cpf { get; set; }

        [Required]
        [Column("dt_nasc_crianca")]
        [Display(Name = "Data de Nascimento")]
        [DataType(DataType.Date)]
        public DateTime DataNascimento { get; set; }

        [Required]
        [StringLength(20)]
        [Column("parentesco")]
        [Display(Name = "Parentesco")]
        public string Parentesco { get; set; }

        [Required]
        [Column("id_resp")]
        [Display(Name = "Responsável")]
        public int IdResponsavel { get; set; }

        // Navegação - SEM [Required] e nullable
        [ForeignKey("IdResponsavel")]
        public virtual Responsavel? Responsavel { get; set; }
    }
}