using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Contact API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseRouting();

app.UseCors("AllowAll");

app.MapControllers();

app.Run();

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ContactController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] ContactRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email) || 
                string.IsNullOrEmpty(request.Message) || string.IsNullOrEmpty(request.Service))
            {
                return BadRequest(new { success = false, message = "Please fill in all required fields." });
            }

            // Email configuration from environment variables
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
            var smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? throw new Exception("SMTP_USERNAME not configured");
            var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? throw new Exception("SMTP_PASSWORD not configured");
            var toEmail = Environment.GetEnvironmentVariable("TO_EMAIL") ?? "aetherion925@gmail.com";

            // Create SMTP client
            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            // Send business notification email
            var businessEmail = new MailMessage
            {
                From = new MailAddress(smtpUsername, "Aetherion Contact Form"),
                Subject = $"New Contact Form Submission from {request.Name}",
                Body = CreateBusinessEmailTemplate(request),
                IsBodyHtml = true
            };
            businessEmail.To.Add(toEmail);
            businessEmail.ReplyToList.Add(new MailAddress(request.Email, request.Name));

            await smtpClient.SendMailAsync(businessEmail);

            // Send auto-response email
            var autoResponse = new MailMessage
            {
                From = new MailAddress(smtpUsername, "Aetherion Team"),
                Subject = "Thank you for contacting Aetherion - We'll be in touch soon!",
                Body = CreateAutoResponseTemplate(request),
                IsBodyHtml = true
            };
            autoResponse.To.Add(new MailAddress(request.Email, request.Name));

            await smtpClient.SendMailAsync(autoResponse);

            return Ok(new { success = true, message = "Thank you for your message! We'll get back to you within 24 hours to schedule your discovery call." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email error: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Sorry, there was an error sending your message. Please try again or contact us directly at contact@aetherion.com" });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    private string CreateBusinessEmailTemplate(ContactRequest request)
    {
        var timestamp = DateTime.Now.ToString("MMMM dd, yyyy h:mm tt");
        
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: 'Arial', sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: linear-gradient(135deg, #3b4476, #c9cff6); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                .field {{ margin-bottom: 20px; }}
                .label {{ font-weight: bold; color: #3b4476; margin-bottom: 5px; display: block; }}
                .value {{ background: white; padding: 15px; border-radius: 5px; border-left: 4px solid #3b4476; word-wrap: break-word; }}
                .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
                .logo {{ font-size: 24px; font-weight: bold; margin-bottom: 10px; }}
                .urgent {{ background: #e74c3c; color: white; padding: 10px; border-radius: 5px; margin-top: 20px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <div class='logo'>Aetherion</div>
                    <h2>New Contact Form Submission</h2>
                    <p>You have received a new inquiry through your website</p>
                </div>
                <div class='content'>
                    <div class='field'>
                        <span class='label'>Name:</span>
                        <div class='value'>{request.Name}</div>
                    </div>
                    <div class='field'>
                        <span class='label'>Email:</span>
                        <div class='value'><a href='mailto:{request.Email}'>{request.Email}</a></div>
                    </div>
                    <div class='field'>
                        <span class='label'>Company:</span>
                        <div class='value'>{request.Company ?? "Not specified"}</div>
                    </div>
                    <div class='field'>
                        <span class='label'>Service Interested:</span>
                        <div class='value'>{request.Service}</div>
                    </div>
                    <div class='field'>
                        <span class='label'>Message:</span>
                        <div class='value'>{request.Message}</div>
                    </div>
                    <div class='field'>
                        <span class='label'>Submitted:</span>
                        <div class='value'>{timestamp}</div>
                    </div>
                    <div class='urgent'>
                        <strong>Action Required:</strong> Please respond to this inquiry within 24 hours.
                    </div>
                </div>
                <div class='footer'>
                    <p>This email was sent from the Aetherion website contact form.</p>
                    <p>Reply directly to this email to respond to {request.Name}.</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string CreateAutoResponseTemplate(ContactRequest request)
    {
        var messagePreview = request.Message?.Length > 100 ? request.Message.Substring(0, 100) + "..." : request.Message;
        
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: 'Arial', sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: linear-gradient(135deg, #3b4476, #c9cff6); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
                .logo {{ font-size: 24px; font-weight: bold; margin-bottom: 10px; }}
                .highlight {{ color: #3b4476; font-weight: bold; }}
                .summary {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #3b4476; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <div class='logo'>Aetherion</div>
                    <h2>Thank You for Your Inquiry!</h2>
                </div>
                <div class='content'>
                    <p>Dear <span class='highlight'>{request.Name}</span>,</p>
                    <p>Thank you for your interest in Aetherion's services. We have received your inquiry regarding <span class='highlight'>{request.Service}</span> and will get back to you within 24 hours to schedule your discovery call.</p>
                    
                    <div class='summary'>
                        <h3>Summary of your submission:</h3>
                        <ul>
                            <li><strong>Service:</strong> {request.Service}</li>
                            <li><strong>Company:</strong> {request.Company ?? "Not specified"}</li>
                            <li><strong>Message:</strong> {messagePreview}</li>
                        </ul>
                    </div>
                    
                    <p>In the meantime, feel free to explore our portfolio and learn more about our expertise on our website.</p>
                    <p>We look forward to discussing how we can help bring your project to life!</p>
                    <p>Best regards,<br><strong>The Aetherion Team</strong></p>
                </div>
                <div class='footer'>
                    <p>This is an automated response. Please do not reply to this email.</p>
                    <p>For urgent matters, please call us at <strong>+1 (555) 123-4567</strong>.</p>
                    <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
                    <p style='font-size: 12px;'>© 2024 Aetherion. All rights reserved.</p>
                </div>
            </div>
        </body>
        </html>";
    }
}

public class ContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string Service { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}