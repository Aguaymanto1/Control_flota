using Resend;

 

public interface IEmailService
{
    Task<bool> EnviarCorreoConConstanciaAsync(string destinatario, string asunto, string mensajeHtml, byte[] pdfAdjunto, string nombrePdf);
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
            
            // Configurar remitente
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "onboarding@resend.dev";
            var fromName = _configuration["EmailSettings:FromName"] ?? "Sistema de Transporte";
            message.From = $"{fromName} <{fromEmail}>";
            
            // En desarrollo, redirigir a email de testing si está configurado
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
            
            // Agregar destinatario
            message.To.Add(emailFinal);
            
            // Configurar asunto y cuerpo HTML con el mensaje de warning si aplica
            message.Subject = asunto;
            message.HtmlBody = mensajeOriginal + mensajeHtml;
            
            // Adjuntar el PDF
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
            
            // Enviar el correo
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
}