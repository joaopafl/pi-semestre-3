// Pi_Odonto.ViewModels/AppointmentViewModel.cs

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
        
        public List<DateTime> AvailableDates { get; set; } = new List<DateTime>();

        public List<Dentista> AvailableDentists { get; set; } = new List<Dentista>();
        
        // NOVO: Nome do dentista (para exibir no modal)
        public string SelectedDentistaName { get; set; } = "Não Selecionado";
        
        // =======================================================
        // PROPRIEDADES DE ENVIO (POST - Dados que vêm do formulário)
        // =======================================================
        
        public int SelectedChildId { get; set; } 
        public string SelectedDateString { get; set; } = string.Empty; 
        public string SelectedTime { get; set; } = string.Empty;
        
        public int SelectedDentistaId { get; set; }
    }
}