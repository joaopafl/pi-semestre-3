using System;
using System.ComponentModel.DataAnnotations;

namespace Pi_Odonto.ViewModels
{
    public class CriancaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da criança é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        [DataType(DataType.Date)]
        public DateTime DataNascimento { get; set; }

        [Required(ErrorMessage = "O gênero é obrigatório.")]
        [StringLength(20)]
        public string Genero { get; set; }

        [Range(0.5, 2.5, ErrorMessage = "Altura deve estar entre 0,5m e 2,5m.")]
        public double? Altura { get; set; }

        [Range(2, 200, ErrorMessage = "Peso deve estar entre 2kg e 200kg.")]
        public double? Peso { get; set; }

        [StringLength(200, ErrorMessage = "O campo observações pode ter no máximo 200 caracteres.")]
        public string Observacoes { get; set; }

        // Propriedades adicionais baseadas na view
        [Required(ErrorMessage = "O parentesco é obrigatório.")]
        [StringLength(50)]
        public string Parentesco { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [StringLength(14, ErrorMessage = "CPF deve ter formato válido.")]
        public string Cpf { get; set; }

        // Propriedade calculada para idade
        public int Idade
        {
            get
            {
                return DateTime.Now.Year - DataNascimento.Year;
            }
        }
    }
}