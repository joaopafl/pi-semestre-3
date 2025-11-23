// Services/EmailService.cs
using Microsoft.Extensions.Options;
using Pi_Odonto.Models;
using System.IO.Pipelines;
using System.Net;
using System.Net.Mail;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;

namespace Pi_Odonto.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
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

        public async Task EnviarEmailRecuperacaoSenhaAsync(string email, string nome, string tokenRecuperacao)
        {
            try
            {
                var linkRecuperacao = $"{_emailSettings.BaseUrl}/RedefinirSenha?token={tokenRecuperacao}";

                var assunto = "Recuperação de Senha - Casa Espírita Trabalhadores de Jesus";
                var corpo = GerarCorpoEmailRecuperacaoSenha(nome, linkRecuperacao);

                await EnviarEmailAsync(email, assunto, corpo);
                _logger.LogInformation($"Email de recuperação de senha enviado para: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de recuperação de senha para: {email}");
                throw;
            }
        }

        public async Task EnviarEmailBoasVindasDentistaAsync(string email, string nome)
        {
            try
            {
                var assunto = "🎉 Bem-vindo à Casa Espírita Trabalhadores de Jesus!";
                var corpo = GerarCorpoEmailBoasVindasDentista(nome);

                await EnviarEmailAsync(email, assunto, corpo);
                _logger.LogInformation($"Email de boas-vindas (dentista) enviado para: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de boas-vindas (dentista) para: {email}");
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

        private string GerarCorpoEmailRecuperacaoSenha(string nome, string linkRecuperacao)
        {
            return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ font-family: 'Poppins', Arial, sans-serif; margin: 0; padding: 20px; background: linear-gradient(135deg, #E3F2FD, #E8F5E8); }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 25px; box-shadow: 0 20px 60px rgba(0,0,0,0.15); overflow: hidden; }}
                .header {{ background: linear-gradient(135deg, #4A90E2, #66BB6A); color: white; text-align: center; padding: 40px 30px 30px; }}
                .header h1 {{ margin: 0; font-size: 2rem; }}
                .header .icon {{ font-size: 3rem; margin-bottom: 1rem; }}
                .content {{ padding: 40px 30px; }}
                .button {{ display: inline-block; padding: 15px 30px; background: linear-gradient(135deg, #66BB6A, #FF6B8A); color: white; text-decoration: none; border-radius: 15px; font-weight: bold; box-shadow: 0 8px 25px rgba(102, 187, 106, 0.3); }}
                .button:hover {{ transform: translateY(-2px); }}
                .footer {{ padding: 30px; background-color: #f8f9fa; text-align: center; font-size: 12px; color: #666; }}
                .highlight {{ background-color: #fff5f5; padding: 15px; border-radius: 15px; border-left: 5px solid #FF6B8A; margin: 20px 0; }}
                .security-info {{ background: linear-gradient(135deg, #E3F2FD, #E8F5E8); padding: 20px; border-radius: 15px; border-left: 5px solid #FFD700; margin: 20px 0; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <div class='icon'>🔐</div>
                    <h1>Recuperação de Senha</h1>
                    <p>Casa Espírita Trabalhadores de Jesus</p>
                </div>
                
                <div class='content'>
                    <p>Olá, <strong>{nome}</strong>!</p>
                    
                    <p>❤️ <strong>Não se preocupe!</strong> Recebemos sua solicitação para recuperar o acesso à sua conta. Estamos aqui para ajudar com muito carinho.</p>
                    
                    <p>Para redefinir sua senha, clique no botão abaixo:</p>
                    
                    <p style='text-align: center; margin: 30px 0;'>
                        <a href='{linkRecuperacao}' class='button'>🔑 Redefinir Minha Senha</a>
                    </p>
                    
                    <div class='highlight'>
                        <p><strong>⏰ Importante:</strong> Este link expira em <strong>1 hora</strong> por questões de segurança.</p>
                    </div>
                    
                    <div class='security-info'>
                        <p><strong>🛡️ Para sua segurança:</strong></p>
                        <ul>
                            <li>Se você não solicitou esta recuperação, pode ignorar este email</li>
                            <li>Nunca compartilhe este link com outras pessoas</li>
                            <li>Crie uma senha forte com pelo menos 8 caracteres</li>
                        </ul>
                    </div>
                    
                    <p>Se você não conseguir clicar no botão, copie e cole este link no seu navegador:</p>
                    <p style='word-break: break-all; background-color: #E3F2FD; padding: 15px; border-radius: 10px; font-family: monospace;'>{linkRecuperacao}</p>
                    
                    <p>Se precisar de ajuda, entre em contato conosco:</p>
                    <p>📧 Email: suporte@casaespirita.org.br<br>
                       📱 WhatsApp: (11) 99999-9999</p>
                </div>
                
                <div class='footer'>
                    <p><em>&quot;Resgatamos vidas e fazemos acreditar que existe sempre um caminho melhor&quot;</em></p>
                    <p>Se você não solicitou esta recuperação, pode ignorar este email com tranquilidade.</p>
                    <p>© Casa Espírita Trabalhadores de Jesus</p>
                </div>
            </div>
        </body>
        </html>";
        }

        private string GerarCorpoEmailBoasVindasDentista(string nome)
        {
            return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ font-family: 'Poppins', Arial, sans-serif; margin: 0; padding: 20px; background: linear-gradient(135deg, #E3F2FD, #E8F5E8); }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 25px; box-shadow: 0 20px 60px rgba(0,0,0,0.15); overflow: hidden; }}
                .header {{ background: linear-gradient(135deg, #4A90E2, #66BB6A); color: white; text-align: center; padding: 40px 30px 30px; }}
                .header h1 {{ margin: 0; font-size: 2rem; }}
                .header .icon {{ font-size: 3rem; margin-bottom: 1rem; }}
                .content {{ padding: 40px 30px; }}
                .welcome-box {{ background: linear-gradient(135deg, #E3F2FD, #E8F5E8); padding: 25px; border-radius: 15px; margin: 25px 0; border-left: 5px solid #FFD700; }}
                .steps {{ background-color: #f8f9fa; padding: 20px; border-radius: 15px; margin: 20px 0; }}
                .step {{ padding: 10px 0; border-bottom: 1px solid #dee2e6; }}
                .step:last-child {{ border-bottom: none; }}
                .mission-box {{ background: linear-gradient(135deg, #fff5f5, #fffacd); padding: 20px; border-radius: 15px; border-left: 5px solid #FFD700; margin: 25px 0; font-style: italic; }}
                .contact-info {{ background-color: #E3F2FD; padding: 20px; border-radius: 15px; margin: 20px 0; }}
                .footer {{ padding: 30px; background: linear-gradient(135deg, #4A90E2, #66BB6A); text-align: center; font-size: 12px; color: white; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <div class='icon'>❤️</div>
                    <h1>Parabéns, {nome}!</h1>
                    <p>Você faz parte da nossa família agora</p>
                </div>
                
                <div class='content'>
                    <p style='font-size: 16px;'>Olá, <strong>{nome}</strong>!</p>
                    
                    <div class='welcome-box'>
                        <p style='margin: 0; font-size: 18px;'>
                            🎊 É com <strong>imensa alegria</strong> que damos as boas-vindas à <strong>Casa Espírita Trabalhadores de Jesus</strong>!
                        </p>
                    </div>
                    
                    <p>Sua candidatura foi <strong style='color: #66BB6A;'>aprovada com sucesso</strong> e agora você faz parte da nossa equipe de voluntários! 🎉</p>
                    
                    <div class='steps'>
                        <h3 style='color: #4A90E2; margin-top: 0;'>🔑 Próximos Passos:</h3>
                        <div class='step'>
                            <strong>1.</strong> Sua conta já está <strong style='color: #66BB6A;'>ativa</strong> no sistema
                        </div>
                        <div class='step'>
                            <strong>2.</strong> Acesse o portal com seu <strong>Email</strong> e senha <strong>CRO + 123</strong>
                        </div>
                         <div class='step'>
                            <strong>3.</strong> Acesse o seu perfil, altere sua senha. Use uma senha forte, combinando letras, números e símbolos, evitando informações pessoais e sequências fáceis. </strong>
                        </div>
                        <div class='step'>
                            <strong>4.</strong> Configure sua <strong>disponibilidade de horários</strong>
                        </div>
                        <div class='step'>
                            <strong>5.</strong> Comece a fazer a diferença na vida de nossas crianças! 💙
                        </div>
                    </div>
                    
                    <p style='text-align: center; margin: 30px 0;'>
                        <a href='https://localhost:7162/Auth/DentistaLogin?ReturnUrl=%2FDentista%2FDashboard' style='display: inline-block; padding: 15px 35px; background-color: #4A90E2; color: #FFFFFF; text-decoration: none; border-radius: 15px; font-weight: bold; font-size: 16px; box-shadow: 0 8px 25px rgba(74, 144, 226, 0.3);'>
                            🔐 Acessar Área do Dentista
                        </a>
                    </p>
                    
                    
                    <div class='mission-box'>
                        <p style='margin: 0; color: #495057;'>
                            <strong>💛 Nossa Missão:</strong><br>
                            ""Resgatamos vidas e fazemos acreditar que existe sempre um caminho melhor a ser seguido""
                        </ p >
                    </ div >


                    < p > Estamos < strong > muito felizes </ strong > em tê - lo(a) conosco nessa jornada de amor e solidariedade! ❤️</ p >


                    < div class='contact-info'>
                        <p style = 'margin: 0;' >< strong > Em caso de dúvidas, entre em contato:</strong></p>
                        <p style = 'margin: 10px 0 0 0;' >
                            📞 <strong>(11) 94155-6472</strong><br>
                            📍 Av.Professor Flávio Pires de Camargo, 56 - Caetetuba, Atibaia/SP
                        </p>
                    </div>
                    
                    <p style = 'text-align: center; margin-top: 30px;' >
                        Com gratidão,<br>
                        <strong style = 'color: #4A90E2;' > Equipe Casa Espírita Trabalhadores de Jesus</strong> 🙏
                    </p>
                </div>
                
                <div class='footer'>
                    <p style = 'margin: 0;' >© 2024 Casa Espírita Trabalhadores de Jesus</p>
                    <p style = 'margin: 5px 0 0 0;' > Todos os direitos reservados</p>
                </div>
            </div>
        </body>
        </html>";
        }
}
}