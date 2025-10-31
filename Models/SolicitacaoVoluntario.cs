using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pi_Odonto.Models
{
    [Table("solicitacao_voluntario")]
    public class SolicitacaoVoluntario
    {
        [Key]
        [Column("id_solicitacao")]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100)]
        [Column("nome")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Column("email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O telefone é obrigatório")]
        [StringLength(15)]
        [Phone(ErrorMessage = "Telefone inválido")]
        [Column("telefone")]
        [Display(Name = "Telefone")]
        public string Telefone { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CRO é obrigatório")]
        [StringLength(20)]
        [Column("cro")]
        [Display(Name = "Número do CRO")]
        public string Cro { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(14)]
        [Column("cpf")]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = string.Empty;

        [StringLength(200)]
        [Column("endereco")]
        [Display(Name = "Endereço")]
        public string? Endereco { get; set; }

        [Required(ErrorMessage = "A mensagem é obrigatória")]
        [StringLength(1000)]
        [Column("mensagem")]
        [Display(Name = "Mensagem / Motivação")]
        [DataType(DataType.MultilineText)]
        public string Mensagem { get; set; } = string.Empty;

        [Column("data_solicitacao")]
        [Display(Name = "Data da Solicitação")]
        public DateTime DataSolicitacao { get; set; } = DateTime.Now;

        [Column("status")]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pendente"; // Pendente, Aprovado, Rejeitado

        [Column("visualizado")]
        [Display(Name = "Visualizado")]
        public bool Visualizado { get; set; } = false;

        [Column("data_resposta")]
        [Display(Name = "Data da Resposta")]
        public DateTime? DataResposta { get; set; }

        [Column("observacao_admin")]
        [StringLength(500)]
        [Display(Name = "Observação do Administrador")]
        [DataType(DataType.MultilineText)]
        public string? ObservacaoAdmin { get; set; }
    }
}