using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace DependencyFinder.UI.Models
{
    public class ReferencesCollectionViewModel : TreeViewItemViewModel
    {
        public ReferencesCollectionViewModel(IEnumerable<ProjectReference> projects, TreeViewItemViewModel parent) : base(parent, false)
        {
            Name = $"References [{projects.Count()}]";
            HasPreview = false;

            foreach (var project in projects.OrderBy(x => x.Name))
            {
                var pvm = new ProjectDirectRefViewModel(project, this, false);

                Children.Add(pvm);
            }
        }
    }
}