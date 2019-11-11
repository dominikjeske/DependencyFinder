using Caliburn.Micro;

namespace DependencyFinder.UI.ViewModels
{
    public class ShellViewModel : Screen
    {
        public ShellViewModel()
        {
            SolutionsRoot = @"E:\Projects\Dependency\DependencyFinder\Test";
        }

         public string SolutionsRoot { get; set; }

    }
}