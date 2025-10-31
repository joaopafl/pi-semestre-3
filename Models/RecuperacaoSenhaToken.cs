using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pi_Odonto.Models
{
    [Table("RecuperacaoSenhaTokens")]
    public class RecuperacaoSenhaToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Token { get; set; } = "";

        [Required]
        public DateTime DataCriacao { get; set; }

        [Required]
        public DateTime DataExpiracao { get; set; }

        [Required]
        public bool Usado { get; set; } = false;

        // Propriedade calculada para verificar se o token ainda é válido
        [NotMapped]
        public bool IsValido => !Usado && DataExpiracao > DateTime.Now;
    }
}