namespace GorgoDresses.Api.Auth;

public static class Policies
{
    public const string NotConfirmedEmail = "NotConfirmedEmail";
    public const string EmailConfirmed = "EmailConfirmed";
    public const string RegisteredUser = "RegisteredUser";
    public const string ExistingUser = "ExistingUser";
    public const string AdministratorUser = "AdministratorUser";
}
