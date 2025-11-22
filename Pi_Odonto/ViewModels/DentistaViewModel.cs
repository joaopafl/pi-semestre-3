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

        // Método para inicializar as disponibilidades com todos os dias e turnos
        public static DentistaViewModel CriarComDisponibilidades()
        {
            var viewModel = new DentistaViewModel();
            
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

        // Método para carregar disponibilidades existentes de um dentista
        public static DentistaViewModel CarregarComDisponibilidadesExistentes(DentistaViewModel baseViewModel, List<Pi_Odonto.Models.DisponibilidadeDentista> disponibilidadesExistentes)
        {
            var viewModel = baseViewModel;
            
            // Inicializar com todas as disponibilidades possíveis
            var diasSemana = new[] { "Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado", "Domingo" };
            viewModel.Disponibilidades = new List<DisponibilidadeItem>();
            
            foreach (var dia in diasSemana)
            {
                var horarios = new[]
                {
                    new { Inicio = new TimeSpan(8, 0, 0), Fim = new TimeSpan(12, 0, 0) },
                    new { Inicio = new TimeSpan(13, 0, 0), Fim = new TimeSpan(17, 0, 0) },
                    new { Inicio = new TimeSpan(18, 0, 0), Fim = new TimeSpan(22, 0, 0) }
                };

                foreach (var horario in horarios)
                {
                    // Verificar se esta disponibilidade já existe para o dentista
                    var existe = disponibilidadesExistentes.Any(d => 
                        d.DiaSemana == dia && 
                        d.HoraInicio == horario.Inicio && 
                        d.HoraFim == horario.Fim &&
                        d.Ativo);

                    viewModel.Disponibilidades.Add(new DisponibilidadeItem
                    {
                        DiaSemana = dia,
                        HoraInicio = horario.Inicio,
                        HoraFim = horario.Fim,
                        Selecionado = existe
                    });
                }
            }

            return viewModel;
        }
    }

    public class DisponibilidadeItem
    {
        public string DiaSemana { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
        public bool Selecionado { get; set; }
    }
}