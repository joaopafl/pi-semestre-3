
namespace Pi_Odonto.Services
{
    public interface IEmailCadastroService
    {
        Task EnviarEmailBoasVindasAsync(string email, string nome);
        Task EnviarEmailVerificacaoAsync(string email, string nome, string tokenVerificacao);
    }
}