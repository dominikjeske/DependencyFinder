using DependencyFinder.Core;
using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{
    public class NugetViewModel : TreeViewItemViewModel
    {
        [ExpandableObject]
        public NugetPackage NugetPackage { get; }

        private readonly ISolutionManager _solutionManager;

        public NugetViewModel(NugetPackage nugetPackage, TreeViewItemViewModel parent, ISolutionManager solutionManager) : base(parent, true)
        {
            NugetPackage = nugetPackage;
            Name = $"{nugetPackage.Name} [{nugetPackage.Version}]";
            FullName = Name;
            _solutionManager = solutionManager;
        }

        protected override void LoadChildren()
        {
            var usage = _solutionManager.GetNugetUsage(NugetPackage.Name);

            foreach (var nversion in usage.GroupBy(x => x.Nuget.Version).OrderBy(x => x.Key))
            {
                Children.Add(new NugetVersionGroupViewModel(nversion.Key.ToString(), this, nversion));
            }

            base.LoadChildren();
        }
    }
}