namespace auth_template.Configuration;

public static class Regexes
{
    public const string Email = @"^[\w.-]+@([\w-]+\.)+[\w-]{2,}$";
    public const string SpecialCharacters = @"[!@#\$%\^&*(),.?""{}|<>]";
    public const string Uppercase = @"[A-Z]";
    public const string UppercaseLocalized = @"^[A-ZÁÉÍÓÖŐÚÜŰ\s-]+$";
    public const string Lowercase = @"[a-z]";
    public const string LowercaseLocalized = @"^[a-záéíóöőúüű\s-]+$";
    public const string LocalizedInvariant = @"^[a-zA-ZáéíóöőúüűÁÉÍÓÖŐÚÜŰ\s-]+$";
    public const string Digits = @"\d";
    public const string FirstLetter = @"^[a-zA-Z]";
    public const string AllowedUsernameCharacters = @"^[a-zA-Z0-9._]+$"; 
    public const string PasswordSpecialCharacters = @"[!@#\$%\^&*(),.?""{}|<>]";
    public const string PhoneNumber = @"^\+?[0-9]{7,15}$";
    public const string SortableInteger = @"\((\d+)\)";
}