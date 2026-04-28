namespace auth_template.Utilities.Extensions;

public static class EnumExtensions
{
    extension<T>(T value) where T : Enum
    {
        public List<string> GetFlagNames()
        {
            return Enum.GetValues(typeof(T)).Cast<T>().Where(x => value.HasFlag(x) && Convert.ToInt64(x) != 0)
                .Select(x => x.ToString()).ToList();
        }

        public string GetFlagNamesAsString()
        {
            return string.Join(";", value.GetFlagNames());
        }
    }
}