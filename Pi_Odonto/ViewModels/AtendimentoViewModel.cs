using System.ComponentModel.DataAnnotations;
using Pi_Odonto.Models;

namespace Pi_Odonto.ViewModels
{
    public class AtendimentoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A data do atendimento é obrigatória")]
        [Display(Name = "Data do Atendimento")]
        [DataType(DataType.Date)]
        public DateTime DataAtendimento { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "O horário do atendimento é obrigatório")]
        [Display(Name = "Horário do Atendimento")]
        [DataType(DataType.Time)]
        public TimeSpan HorarioAtendimento { get; set; }

        [Required(ErrorMessage = "A duração do atendimento é obrigatória")]
        [Display(Name = "Duração do Atendimento (minutos)")]
        [Range(15, 480, ErrorMessage = "A duração deve ser entre 15 minutos e 8 horas")]
        public int DuracaoAtendimento { get; set; }

        [StringLength(100, ErrorMessage = "A observação não pode exceder 100 caracteres")]
        [Display(Name = "Observações")]
        [DataType(DataType.MultilineText)]
        public string? Observacao { get; set; }

        [Required(ErrorMessage = "Selecione uma criança")]
        [Display(Name = "Criança")]
        public int IdCrianca { get; set; }

        [Required(ErrorMessage = "Selecione um dentista")]
        [Display(Name = "Dentista")]
        public int IdDentista { get; set; }

        [Display(Name = "Agenda")]
        public int? IdAgenda { get; set; }

        [Display(Name = "Odontograma")]
        public int? IdOdontograma { get; set; }

        // Propriedades para popular os dropdowns nas views
        public List<Crianca>? CriancasDisponiveis { get; set; }
        public List<Dentista>? DentistasDisponiveis { get; set; }
        // Agenda será implementado posteriormente como módulo separado
        // public List<Agenda>? AgendasDisponiveis { get; set; }

        // Propriedades calculadas para exibição
        [Display(Name = "Data e Hora Completa")]
        public DateTime DataHoraCompleta => DataAtendimento.Add(HorarioAtendimento);

        [Display(Name = "Horário de Término")]
        public TimeSpan HorarioTermino => HorarioAtendimento.Add(TimeSpan.FromMinutes(DuracaoAtendimento));

        // Propriedades para facilitar a exibição nas views
        public string? NomeCrianca { get; set; }
        public string? NomeDentista { get; set; }

        // Formatação customizada para exibição
        public string HorarioFormatado => HorarioAtendimento.ToString(@"hh\:mm");
        public string DuracaoFormatada => $"{DuracaoAtendimento} min";
        public string PeriodoCompleto => $"{HorarioFormatado} às {HorarioTermino:hh\\:mm} ({DuracaoFormatada})";
    }
}
