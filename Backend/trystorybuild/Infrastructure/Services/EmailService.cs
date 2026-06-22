using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    public Task SendTeacherWelcomeAsync(string toEmail, string teacherName, string password) =>
        SendAsync(toEmail,
            "🎉 تم إنشاء حسابك في منصة لغتي",
            $"""
            <div dir="rtl" style="font-family:Arial,sans-serif;color:#333;max-width:520px;margin:0 auto">
              <div style="background:linear-gradient(135deg,#F4788A,#F472B6);padding:28px 24px;border-radius:16px 16px 0 0;text-align:center">
                <h1 style="color:#fff;margin:0;font-size:26px">منصة لغتي 📚</h1>
                <p style="color:rgba(255,255,255,.9);margin:8px 0 0">تعليم اللغة العربية للأطفال</p>
              </div>
              <div style="background:#fff;padding:28px 24px;border-radius:0 0 16px 16px;border:1px solid #FCE4EC">
                <h2 style="color:#F4788A;margin:0 0 16px">أهلاً {teacherName}! 👋</h2>
                <p>تم إنشاء حسابك كمعلم في منصة لغتي بنجاح. يمكنك الآن تسجيل الدخول باستخدام:</p>
                <div style="background:#FFF0F3;border-radius:12px;padding:20px;margin:20px 0;border-right:4px solid #F4788A">
                  <p style="margin:0 0 8px"><strong>البريد الإلكتروني:</strong> {toEmail}</p>
                  <p style="margin:0"><strong>كلمة المرور:</strong>
                    <span style="font-size:18px;font-weight:bold;letter-spacing:3px;color:#F4788A">{password}</span>
                  </p>
                </div>
                <p style="color:#E57373"><strong>⚠️ يُرجى تغيير كلمة المرور فور تسجيل الدخول الأول.</strong></p>
                <p style="color:#888;font-size:13px;margin-top:24px">إذا لم تطلب هذا الحساب، تجاهل هذا البريد وتواصل مع مدير المدرسة.</p>
              </div>
            </div>
            """);

    public Task SendTeacherPasswordResetAsync(string toEmail, string teacherName, string newPassword) =>
        SendAsync(toEmail,
            "🔐 تم إعادة تعيين كلمة المرور — منصة لغتي",
            $"""
            <div dir="rtl" style="font-family:Arial,sans-serif;color:#333;max-width:520px;margin:0 auto">
              <div style="background:linear-gradient(135deg,#F4788A,#F472B6);padding:28px 24px;border-radius:16px 16px 0 0;text-align:center">
                <h1 style="color:#fff;margin:0;font-size:26px">منصة لغتي 📚</h1>
              </div>
              <div style="background:#fff;padding:28px 24px;border-radius:0 0 16px 16px;border:1px solid #FCE4EC">
                <h2 style="color:#F4788A;margin:0 0 16px">أخي/أختي {teacherName}</h2>
                <p>قام مدير المدرسة بإعادة تعيين كلمة المرور الخاصة بحسابك. كلمة المرور الجديدة:</p>
                <div style="background:#FFF0F3;border-radius:12px;padding:20px;margin:20px 0;text-align:center;border-right:4px solid #F4788A">
                  <span style="font-size:24px;font-weight:bold;letter-spacing:4px;color:#F4788A">{newPassword}</span>
                </div>
                <p style="color:#E57373"><strong>⚠️ يُرجى تغيير كلمة المرور فور تسجيل الدخول.</strong></p>
                <p style="color:#888;font-size:13px">إذا لم تطلب إعادة التعيين، تواصل مع مدير المدرسة فوراً.</p>
              </div>
            </div>
            """);

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var host     = config["Email:Host"]        ?? "smtp.gmail.com";
        var port     = int.Parse(config["Email:Port"] ?? "587");
        var username = config["Email:Username"]    ?? "";
        var password = config["Email:Password"]    ?? "";
        var fromAddr = config["Email:FromAddress"] ?? username;
        var fromName = config["Email:FromName"]    ?? "منصة لغتي";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("[EMAIL SKIPPED] Email not configured. Would send to {Email}: {Subject}", toEmail, subject);
            return;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials    = new NetworkCredential(username, password),
                EnableSsl      = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            using var message = new MailMessage
            {
                From        = new MailAddress(fromAddr, fromName),
                Subject     = subject,
                Body        = htmlBody,
                IsBodyHtml  = true
            };
            message.To.Add(new MailAddress(toEmail));

            await client.SendMailAsync(message);
            logger.LogInformation("[EMAIL SENT] To: {Email} | Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[EMAIL FAILED] To: {Email} | Subject: {Subject}", toEmail, subject);
            // Never throw — email failure must not break the request flow
        }
    }
}
