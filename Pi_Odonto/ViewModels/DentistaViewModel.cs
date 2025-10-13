using System.ComponentModel.DataAnnotations;
using Pi_Odonto.Models;

namespace Pi_Odonto.ViewModels
{
    public class DentistaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(50, ErrorMessage = "O nome deve ter no máximo 50 caracteres")]
        [Display(Name = "Nome do Dentista")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(11, ErrorMessage = "O CPF deve ter 11 dígitos")]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CRO é obrigatório")]
        [StringLength(10, ErrorMessage = "O CRO deve ter no máximo 10 caracteres")]
        [Display(Name = "CRO")]
        public string Cro { get; set; } = string.Empty;

        [Required(ErrorMessage = "O endereço é obrigatório")]
        [StringLength(60, ErrorMessage = "O endereço deve ter no máximo 60 caracteres")]
        [Display(Name = "Endereço")]
        public string Endereco { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [StringLength(50, ErrorMessage = "O email deve ter no máximo 50 caracteres")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O telefone é obrigatório")]
        [StringLength(15, ErrorMessage = "O telefone deve ter no máximo 15 caracteres")]
        [Display(Name = "Telefone")]
        public string Telefone { get; set; } = string.Empty;

        // Lista de disponibilidades selecionadas
        public List<DisponibilidadeItem> Disponibilidades { get; set; } = new List<DisponibilidadeItem>();
    }

    public class DisponibilidadeItem
    {
        public string DiaSemana { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
        public bool Selecionado { get; set; }
    }
}
