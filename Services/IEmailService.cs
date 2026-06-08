using Resend;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IEmailService
{
    Task<bool> EnviarCorreoConConstanciaAsync(string destinatario, string asunto, string mensajeHtml, byte[] pdfAdjunto, string nombrePdf);
    
    // 👇 NUEVO MÉTODO PARA ENVIAR CORREO SIN ADJUNTO
    Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string mensajeHtml);
}

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public EmailService(IResend resend, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _resend = resend;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<bool> EnviarCorreoConConstanciaAsync(string destinatario, string asunto, string mensajeHtml, byte[] pdfAdjunto, string nombrePdf)
    {
        try
        {
            var message = new EmailMessage();
            
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "onboarding@resend.dev";
            var fromName = _configuration["EmailSettings:FromName"] ?? "Sistema de Transporte";
            message.From = $"{fromName} <{fromEmail}>";
            
            var emailFinal = destinatario;
            var mensajeOriginal = "";
            
            if (_environment.IsDevelopment())
            {
                var testEmail = _configuration["EmailSettings:TestEmailAddress"];
                if (!string.IsNullOrWhiteSpace(testEmail) && !string.Equals(destinatario, testEmail, StringComparison.OrdinalIgnoreCase))
                {
                    mensajeOriginal = $"<p style='background:#fff3cd;padding:10px;margin-bottom:15px;border:1px solid #ffc107;border-radius:4px;'><strong>⚠️ En desarrollo:</strong> Este correo se está enviando a <strong>{testEmail}</strong> por restricciones de testing. Destinatario original: <strong>{destinatario}</strong></p>";
                    emailFinal = testEmail;
                }
            }
            
            message.To.Add(emailFinal);
            message.Subject = asunto;
            message.HtmlBody = mensajeOriginal + mensajeHtml;
            
            var pdfBase64 = Convert.ToBase64String(pdfAdjunto);
            
            message.Attachments = new List<EmailAttachment>
            {
                new EmailAttachment
                {
                    Filename = nombrePdf,
                    Content = pdfBase64,
                    ContentType = "application/pdf"
                }
            };
            
            var result = await _resend.EmailSendAsync(message);
            
            Console.WriteLine($"✅ Correo enviado con ID: {result}");
            if (!string.IsNullOrWhiteSpace(mensajeOriginal))
                Console.WriteLine($"ℹ️ Redirección en desarrollo a: {emailFinal}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error enviando correo con Resend: {ex.Message}");
            return false;
        }
    }

    // 👇 NUEVO MÉTODO PARA ENVIAR CORREO SIN ADJUNTO (SOLO TEXTO)
    public async Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string mensajeHtml)
    {
        try
        {
            var message = new EmailMessage();
            
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "onboarding@resend.dev";
            var fromName = _configuration["EmailSettings:FromName"] ?? "Sistema de Transporte";
            message.From = $"{fromName} <{fromEmail}>";
            
            var emailFinal = destinatario;
            var mensajeOriginal = "";
            
            if (_environment.IsDevelopment())
            {
                var testEmail = _configuration["EmailSettings:TestEmailAddress"];
                if (!string.IsNullOrWhiteSpace(testEmail) && !string.Equals(destinatario, testEmail, StringComparison.OrdinalIgnoreCase))
                {
                    mensajeOriginal = $"<p style='background:#fff3cd;padding:10px;margin-bottom:15px;border:1px solid #ffc107;border-radius:4px;'><strong>⚠️ En desarrollo:</strong> Este correo se está enviando a <strong>{testEmail}</strong> por restricciones de testing. Destinatario original: <strong>{destinatario}</strong></p>";
                    emailFinal = testEmail;
                }
            }
            
            message.To.Add(emailFinal);
            message.Subject = asunto;
            message.HtmlBody = mensajeOriginal + mensajeHtml;
            
            var result = await _resend.EmailSendAsync(message);
            
            Console.WriteLine($"✅ Correo enviado con ID: {result}");
            if (!string.IsNullOrWhiteSpace(mensajeOriginal))
                Console.WriteLine($"ℹ️ Redirección en desarrollo a: {emailFinal}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error enviando correo: {ex.Message}");
            return false;
        }
    }
}