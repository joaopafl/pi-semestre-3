using System;

namespace Pi_Odonto.ViewModels
{
    public class DisponibilidadeViewModel
    {
        public string DiaSemana { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
        public bool Selecionado { get; set; }
    }

    // Alias para compatibilidade
    public class DisponibilidadeItemV : DisponibilidadeViewModel
    {
    }
}