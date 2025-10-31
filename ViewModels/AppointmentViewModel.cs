using System;
using System.Collections.Generic;
using Pi_Odonto.Models;

namespace Pi_Odonto.ViewModels
{
    public class AppointmentViewModel
    {
        // ID do Agendamento que está sendo editado (0 para novo)
        public int AgendamentoId { get; set; } 
        
        // =======================================================
        // PROPRIEDADES DE EXIBIÇÃO (GET)
        // =======================================================
        public List<Crianca> Children { get; set; } = new List<Crianca>();
        public List<DateTime> AvailableDates { get; set; } = new List<DateTime>();
        
        // =======================================================
        // PROPRIEDADES DE ENVIO (POST - Dados que vêm do formulário)
        // =======================================================
        
        public int SelectedChildId { get; set; } // ID da Criança
        public string SelectedDateString { get; set; } = string.Empty; // Data selecionada (ex: "2025-10-24")
        public string SelectedTime { get; set; } = string.Empty; // Horário selecionado (ex: "09:00")
        public int SelectedDentistaId { get; set; } // ID do Dentista selecionado
    }
}