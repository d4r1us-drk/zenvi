using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;
using Zenvi.Core.Data.Entities;

namespace Zenvi.Core.Identity;

public class AccountEmailSender(IFluentEmail fluentEmail) : IEmailSender<User>
{
    private Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return fluentEmail
            .To(email)
            .Subject(subject)
            .Body(htmlMessage, true)
            .SendAsync();
    }

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        var subject = "Confirm your email";
        var message = $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.";
        return SendEmailAsync(email, subject, message);
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        var subject = "Reset your password";
        var message = $"Please reset your password by <a href='{resetLink}'>clicking here</a>.";
        return SendEmailAsync(email, subject, message);
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        var subject = "Reset your password";
        var message = $"Please reset your password using the following code: {resetCode}";
        return SendEmailAsync(email, subject, message);
    }
}