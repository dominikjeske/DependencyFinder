using DependencyFinder.Core;
using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace DependencyFinder.UI.Models
{
    public class NugetCollectionViewModel : TreeViewItemViewModel
    {
        public NugetCollectionViewModel(IEnumerable<NugetPackage> nugets, TreeViewItemViewModel parent, ISolutionManager solutionManager) : base(parent, false)
        {
            Name = $"Nugets [{nugets.Count()}]";
            HasPreview = false;

            foreach (var nuget in nugets.OrderBy(x => x.Name))
            {
                var pvm = new NugetViewModel(nuget, this, solutionManager);

                Children.Add(pvm);
            }
        }
    }
}