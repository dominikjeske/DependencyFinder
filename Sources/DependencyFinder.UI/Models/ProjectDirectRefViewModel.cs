using DependencyFinder.Core.Models;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{

    public class ProjectDirectRefViewModel : TreeViewItemViewModel
    {
        [ExpandableObject]
        public ProjectReference ProjectReference { get; }

        public ProjectDirectRefViewModel(ProjectReference projectRef, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            ProjectReference = projectRef;
            Name = ProjectReference.Name;
            FullName = ProjectReference.FilePath;
        }
    }
}