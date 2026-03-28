namespace auth_template.Features.Email.Transfer;

public record ForgotPasswordDto(string token, string password, string encryptedUserId);
