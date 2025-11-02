using Pi_Odonto.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Pi_Odonto.ViewModels
{
    public class EditarPerfilResponsavelViewModel
    {
        // ========================================
        // DADOS DO RESPONSÁVEL
        // ========================================
        public int Id { get; set; }

        [Required(ErrorMessage = "O Nome Completo é obrigatório.")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; } = ""; // Garante que não é nulo

        // CORREÇÃO CS0117/CS1061: Propriedades necessárias para o Controller (GET e POST)
        public string Email { get; set; } = ""; // Usado como hidden field
        public string Cpf { get; set; } = "";   // Usado como hidden field

        [Required(ErrorMessage = "O Telefone é obrigatório.")]
        [StringLength(15, ErrorMessage = "O telefone não pode exceder 15 caracteres (incluindo máscara).")]
        [Display(Name = "Telefone")]
        public string Telefone { get; set; } = "";

        [Display(Name = "Endereço")]
        public string? Endereco { get; set; } // Pode ser nulo

        // CORREÇÃO CS0117/CS1061: Adicionado Ativo (Usado no GET e no POST para reexibição)
        [Display(Name = "Status da Conta")]
        public bool Ativo { get; set; } = true; 

        // ========================================
        // ALTERAÇÃO DE SENHA (Opcional)
        // ========================================

        // CORREÇÃO CS0117/CS1061: Propriedades de Senha
        [DataType(DataType.Password)]
        [Display(Name = "Senha Atual")]
        public string? SenhaAtual { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "A nova senha deve ter pelo menos {2} caracteres.", MinimumLength = 8)]
        [Display(Name = "Nova Senha")]
        public string? NovaSenha { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NovaSenha), ErrorMessage = "A nova senha e a confirmação não coincidem.")]
        [Display(Name = "Confirmar Nova Senha")]
        public string? ConfirmarNovaSenha { get; set; }

        // ========================================
        // GESTÃO DE CRIANÇAS
        // ========================================

        // A lista que o Model Binder vai preencher
        public List<Crianca> Criancas { get; set; } = new List<Crianca>();

        // Usado para popular o Dropdown de Parentesco na View
        public List<string> OpcoesParentesco { get; set; } = new List<string>
        {
            "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
        };
    }
}