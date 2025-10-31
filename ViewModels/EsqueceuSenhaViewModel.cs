using System.ComponentModel.DataAnnotations;

namespace Pi_Odonto.ViewModels
{
    public class EsqueceuSenhaViewModel
    {
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Digite um email válido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";
    }

    public class RedefinirSenha
    {
        [Required]
        public string Token { get; set; } = "";

        [Required(ErrorMessage = "A nova senha é obrigatória")]
        [StringLength(100, ErrorMessage = "A senha deve ter pelo menos {2} caracteres", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Nova Senha")]
        public string NovaSenha { get; set; } = "";

        [Required(ErrorMessage = "Confirme a nova senha")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nova Senha")]
        [Compare("NovaSenha", ErrorMessage = "As senhas não coincidem")]
        public string ConfirmarSenha { get; set; } = "";
    }
}