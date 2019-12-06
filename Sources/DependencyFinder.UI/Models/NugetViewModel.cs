using DependencyFinder.Core.Models;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{
    public class NugetViewModel : TreeViewItemViewModel
    {
        [ExpandableObject]
        public NugetPackage NugetPackage { get; }

        public NugetViewModel(NugetPackage nugetPackage, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            NugetPackage = nugetPackage;
            Name = $"{nugetPackage.Name} [{nugetPackage.Version}]";
            FullName = Name;
        }
    }
}