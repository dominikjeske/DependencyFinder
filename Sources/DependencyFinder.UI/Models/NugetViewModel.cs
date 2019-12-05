using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class NugetViewModel : TreeViewItemViewModel
    {
        private readonly NugetPackage _nugetPackage;

        public NugetViewModel(NugetPackage nugetPackage, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            _nugetPackage = nugetPackage;
            Name = $"{nugetPackage.Name} [{nugetPackage.Version}]";
            FullName = Name;
        }
    }
}