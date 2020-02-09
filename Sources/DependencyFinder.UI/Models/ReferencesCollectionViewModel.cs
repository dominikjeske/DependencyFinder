using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace DependencyFinder.UI.Models
{
    public class ReferencesCollectionViewModel : TreeViewItemViewModel
    {
        public ReferencesCollectionViewModel(IEnumerable<ProjectReference> projects, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            Name = $"References [{projects.Count()}]";
            HasPreview = false;

            foreach (var project in projects)
            {
                var pvm = new ProjectRefViewModel(project, this, false);

                Children.Add(pvm);
            }
        }
    }
}