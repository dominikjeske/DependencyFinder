using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class ProjectRefViewModel : TreeViewItemViewModel
    {
        private readonly ProjectReference _projectReference;

        public ProjectRefViewModel(ProjectReference projectRef, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            _projectReference = projectRef;
            Name = _projectReference.Name;
        }
    }
}