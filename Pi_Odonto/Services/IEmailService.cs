using Pi_Odonto.Models;

namespace Pi_Odonto.Services
{
    public interface IEmailService
    {
        Task EnviarEmailVerificacaoAsync(string email, string nome, string tokenVerificacao);
        Task EnviarEmailBoasVindasAsync(string email, string nome);

        Task EnviarEmailRecuperacaoSenhaAsync(string email, string nome, string tokenRecuperacao);
        Task EnviarEmailBoasVindasDentistaAsync(string email, string nome);
       
    }
}