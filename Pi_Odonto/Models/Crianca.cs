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
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(14)]
        [Column("cpf_crianca")]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = string.Empty;

        [Required]
        [Column("dt_nasc_crianca")]
        [Display(Name = "Data de Nascimento")]
        [DataType(DataType.Date)]
        // ** VALIDAÇÃO DE IDADE ADICIONADA AQUI **
        [CustomMaxAge(18, ErrorMessage = "Data inválida, a criança deve ter entre 1 à 18 anos.")]
        public DateTime DataNascimento { get; set; }

        [Required]
        [StringLength(20)]
        [Column("parentesco")]
        [Display(Name = "Parentesco")]
        public string Parentesco { get; set; } = string.Empty;

        [Required]
        [Column("id_resp")]
        [Display(Name = "Responsável")]
        public int IdResponsavel { get; set; }

        [Column("ativa")]
        [Display(Name = "Ativa")]
        public bool Ativa { get; set; } = true;

        // Navegação
        [ForeignKey("IdResponsavel")]
        public virtual Responsavel? Responsavel { get; set; }
    }
}

namespace Pi_Odonto.Models
{
    public class Criancaa
    {
        // ...
        [Required]
        [StringLength(14)]
        [Column("cpf_crianca")]
        [Display(Name = "CPF")]
        // >>> NOVO: Aplica o atributo para checar o CPF no banco
        [UniqueCpf(ErrorMessage = "Este CPF já está cadastrado no sistema.")]
        public string Cpf { get; set; } = string.Empty;
        // ...
    }
}