using System;
using System.Collections.Generic;
using Pi_Odonto.Models; // Necessário para usar List<Crianca>

namespace Pi_Odonto.ViewModels
{
    public class AppointmentViewModel
    {
        // Corrigido para usar o Model de BD Crianca
        public List<Crianca> Children { get; set; } = new List<Crianca>();
        
        // Propriedades mantidas
        public DateTime SelectedDate { get; set; }
        public List<DateTime> AvailableSaturdays { get; set; } = new List<DateTime>();
        public List<string> AvailableTimes { get; set; } = new List<string>
        {
            "08:00", "09:00", "10:00", "11:00", 
            "13:00", "14:00"
        };
    }
}