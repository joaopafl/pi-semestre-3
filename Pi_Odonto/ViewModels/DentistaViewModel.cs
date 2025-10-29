using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pi_Odonto.ViewModels
{
    public class DentistaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(50)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(11)]
        public string Cpf { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CRO é obrigatório")]
        [StringLength(10)]
        public string Cro { get; set; } = string.Empty;

        [Required(ErrorMessage = "O endereço é obrigatório")]
        [StringLength(60)]
        public string Endereco { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(50)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O telefone é obrigatório")]
        [StringLength(15)]
        public string Telefone { get; set; } = string.Empty;

        public int? IdEscala { get; set; }

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