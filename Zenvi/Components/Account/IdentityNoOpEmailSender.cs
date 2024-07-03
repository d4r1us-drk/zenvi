using Microsoft.AspNetCore.Identity;
using Zenvi.Data.Models;
using FluentEmail.Core;

namespace Zenvi.Components.Account;

internal sealed class IdentityNoOpEmailSender(IFluentEmail fluentEmail) : IEmailSender<ApplicationUser>
{
    private Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return fluentEmail
            .To(email)
            .Subject(subject)
            .Body(htmlMessage, true)
            .SendAsync();
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var subject = "Confirm your email";
        var message = $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.";
        return SendEmailAsync(email, subject, message);
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var subject = "Reset your password";
        var message = $"Please reset your password by <a href='{resetLink}'>clicking here</a>.";
        return SendEmailAsync(email, subject, message);
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var subject = "Reset your password";
        var message = $"Please reset your password using the following code: {resetCode}";
        return SendEmailAsync(email, subject, message);
    }
}