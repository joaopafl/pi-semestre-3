using System;
using System.ComponentModel.DataAnnotations;

public class CustomMaxAgeAttribute : ValidationAttribute
{
    private readonly int _maxAge;

    // O construtor recebe a idade máxima permitida (neste caso, 18)
    public CustomMaxAgeAttribute(int maxAge)
    {
        _maxAge = maxAge;
    }

    // Este é o método principal de validação
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
        {
            // Se o campo for opcional e nulo, a validação passa.
            // Se for obrigatório, use o atributo [Required] no Model.
            return ValidationResult.Success;
        }

        if (value is DateTime dataNascimento)
        {
            // Calcula a data limite: hoje, menos a idade máxima permitida.
            // Uma data de nascimento válida deve ser igual ou posterior a esta data limite.
            var dataLimite = DateTime.Today.AddYears(-_maxAge);

            // Adiciona 1 dia para que a criança possa fazer 18 anos E SE MANTER ATÉ 18
            // Se você quer que a criança seja *estritamente menor* de 18, remova o .AddDays(1)
            // No seu caso, 'até 18 anos' significa que a data de nascimento deve ser >= dataLimite

            if (dataNascimento > dataLimite)
            {
                // Data de nascimento está OK (a criança é mais nova que o limite)
                return ValidationResult.Success;
            }
            else
            {
                // Data de nascimento não está OK (a criança é mais velha que o limite)
                // Use a mensagem de erro que foi passada no construtor (ErrorMessage)
                return new ValidationResult(ErrorMessage ?? $"Permitido criança com no máximo {_maxAge} anos.");
            }
        }

        // Se o valor não for uma DateTime (o que não deve acontecer), permite a passagem.
        return ValidationResult.Success;
    }
}