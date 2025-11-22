using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pi_Odonto.ViewModels
{
    public class VoluntarioCadastroViewModel
    {
        // Dados do Dentista
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(50)]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(11)]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CRO é obrigatório")]
        [StringLength(10)]
        [Display(Name = "Número do CRO")]
        public string Cro { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(50)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O telefone é obrigatório")]
        [StringLength(15)]
        [Phone(ErrorMessage = "Telefone inválido")]
        [Display(Name = "Telefone/WhatsApp")]
        public string Telefone { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Endereço")]
        public string? Endereco { get; set; }

        [Required(ErrorMessage = "A mensagem é obrigatória")]
        [StringLength(1000)]
        [Display(Name = "Mensagem / Motivação")]
        [DataType(DataType.MultilineText)]
        public string Mensagem { get; set; } = string.Empty;

        // Disponibilidades
        public List<DisponibilidadeItem> Disponibilidades { get; set; } = new List<DisponibilidadeItem>();

        // Método para inicializar as disponibilidades com todos os dias e turnos
        public static VoluntarioCadastroViewModel CriarComDisponibilidades()
        {
            var viewModel = new VoluntarioCadastroViewModel();
            
            var diasSemana = new[] { "Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado", "Domingo" };
            
            foreach (var dia in diasSemana)
            {
                // Turno Manhã: 08:00 - 12:00
                viewModel.Disponibilidades.Add(new DisponibilidadeItem
                {
                    DiaSemana = dia,
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFim = new TimeSpan(12, 0, 0),
                    Selecionado = false
                });

                // Turno Tarde: 13:00 - 17:00
                viewModel.Disponibilidades.Add(new DisponibilidadeItem
                {
                    DiaSemana = dia,
                    HoraInicio = new TimeSpan(13, 0, 0),
                    HoraFim = new TimeSpan(17, 0, 0),
                    Selecionado = false
                });

                // Turno Noite: 18:00 - 22:00
                viewModel.Disponibilidades.Add(new DisponibilidadeItem
                {
                    DiaSemana = dia,
                    HoraInicio = new TimeSpan(18, 0, 0),
                    HoraFim = new TimeSpan(22, 0, 0),
                    Selecionado = false
                });
            }

            return viewModel;
        }
    }
}

