using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Zenvi.Core.Data.Context;
using Zenvi.Core.Data.Entities;
using Zenvi.Core.Utils;
using Zenvi.Shared;

namespace Zenvi.Core.Identity;

// This is a custom implementation of the default IdentityApiEndpointRouteBuilderExtensions available at:
// https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs
// This was needed for my custom user class with extra data, at this point there's no support for a custom IdentityUser
// class, so I needed to grab the original source code and modify it. Specifically the register and info endpoints.
public static class AccountApiMapper
{
    private static readonly LogHandler LogHandler = new(typeof(AccountApiMapper));

    private static readonly EmailAddressAttribute EmailAddressAttribute = new();

    private static readonly HashSet<string> AcceptablePictureMediaTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif"
    ];

    public static IEndpointConventionBuilder MapAccountApi(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var timeProvider = endpoints.ServiceProvider.GetRequiredService<TimeProvider>();
        var bearerTokenOptions = endpoints.ServiceProvider.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>();
        var emailSender = endpoints.ServiceProvider.GetRequiredService<IEmailSender<User>>();
        var linkGenerator = endpoints.ServiceProvider.GetRequiredService<LinkGenerator>();

        string? confirmEmailEndpointName = null;

        var routeGroup = endpoints.MapGroup("/identity");

        routeGroup.MapPost("/register", async Task<Results<Ok, ValidationProblem>>
            ([FromBody] RegisterUserDto registration, HttpContext context, [FromServices] IServiceProvider sp) =>
        {
            LogHandler.LogInfo("Registering a new user.");

            var userManager = sp.GetRequiredService<UserManager<User>>();

            if (!userManager.SupportsUserEmail)
            {
                LogHandler.LogError("Email support is required.", new NotSupportedException());
                throw new NotSupportedException($"{nameof(MapAccountApi)} requires a user store with email support.");
            }

            var userStore = sp.GetRequiredService<IUserStore<User>>();
            var emailStore = (IUserEmailStore<User>)userStore;
            var email = registration.Email;

            if (string.IsNullOrEmpty(email) || !EmailAddressAttribute.IsValid(email))
            {
                LogHandler.LogError("Invalid email.", new ArgumentException("Invalid email."));
                return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidEmail(email)));
            }

            var user = new User
            {
                Email = registration.Email,
                Name = registration.Name,
                Surname = registration.Surname,
                UserName = registration.Email
            };

            await userStore.SetUserNameAsync(user, email, CancellationToken.None);
            await emailStore.SetEmailAsync(user, email, CancellationToken.None);
            var result = await userManager.CreateAsync(user, registration.Password);

            if (!result.Succeeded)
            {
                LogHandler.LogError("User registration failed.", new InvalidOperationException());
                return CreateValidationProblem(result);
            }

            await SendConfirmationEmailAsync(user, userManager, context, email);
            LogHandler.LogInfo("User registered successfully.");

            return TypedResults.Ok();
        });

        routeGroup.MapPost("/login", async Task<Results<Ok<AccessTokenResponse>, EmptyHttpResult, ProblemHttpResult>>
        ([FromBody] LoginUserDto login, [FromQuery] bool? useCookies, [FromQuery] bool? useSessionCookies,
            [FromServices] IServiceProvider sp) =>
        {
            LogHandler.LogInfo("User login attempt.");

            var signInManager = sp.GetRequiredService<SignInManager<User>>();
            var userManager = sp.GetRequiredService<UserManager<User>>();

            var user = await userManager.FindByEmailAsync(login.Email);
            if (user == null)
            {
                LogHandler.LogError("User login failed. User not found.", new UnauthorizedAccessException());
                return TypedResults.Problem("Invalid login attempt.", statusCode: StatusCodes.Status401Unauthorized);
            }

            if (user.Banned)
            {
                LogHandler.LogError("User login failed. User is banned.", new UnauthorizedAccessException());
                return TypedResults.Problem("Your account has been banned.",
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var useCookieScheme = useCookies == true || useSessionCookies == true;
            var isPersistent = useCookies == true && useSessionCookies != true;
            signInManager.AuthenticationScheme =
                useCookieScheme ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;

            var result =
                await signInManager.PasswordSignInAsync(login.Email, login.Password, isPersistent,
                    lockoutOnFailure: true);

            if (result.RequiresTwoFactor)
            {
                if (!string.IsNullOrEmpty(login.TwoFactorCode))
                {
                    result = await signInManager.TwoFactorAuthenticatorSignInAsync(login.TwoFactorCode, isPersistent,
                        rememberClient: isPersistent);
                }
                else if (!string.IsNullOrEmpty(login.TwoFactorRecoveryCode))
                {
                    result = await signInManager.TwoFactorRecoveryCodeSignInAsync(login.TwoFactorRecoveryCode);
                }
            }

            if (!result.Succeeded)
            {
                LogHandler.LogError("User login failed.", new UnauthorizedAccessException());
                return TypedResults.Problem("Invalid login attempt.", statusCode: StatusCodes.Status401Unauthorized);
            }

            LogHandler.LogInfo("User logged in successfully.");
            return TypedResults.Empty;
        });

        // Provide an end point to clear the cookie for logout
        //
        // For more information on the logout endpoint and antiforgery, see:
        // https://learn.microsoft.com/aspnet/core/blazor/security/webassembly/standalone-with-identity#antiforgery-support
        routeGroup.MapPost("/logout", async (SignInManager<User> signInManager, [FromBody] object empty) =>
        {
            if (empty is not null)
            {
                await signInManager.SignOutAsync();

                return Results.Ok();
            }

            return Results.Unauthorized();
        })
        .RequireAuthorization();

        routeGroup.MapPost("/refresh",
            async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult, SignInHttpResult, ChallengeHttpResult>>
                ([FromBody] RefreshUserDto refreshRequest, [FromServices] IServiceProvider sp) =>
        {
            LogHandler.LogInfo("Refreshing user token.");

            var signInManager = sp.GetRequiredService<SignInManager<User>>();
            var refreshTokenProtector =
                bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
            var refreshTicket = refreshTokenProtector.Unprotect(refreshRequest.RefreshToken);

            if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc ||
                timeProvider.GetUtcNow() >= expiresUtc ||
                await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not { } user)
            {
                LogHandler.LogError("Token refresh failed.", new UnauthorizedAccessException());
                return TypedResults.Challenge();
            }

            var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
            LogHandler.LogInfo("Token refreshed successfully.");

            return TypedResults.SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
        });

        routeGroup.MapGet("/confirmEmail", async Task<Results<ContentHttpResult, UnauthorizedHttpResult>>
            ([FromQuery] string userId, [FromQuery] string code, [FromQuery] string? changedEmail,
                [FromServices] IServiceProvider sp) =>
        {
            LogHandler.LogInfo("Email confirmation attempt.");

            var userManager = sp.GetRequiredService<UserManager<User>>();
            if (await userManager.FindByIdAsync(userId) is not { } user)
            {
                LogHandler.LogError("User not found for email confirmation.", new KeyNotFoundException());
                return TypedResults.Unauthorized();
            }

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch (FormatException)
            {
                LogHandler.LogError("Invalid email confirmation code format.", new FormatException());
                return TypedResults.Unauthorized();
            }

            IdentityResult result;

            if (string.IsNullOrEmpty(changedEmail))
            {
                result = await userManager.ConfirmEmailAsync(user, code);
            }
            else
            {
                result = await userManager.ChangeEmailAsync(user, changedEmail, code);

                if (result.Succeeded)
                {
                    result = await userManager.SetUserNameAsync(user, changedEmail);
                }
            }

            if (!result.Succeeded)
            {
                LogHandler.LogError("Email confirmation failed.", new InvalidOperationException());
                return TypedResults.Unauthorized();
            }

            LogHandler.LogInfo("Email confirmed successfully.");
            return TypedResults.Text("Thank you for confirming your email.");
        })
        .Add(endpointBuilder =>
        {
            var finalPattern = ((RouteEndpointBuilder)endpointBuilder).RoutePattern.RawText;
            confirmEmailEndpointName = $"{nameof(MapAccountApi)}-{finalPattern}";
            endpointBuilder.Metadata.Add(new EndpointNameMetadata(confirmEmailEndpointName));
        });

        routeGroup.MapPost("/resendConfirmationEmail", async Task<Ok>
            ([FromBody] ResendConfirmationEmailUserDto resendRequest, HttpContext context,
                [FromServices] IServiceProvider sp) =>
        {
            LogHandler.LogInfo("Resending confirmation email.");

            var userManager = sp.GetRequiredService<UserManager<User>>();
            if (await userManager.FindByEmailAsync(resendRequest.Email) is not { } user)
            {
                return TypedResults.Ok();
            }

            await SendConfirmationEmailAsync(user, userManager, context, resendRequest.Email);
            LogHandler.LogInfo("Confirmation email resent successfully.");

            return TypedResults.Ok();
        });

        routeGroup.MapPost("/forgotPassword", async Task<Results<Ok, ValidationProblem>>
            ([FromBody] ForgotPasswordUserDto resetRequest, [FromServices] IServiceProvider sp) =>
        {
            LogHandler.LogInfo("Password reset request received.");

            var userManager = sp.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(resetRequest.Email);

            if (user is not null && await userManager.IsEmailConfirmedAsync(user))
            {
                var code = await userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                await emailSender.SendPasswordResetCodeAsync(user, resetRequest.Email,
                    HtmlEncoder.Default.Encode(code));
            }

            LogHandler.LogInfo("Password reset process initiated.");
            return TypedResults.Ok();
        });

        routeGroup.MapPost("/resetPassword", async Task<Results<Ok, ValidationProblem>>
            ([FromBody] ResetPasswordUserDto resetRequest, [FromServices] IServiceProvider sp) =>
        {
            LogHandler.LogInfo("Resetting password.");

            var userManager = sp.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(resetRequest.Email);

            if (user is null || !await userManager.IsEmailConfirmedAsync(user))
            {
                LogHandler.LogError("Invalid password reset token.", new InvalidOperationException());
                return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken()));
            }

            IdentityResult result;
            try
            {
                var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetRequest.ResetCode));
                result = await userManager.ResetPasswordAsync(user, code, resetRequest.NewPassword);
            }
            catch (FormatException)
            {
                LogHandler.LogError("Invalid password reset code format.", new FormatException());
                result = IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken());
            }

            if (!result.Succeeded)
            {
                LogHandler.LogError("Password reset failed.", new InvalidOperationException());
                return CreateValidationProblem(result);
            }

            LogHandler.LogInfo("Password reset successfully.");
            return TypedResults.Ok();
        });

        var accountGroup = routeGroup.MapGroup("/manage").RequireAuthorization();

        accountGroup.MapPost("/2fa", async Task<Results<Ok<TwoFactorResponse>, ValidationProblem, NotFound>>
            (ClaimsPrincipal claimsPrincipal, [FromBody] TwoFactorUserDto tfaRequest,
                [FromServices] IServiceProvider sp) =>
        {
            LogHandler.LogInfo("Two-factor authentication setup attempt.");

            var signInManager = sp.GetRequiredService<SignInManager<User>>();
            var userManager = signInManager.UserManager;
            if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            {
                LogHandler.LogError("User not found for 2FA setup.", new KeyNotFoundException());
                return TypedResults.NotFound();
            }

            if (tfaRequest.Enable == true)
            {
                if (tfaRequest.ResetSharedKey)
                {
                    LogHandler.LogError("Resetting shared key and enabling 2FA is not allowed.",
                        new InvalidOperationException());
                    return CreateValidationProblem("CannotResetSharedKeyAndEnable",
                        "Resetting the 2fa shared key must disable 2fa until a 2fa token based on the new shared key is validated.");
                }
                else if (string.IsNullOrEmpty(tfaRequest.TwoFactorCode))
                {
                    LogHandler.LogError("2FA token not provided.", new ArgumentException());
                    return CreateValidationProblem("RequiresTwoFactor",
                        "No 2fa token was provided by the request. A valid 2fa token is required to enable 2fa.");
                }
                else if (!await userManager.VerifyTwoFactorTokenAsync(user,
                             userManager.Options.Tokens.AuthenticatorTokenProvider, tfaRequest.TwoFactorCode))
                {
                    LogHandler.LogError("Invalid 2FA token.", new InvalidOperationException());
                    return CreateValidationProblem("InvalidTwoFactorCode",
                        "The 2fa token provided by the request was invalid. A valid 2fa token is required to enable 2fa.");
                }

                await userManager.SetTwoFactorEnabledAsync(user, true);
            }
            else if (tfaRequest.Enable == false || tfaRequest.ResetSharedKey)
            {
                await userManager.SetTwoFactorEnabledAsync(user, false);
            }

            if (tfaRequest.ResetSharedKey)
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
            }

            string[]? recoveryCodes = null;
            if (tfaRequest.ResetRecoveryCodes ||
                tfaRequest.Enable == true && await userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodesEnumerable = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                recoveryCodes = recoveryCodesEnumerable?.ToArray();
            }

            if (tfaRequest.ForgetMachine)
            {
                await signInManager.ForgetTwoFactorClientAsync();
            }

            var key = await userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
                key = await userManager.GetAuthenticatorKeyAsync(user);

                if (string.IsNullOrEmpty(key))
                {
                    LogHandler.LogError("Failed to generate authenticator key.", new NotSupportedException());
                    throw new NotSupportedException("The user manager must produce an authenticator key after reset.");
                }
            }

            LogHandler.LogInfo("2FA setup completed successfully.");
            return TypedResults.Ok(new TwoFactorResponse
            {
                SharedKey = key,
                RecoveryCodes = recoveryCodes,
                RecoveryCodesLeft = recoveryCodes?.Length ?? await userManager.CountRecoveryCodesAsync(user),
                IsTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user),
                IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user),
            });
        });

        accountGroup.MapGet("/info", async Task<Results<Ok<AboutUserDto>, ValidationProblem, NotFound>>
            (ClaimsPrincipal claimsPrincipal, [FromServices] IServiceProvider sp) =>
        {
            LogHandler.LogInfo("Fetching user info.");

            var userManager = sp.GetRequiredService<UserManager<User>>();
            if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            {
                LogHandler.LogError("User not found.", new KeyNotFoundException());
                return TypedResults.NotFound();
            }

            LogHandler.LogInfo("User info retrieved successfully.");
            return TypedResults.Ok(await CreateInfoResponseAsync(user, userManager));
        });

        accountGroup.MapPost("/update", async Task<Results<Ok<AboutUserDto>, ValidationProblem, NotFound>>
            (ClaimsPrincipal claimsPrincipal, [FromForm] UpdateUserDto updateRequest, HttpContext context,
                [FromServices] IServiceProvider sp, [FromServices] ApplicationDbContext dbContext) =>
        {
            LogHandler.LogInfo("Updating user info.");

            var userManager = sp.GetRequiredService<UserManager<User>>();

            if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            {
                LogHandler.LogError("User not found.", new KeyNotFoundException());
                return TypedResults.NotFound();
            }

            if (!string.IsNullOrEmpty(updateRequest.NewEmail) && !EmailAddressAttribute.IsValid(updateRequest.NewEmail))
            {
                LogHandler.LogError("Invalid email address.", new ArgumentException());
                return CreateValidationProblem(
                    IdentityResult.Failed(userManager.ErrorDescriber.InvalidEmail(updateRequest.NewEmail)));
            }

            if (!string.IsNullOrEmpty(updateRequest.NewPassword))
            {
                if (string.IsNullOrEmpty(updateRequest.OldPassword))
                {
                    LogHandler.LogError("Old password required to set a new password.", new ArgumentException());
                    return CreateValidationProblem("OldPasswordRequired",
                        "The old password is required to set a new password. If the old password is forgotten, use /resetPassword.");
                }

                var changePasswordResult =
                    await userManager.ChangePasswordAsync(user, updateRequest.OldPassword, updateRequest.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    LogHandler.LogError("Password change failed.", new InvalidOperationException());
                    return CreateValidationProblem(changePasswordResult);
                }
            }

            if (!string.IsNullOrEmpty(updateRequest.Name))
            {
                user.Name = updateRequest.Name;
            }

            if (!string.IsNullOrEmpty(updateRequest.Surname))
            {
                user.Surname = updateRequest.Surname;
            }

            if (!string.IsNullOrEmpty(updateRequest.Bio))
            {
                user.Bio = updateRequest.Bio;
            }

            if (!string.IsNullOrEmpty(updateRequest.ProfilePictureName))
            {
                var profilePicture = await dbContext.Media.FindAsync(updateRequest.ProfilePictureName);

                if (profilePicture != null)
                {
                    if (AcceptablePictureMediaTypes.Contains(profilePicture.Type))
                    {
                        user.ProfilePicture = profilePicture;
                    }
                    else
                    {
                        LogHandler.LogError("Invalid media format for profile picture.",
                            new InvalidOperationException());
                        return CreateValidationProblem("InvalidMediaType", "The provided media format isn't a picture");
                    }
                }
                else
                {
                    LogHandler.LogError("Profile picture not found.", new KeyNotFoundException());
                    return CreateValidationProblem("ProfilePictureNotFound",
                        "The provided profile picture file name was not found");
                }
            }

            if (!string.IsNullOrEmpty(updateRequest.BannerPictureName))
            {
                var bannerPicture = await dbContext.Media.FindAsync(updateRequest.BannerPictureName);

                if (bannerPicture != null)
                {
                    if (AcceptablePictureMediaTypes.Contains(bannerPicture.Type))
                    {
                        user.BannerPicture = bannerPicture;
                    }
                    else
                    {
                        LogHandler.LogError("Invalid media format for banner picture.",
                            new InvalidOperationException());
                        return CreateValidationProblem("InvalidMediaType",
                            "The provided media format isn't a banner picture");
                    }
                }
                else
                {
                    LogHandler.LogError("Banner picture not found.", new KeyNotFoundException());
                    return CreateValidationProblem("BannerPictureNotFound",
                        "The provided banner picture file name was not found");
                }
            }

            if (!string.IsNullOrEmpty(updateRequest.DateOfBirth))
            {
                if (DateOnly.TryParse(updateRequest.DateOfBirth, out var dateOfBirth))
                {
                    user.DateOfBirth = dateOfBirth;
                }
                else
                {
                    LogHandler.LogError("Invalid date of birth format.", new FormatException());
                    return CreateValidationProblem("InvalidDateOfBirth", "The DateOfBirth is not valid.");
                }
            }

            if (!string.IsNullOrEmpty(updateRequest.NewEmail))
            {
                var email = await userManager.GetEmailAsync(user);

                if (email != updateRequest.NewEmail)
                {
                    await SendConfirmationEmailAsync(user, userManager, context, updateRequest.NewEmail,
                        isChange: true);
                }
            }

            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                LogHandler.LogInfo("User info updated successfully.");
                return TypedResults.Ok(await CreateInfoResponseAsync(user, userManager));
            }

            LogHandler.LogError("User info update failed.", new InvalidOperationException());
            return CreateValidationProblem(result);
        });

        async Task SendConfirmationEmailAsync(User user, UserManager<User> userManager, HttpContext context,
            string email, bool isChange = false)
        {
            if (confirmEmailEndpointName is null)
            {
                LogHandler.LogError("Email confirmation endpoint not registered.", new NotSupportedException());
                throw new NotSupportedException("No email confirmation endpoint was registered!");
            }

            var code = isChange
                ? await userManager.GenerateChangeEmailTokenAsync(user, email)
                : await userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var userId = await userManager.GetUserIdAsync(user);
            var routeValues = new RouteValueDictionary
            {
                ["userId"] = userId,
                ["code"] = code
            };

            if (isChange)
            {
                routeValues.Add("changedEmail", email);
            }

            var confirmEmailUrl = linkGenerator.GetUriByName(context, confirmEmailEndpointName, routeValues)
                                  ?? throw new NotSupportedException(
                                      $"Could not find endpoint named '{confirmEmailEndpointName}'.");

            await emailSender.SendConfirmationLinkAsync(user, email, HtmlEncoder.Default.Encode(confirmEmailUrl));
            LogHandler.LogInfo("Confirmation email sent successfully.");
        }

        return new IdentityEndpointsConventionBuilder(routeGroup);
    }

    private static ValidationProblem CreateValidationProblem(string errorCode, string errorDescription) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            { errorCode, [errorDescription] }
        });

    private static ValidationProblem CreateValidationProblem(IdentityResult result)
    {
        Debug.Assert(!result.Succeeded);
        var errorDictionary = new Dictionary<string, string[]>(1);

        foreach (var error in result.Errors)
        {
            string[] newDescriptions;

            if (errorDictionary.TryGetValue(error.Code, out var descriptions))
            {
                newDescriptions = new string[descriptions.Length + 1];
                Array.Copy(descriptions, newDescriptions, descriptions.Length);
                newDescriptions[descriptions.Length] = error.Description;
            }
            else
            {
                newDescriptions = [error.Description];
            }

            errorDictionary[error.Code] = newDescriptions;
        }

        return TypedResults.ValidationProblem(errorDictionary);
    }

    private static async Task<AboutUserDto> CreateInfoResponseAsync(User user, UserManager<User> userManager)
    {
        return new AboutUserDto
        {
            Email = await userManager.GetEmailAsync(user) ??
                    throw new NotSupportedException("Users must have an email."),
            IsEmailConfirmed = await userManager.IsEmailConfirmedAsync(user),
            Name = user.Name,
            Surname = user.Surname,
            Bio = user.Bio,
            DateOfBirth = user.DateOfBirth,
            Banned = user.Banned,
            ProfilePictureName = user.ProfilePicture?.Name,
            BannerPictureName = user.BannerPicture?.Name
        };
    }

    private sealed class IdentityEndpointsConventionBuilder(RouteGroupBuilder inner) : IEndpointConventionBuilder
    {
        private IEndpointConventionBuilder InnerAsConventionBuilder => inner;

        public void Add(Action<EndpointBuilder> convention) => InnerAsConventionBuilder.Add(convention);

        public void Finally(Action<EndpointBuilder> finallyConvention) =>
            InnerAsConventionBuilder.Finally(finallyConvention);
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromBodyAttribute : Attribute, IFromBodyMetadata;

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromServicesAttribute : Attribute, IFromServiceMetadata;

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromQueryAttribute : Attribute, IFromQueryMetadata
    {
        public string? Name => null;
    }
}