namespace auth_template.Validation;

public static class ValidationRules
{
    public static bool BeAValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2;
    }
    
}