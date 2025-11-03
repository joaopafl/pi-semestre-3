using System.Linq;
using Pi_Odonto.Models;
using System.ComponentModel.DataAnnotations;

// Importe o namespace onde está o seu DbContext (Ajuste se necessário!)
// Importe o namespace onde está o seu Model Crianca (Ajuste se necessário!)

namespace Pi_Odonto.ValidationAttributes
{
    // O atributo 'AllowMultiple = false' garante que ele só pode ser aplicado uma vez por propriedade
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class UniqueCpfAttribute : ValidationAttribute
    {
        // Construtor padrão
        public UniqueCpfAttribute()
        {
            // Mensagem de erro padrão se nenhuma for especificada no Model
            ErrorMessage = "CPF já existe no sistema, digite outro.";
        }

        // Este é o método principal que o MVC chama para a validação Server-Side
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                // Se o campo for nulo ou vazio, a validação [Required] deve lidar com isso.
                return ValidationResult.Success;
            }

            // 1. Limpa o CPF para a consulta no banco (assumindo que o banco armazena apenas números)
            string cpfLimpo = value.ToString().Replace(".", "").Replace("-", "").Trim();

            // Verifica se o CPF tem o tamanho correto para evitar consultas desnecessárias
            if (cpfLimpo.Length != 11)
            {
                // Deixa o [StringLength] ou outro atributo lidar com a formatação inválida/incompleta
                return ValidationResult.Success;
            }

            // 2. Acessar o DbContext via Dependency Injection
            // Isso funciona porque o ValidationContext tem acesso ao ServiceProvider do ASP.NET Core

            // **IMPORTANTE: Mude 'PiOdontoContext' para o nome real da sua classe DbContext.**
            var dbContext = validationContext.GetService<PiOdontoContext>();

            if (dbContext == null)
            {
                // Indica um erro de configuração, o DbContext não está registrado no DI
                return new ValidationResult("Erro interno: O serviço de banco de dados não pôde ser acessado.");
            }

            // 3. Realizar a consulta no banco
            // Verifica se existe alguma criança (Criancas) no banco que já possui este CPF.
            // Converta o resultado de Set<Crianca>() para IQueryable<Crianca>
            var criancas = dbContext.Set<Crianca>() as IQueryable<Crianca>;
            if (criancas == null)
            {
                return new ValidationResult("Erro interno: O serviço de banco de dados não pôde ser acessado.");
            }

            bool cpfExiste = criancas.Any(c => c.Cpf.Replace(".", "").Replace("-", "") == cpfLimpo);

            if (cpfExiste)
            {
                // Validação falhou: CPF já existe
                return new ValidationResult(ErrorMessage);
            }

            // Validação passou
            return ValidationResult.Success;
        }
    }
}