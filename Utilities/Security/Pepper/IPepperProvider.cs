namespace auth_template.Utilities.Security.Pepper;

public interface IPepperProvider
{
    byte[] GetPepper(int version);
    int GetCurrentVersion();
}