using DependencyFinder.Core.Models;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{
    public class ProjectRefViewModel : TreeViewItemViewModel
    {
        [ExpandableObject]
        public ProjectReference ProjectReference { get; }

        public ProjectRefViewModel(ProjectReference projectRef, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            ProjectReference = projectRef;
            Name = ProjectReference.Name;
        }
    }
}