using Resend;

 

public interface IEmailService
{
    Task<bool> EnviarCorreoConConstanciaAsync(string destinatario, string asunto, string mensajeHtml, byte[] pdfAdjunto, string nombrePdf);
}

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _configuration;

    public EmailService(IResend resend, IConfiguration configuration)
    {
        _resend = resend;
        _configuration = configuration;
    }

    public async Task<bool> EnviarCorreoConConstanciaAsync(string destinatario, string asunto, string mensajeHtml, byte[] pdfAdjunto, string nombrePdf)
    {
        try
        {
            var message = new EmailMessage();
            
            // Configurar remitente (puedes ponerlo en appsettings.json)
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "onboarding@resend.dev";
            var fromName = _configuration["EmailSettings:FromName"] ?? "Sistema de Transporte";
            message.From = $"{fromName} <{fromEmail}>";
            
            // Agregar destinatario
            message.To.Add(destinatario);
            
            // Configurar asunto y cuerpo HTML
            message.Subject = asunto;
            message.HtmlBody = mensajeHtml;
            
            // Adjuntar el PDF (Resend maneja attachments de forma diferente)
            // Convertir el byte array a Base64 para el attachment
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
            
            // Si llegamos aquí, el envío fue exitoso
            Console.WriteLine($"Correo enviado con ID: {result}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando correo con Resend: {ex.Message}");
            return false;
        }
    }
}