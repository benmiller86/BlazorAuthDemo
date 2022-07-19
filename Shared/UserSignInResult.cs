namespace BlazorAuthDemo.Shared;

public record UserSignInResult(bool Success, string? ErrorMessage = null, string? Token = null);
