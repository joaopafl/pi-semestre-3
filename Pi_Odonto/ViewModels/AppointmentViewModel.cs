using System;
using System.Collections.Generic;
using Pi_Odonto.Models;

namespace Pi_Odonto.ViewModels
{
    public class AppointmentViewModel
    {
        // =======================================================
        // PROPRIEDADES DE EXIBIÇÃO (GET - Dados para a tela)
        // =======================================================
        public List<Crianca> Children { get; set; } = new List<Crianca>();
        public DateTime SelectedDate { get; set; } = DateTime.Today; 
        public List<DateTime> AvailableSaturdays { get; set; } = new List<DateTime>();
        public List<string> AvailableTimes { get; set; } = new List<string>
        {
            "08:00", "09:00", "10:00", "11:00", 
            "13:00", "14:00"
        };
        
        // =======================================================
        // PROPRIEDADES DE ENVIO (POST - Dados que vêm do formulário)
        // =======================================================
        
        // Recebe o ID da criança selecionada
        public int SelectedChildId { get; set; } 

        // Recebe a data selecionada do input oculto (formato YYYY-MM-DD)
        // O nome foi ajustado para SelectedDateString para evitar conflito com SelectedDate (DateTime)
        public string SelectedDateString { get; set; } = string.Empty; 

        // Recebe o horário selecionado do input oculto (formato HH:mm)
        public string SelectedTime { get; set; } = string.Empty;
        
        // PROPRIEDADE PARA FUTURA INTEGRAÇÃO DO DENTISTA
        public int SelectedDentistaId { get; set; }
    }
}