namespace DependencyFinder
{
    public static class StringExtensions
    {
        public static bool IsSystemNuget(this string nuget)
        {
            return nuget.IndexOf("System", System.StringComparison.InvariantCultureIgnoreCase) == 0 ||
                   nuget.IndexOf("Microsoft", System.StringComparison.InvariantCultureIgnoreCase) == 0;
        }
    }
}