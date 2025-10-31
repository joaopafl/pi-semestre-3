using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pi_Odonto.Models
{
    public class Responsavel
    {
        internal DateTime DataAtualizacao;

        [Key]
        [Column("id_resp")]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Column("nome_resp")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        [Column("cpf_resp")]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        [Column("tel_resp")]
        [Display(Name = "Telefone")]
        public string Telefone { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Column("email_resp")]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(60)]
        [Column("endereco_resp")]
        [Display(Name = "Endereço")]
        public string Endereco { get; set; } = string.Empty;

        [Column("ativo")]
        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Column("data_cadastro")]
        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        [Column("senha_resp")]
        [Display(Name = "Senha")]
        public string? Senha { get; set; }

        [Column("email_verificado")]
        [Display(Name = "Email Verificado")]
        public bool EmailVerificado { get; set; } = false;

        [StringLength(255)]
        [Column("token_verificacao")]
        [Display(Name = "Token de Verificação")]
        public string? TokenVerificacao { get; set; }

        // Navegação - Lista de crianças
        public virtual ICollection<Crianca> Criancas { get; set; } = new List<Crianca>();
    }
}