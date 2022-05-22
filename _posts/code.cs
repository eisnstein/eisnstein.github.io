public static class ApiRoutes
{
    public static void Definitions(WebApplication app)
    {
        // Account Endpoints
        app.MapPost("/api/accounts/authenticate", AccountsEndpoint.Authenticate);
        app.MapPost("/api/accounts/register", AccountsEndpoint.Register);
        app.MapPost("/api/accounts/verify-email", AccountsEndpoint.VerifyEmail);
        app.MapPost("/api/accounts/forgot-password", AccountsEndpoint.ForgotPassword);
        app.MapPost("/api/accounts/validate-reset-token", AccountsEndpoint.ValidateResetToken);
        app.MapPost("/api/accounts/reset-password", AccountsEndpoint.ResetPassword);
        app.MapPost("/api/accounts/refresh-token", AccountsEndpoint.RefreshToken);

        app.MapPost("/api/accounts/revoke-token", AccountsEndpoint.RevokeToken).RequireAuthorization();
        app.MapGet("/api/accounts/{id:int}", AccountsEndpoint.GetById).RequireAuthorization();
        app.MapPut("/api/accounts/{id:int}", AccountsEndpoint.Update).RequireAuthorization();
    }
}