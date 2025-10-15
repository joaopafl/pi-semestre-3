using Pi_Odonto.Models;

public class ResponsavelCriancaViewModel
{
    public Responsavel Responsavel { get; set; } = new Responsavel(); // SEM [Required]

    public List<Crianca> Criancas { get; set; } = new List<Crianca> { new Crianca() };

    public List<string> OpcoesParentesco { get; set; } = new List<string>
    {
        "Pai", "Mãe", "Avô", "Avó", "Tio", "Tia", "Padrasto", "Madrasta", "Tutor Legal"
    };
}