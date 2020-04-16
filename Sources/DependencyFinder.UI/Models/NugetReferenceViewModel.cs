using DependencyFinder.Core;
using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{

    public class NugetReferenceViewModel : TreeViewItemViewModel
    {
        public NugetReferenceViewModel(NugetProjectMap map, TreeViewItemViewModel parent) : base(parent, false)
        {
            Name = map.Project.Name;
            FullName = map.Project.AbsolutePath;
            Map = map;
        }

        public NugetProjectMap Map { get; }
    }
}