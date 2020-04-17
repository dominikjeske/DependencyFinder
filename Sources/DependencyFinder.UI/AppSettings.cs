using System.Collections.Generic;

namespace DependencyFinder.UI
{
    public class AppSettings
    {
        public string SolutionsLocation { get; set; }
        public List<string> SkipSolutions { get; set; }
    }
}