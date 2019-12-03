using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DependencyFinder.UI.Models
{

    public class NugetCollectionViewModel : TreeViewItemViewModel
    {
        public NugetCollectionViewModel(IEnumerable<NugetPackage> nugets, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            Name = $"Nugets [{nugets.Count()}]";

            foreach(var nuget in nugets)
            {
                var pvm = new NugetViewModel(nuget, this, false);

                Children.Add(pvm);
            }
        }
    }
}
