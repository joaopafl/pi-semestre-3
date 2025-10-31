using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Pi_Odonto.Models;

namespace Pi_Odonto.Services
{
    public class EmailCadastroService : IEmailCadastroService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailCadastroService> _logger;

        public EmailCadastroService(IOptions<EmailSettings> emailSettings, ILogger<EmailCadastroService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task EnviarEmailVerificacaoAsync(string email, string nome, string tokenVerificacao)
        {
            try
            {
                var linkVerificacao = $"{_emailSettings.BaseUrl}/Responsavel/VerificarEmail?token={tokenVerificacao}";

                var assunto = "Confirme seu cadastro - Pi Odonto";
                var corpo = GerarCorpoEmailVerificacao(nome, linkVerificacao);

                await EnviarEmailAsync(email, assunto, corpo);
                _logger.LogInformation($"Email de verificação enviado para: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de verificação para: {email}");
                throw;
            }
        }

        public async Task EnviarEmailBoasVindasAsync(string email, string nome)
        {
            try
            {
                var assunto = "Bem-vindo ao Pi Odonto!";
                var corpo = GerarCorpoEmailBoasVindas(nome);

                await EnviarEmailAsync(email, assunto, corpo);
                _logger.LogInformation($"Email de boas-vindas enviado para: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de boas-vindas para: {email}");
                throw;
            }
        }

        private async Task EnviarEmailAsync(string destinatario, string assunto, string corpo)
        {
            using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPassword);
            client.EnableSsl = _emailSettings.EnableSsl;

            var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = assunto,
                Body = corpo,
                IsBodyHtml = true
            };

            message.To.Add(destinatario);

            await client.SendMailAsync(message);
        }

        private string GerarCorpoEmailVerificacao(string nome, string linkVerificacao)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }}
                        .header {{ text-align: center; color: #0d6efd; margin-bottom: 30px; }}
                        .button {{ display: inline-block; padding: 15px 30px; background-color: #0d6efd; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }}
                        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🦷 Pi Odonto</h1>
                            <h2>Confirme seu cadastro</h2>
                        </div>
                        
                        <p>Olá, <strong>{nome}</strong>!</p>
                        
                        <p>Obrigado por se cadastrar no Pi Odonto. Para concluir seu cadastro, você precisa confirmar seu endereço de email.</p>
                        
                        <p style='text-align: center; margin: 30px 0;'>
                            <a href='{linkVerificacao}' class='button'>✅ Confirmar Email</a>
                        </p>
                        
                        <p><strong>⚠️ Importante:</strong> Este link expira em 24 horas.</p>
                        
                        <p>Se você não conseguir clicar no botão, copie e cole este link no seu navegador:</p>
                        <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 5px;'>{linkVerificacao}</p>
                        
                        <div class='footer'>
                            <p>Se você não se cadastrou no Pi Odonto, pode ignorar este email.</p>
                            <p>© Pi Odonto - Sistema de Gestão Odontológica</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GerarCorpoEmailBoasVindas(string nome)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }}
                        .header {{ text-align: center; color: #0d6efd; margin-bottom: 30px; }}
                        .feature {{ background-color: #f8f9fa; padding: 15px; margin: 10px 0; border-radius: 5px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🦷 Pi Odonto</h1>
                            <h2>Bem-vindo!</h2>
                        </div>
                        
                        <p>Olá, <strong>{nome}</strong>!</p>
                        
                        <p>🎉 Parabéns! Seu cadastro foi confirmado com sucesso no Pi Odonto.</p>
                        
                        <h3>O que acontece agora?</h3>
                        
                        <div class='feature'>
                            📞 <strong>Contato:</strong> Nossa equipe entrará em contato em até 24 horas para agendar a primeira consulta.
                        </div>
                        
                        <div class='feature'>
                            🗓️ <strong>Agendamento:</strong> Você receberá informações sobre disponibilidade de horários.
                        </div>
                        
                        <div class='feature'>
                            📋 <strong>Documentos:</strong> Prepare os documentos necessários (RG, CPF, cartão SUS).
                        </div>
                        
                        <p><strong>Dúvidas?</strong> Entre em contato conosco:</p>
                        <p>📱 WhatsApp: (11) 99999-9999<br>
                           📧 Email: contato@piodonto.com</p>
                        
                        <p>Obrigado por escolher o Pi Odonto para cuidar da saúde bucal da sua família!</p>
                        
                        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666;'>
                            <p>© Pi Odonto - Sistema de Gestão Odontológica</p>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}