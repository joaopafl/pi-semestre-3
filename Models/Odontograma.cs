using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pi_Odonto.Models
{
    // ============================================
    // MODELO: ODONTOGRAMA
    // ============================================
    [Table("odontograma")]
    public class Odontograma
    {
        [Key]
        [Column("id_odontograma")]
        public int Id { get; set; }

        [Required]
        [Column("id_crianca")]
        [Display(Name = "Criança")]
        public int IdCrianca { get; set; }

        [Column("data_criacao")]
        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        [Column("data_atualizacao")]
        [Display(Name = "Última Atualização")]
        public DateTime DataAtualizacao { get; set; } = DateTime.Now;

        [Column("observacoes_gerais")]
        [StringLength(500)]
        [Display(Name = "Observações Gerais")]
        [DataType(DataType.MultilineText)]
        public string? ObservacoesGerais { get; set; }

        // Navegação
        [ForeignKey("IdCrianca")]
        public virtual Crianca? Crianca { get; set; }

        public virtual ICollection<TratamentoDente> Tratamentos { get; set; } = new List<TratamentoDente>();
    }

    // ============================================
    // MODELO: TRATAMENTO DO DENTE
    // ============================================
    [Table("tratamento_dente")]
    public class TratamentoDente
    {
        [Key]
        [Column("id_tratamento")]
        public int Id { get; set; }

        [Required]
        [Column("id_odontograma")]
        [Display(Name = "Odontograma")]
        public int IdOdontograma { get; set; }

        [Required]
        [Column("numero_dente")]
        [Display(Name = "Número do Dente")]
        [Range(11, 85, ErrorMessage = "Número do dente inválido")]
        public int NumeroDente { get; set; }

        [Required]
        [Column("tipo_tratamento")]
        [StringLength(50)]
        [Display(Name = "Tipo de Tratamento")]
        public string TipoTratamento { get; set; } = string.Empty;
        // Tipos: Cárie, Restauração, Extração, Canal, Ausente, Hígido, etc.

        [Column("face")]
        [StringLength(50)]
        [Display(Name = "Face do Dente")]
        public string? Face { get; set; }
        // Faces: Oclusal, Vestibular, Lingual, Mesial, Distal

        [Column("status")]
        [StringLength(20)]
        [Display(Name = "Status do Tratamento")]
        public string Status { get; set; } = "Planejado";
        // Status: Planejado, Em Andamento, Concluído

        [Column("data_tratamento")]
        [Display(Name = "Data do Tratamento")]
        [DataType(DataType.Date)]
        public DateTime? DataTratamento { get; set; }

        [Column("observacao")]
        [StringLength(300)]
        [Display(Name = "Observação")]
        [DataType(DataType.MultilineText)]
        public string? Observacao { get; set; }

        [Column("id_dentista")]
        [Display(Name = "Dentista Responsável")]
        public int? IdDentista { get; set; }

        // Navegação
        [ForeignKey("IdOdontograma")]
        public virtual Odontograma? Odontograma { get; set; }

        [ForeignKey("IdDentista")]
        public virtual Dentista? Dentista { get; set; }
    }
}