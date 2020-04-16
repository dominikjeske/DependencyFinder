using DependencyFinder.Core;
using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{

    public class NugetVersionGroupViewModel : TreeViewItemViewModel
    {
        public NugetVersionGroupViewModel(string name, TreeViewItemViewModel parent, IEnumerable<NugetProjectMap> projects) : base(parent, false)
        {
            Name = $"{name} [{projects.Count()}]";
            FullName = name;

            foreach (var project in projects.OrderBy(x => x.Project.Name))
            {
                Children.Add(new NugetReferenceViewModel(project, this));
            }
        }
    }
}