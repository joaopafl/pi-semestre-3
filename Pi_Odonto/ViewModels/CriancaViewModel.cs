using System;
using System.ComponentModel.DataAnnotations;

namespace Pi_Odonto.ViewModels
{
    public class CriancaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da criança é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        [DataType(DataType.Date)]
        public DateTime? DataNascimento { get; set; }  // <-- MUDANÇA AQUI: adicionei "?"

        [Required(ErrorMessage = "O gênero é obrigatório.")]
        [StringLength(20)]
        public string Genero { get; set; } = string.Empty;

        [Range(0.5, 2.5, ErrorMessage = "Altura deve estar entre 0,5m e 2,5m.")]
        public double? Altura { get; set; }

        [Range(2, 200, ErrorMessage = "Peso deve estar entre 2kg e 200kg.")]
        public double? Peso { get; set; }

        [StringLength(200, ErrorMessage = "O campo observações pode ter no máximo 200 caracteres.")]
        public string Observacoes { get; set; } = string.Empty;

        [Required(ErrorMessage = "O parentesco é obrigatório.")]
        [StringLength(50)]
        public string Parentesco { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [StringLength(14, ErrorMessage = "CPF deve ter formato válido.")]
        public string Cpf { get; set; } = string.Empty;

        // Propriedade calculada para idade (com verificação de null)
        public int? Idade
        {
            get
            {
                if (DataNascimento.HasValue)
                {
                    var hoje = DateTime.Now;
                    int idade = hoje.Year - DataNascimento.Value.Year;

                    // Ajusta se ainda não fez aniversário este ano
                    if (hoje.Month < DataNascimento.Value.Month ||
                        (hoje.Month == DataNascimento.Value.Month && hoje.Day < DataNascimento.Value.Day))
                    {
                        idade--;
                    }

                    return idade;
                }
                return null;
            }
        }
    }
}