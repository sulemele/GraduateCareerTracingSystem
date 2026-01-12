using Microsoft.AspNetCore.Identity;

namespace WebUI.Models
{
    public class Enforce2FAMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly List<string> _allowedPaths = new()
        {
            "/Identity/Account/Manage/EnableAuthenticator",
            "/Identity/Account/Manage/TwoFactorAuthentication",
            "/Identity/Account/Manage/ResetAuthenticator",
            "/Identity/Account/Manage/GenerateRecoveryCodes",
            "/Identity/Account/Logout",
            "/Identity/Account/Login",
            "/Identity/Account/Register",
            "/Identity/Account/ForgotPassword",
            "/Identity/Account/ResetPassword",
            "/Identity/Account/AccessDenied",
            "/Identity/Account/ExternalLogin",
            "/Identity/Account/ConfirmEmail"
        };

        public Enforce2FAMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<IdentityUser> userManager)
        {
            // Skip middleware for non-authenticated users and API requests
            if (!context.User.Identity?.IsAuthenticated == true ||
                context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var user = await userManager.GetUserAsync(context.User);

            if (user != null && !await userManager.GetTwoFactorEnabledAsync(user))
            {
                var currentPath = context.Request.Path.Value;

                // Check if current path is allowed
                var isAllowedPath = _allowedPaths.Any(path =>
                    currentPath.StartsWith(path, StringComparison.OrdinalIgnoreCase));

                if (!isAllowedPath)
                {
                    // Redirect to 2FA setup page
                    context.Response.Redirect("/Identity/Account/Manage/TwoFactorAuthentication");
                    return;
                }
            }

            await _next(context);
        }
    }

}
