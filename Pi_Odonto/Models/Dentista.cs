﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; 
using System;

namespace Pi_Odonto.Models
{
    public class Dentista
    {
        [Key]
        [Column("id_dentista")]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Column("nome_dent")]
        [Display(Name = "Nome do dentista")]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(11)]
        [Column("cpf_dent")]
        [Display(Name = "CPF")]
        public string Cpf { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [Column("cro")]
        [Display(Name = "CRO")]
        public string Cro { get; set; } = string.Empty;

        [Required]
        [StringLength(60)]
        [Column("endereco_dent")]
        [Display(Name = "Endereço")]
        public string Endereco { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Column("email_dent")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        [Column("tel_dent")]
        [Display(Name = "Telefone")]
        public string Telefone { get; set; } = string.Empty;

        [Column("id_escala")]
        [Display(Name = "Escala de Trabalho")]
        public int? IdEscala { get; set; }

        // MUDANÇA CRÍTICA: REMOÇÃO TEMPORÁRIA DA PROPRIEDADE ATIVO
        // [Column("ativo")] 
        // public bool Ativo { get; set; } = true; 
        
        // Se você precisar da propriedade, mas ela não está no banco:
        // [NotMapped]
        // public bool Ativo { get; set; } = true;
        
        // Omitindo a propriedade "Ativo" por enquanto para forçar o EF a parar de procurá-la

        // Navigation property
        [ForeignKey("IdEscala")]
        public virtual EscalaTrabalho? EscalaTrabalho { get; set; }

        public virtual ICollection<DisponibilidadeDentista> Disponibilidades { get; set; } = new List<DisponibilidadeDentista>();
    }
}