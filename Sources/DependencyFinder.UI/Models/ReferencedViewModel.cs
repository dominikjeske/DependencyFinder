using DependencyFinder.Core;
using DependencyFinder.Core.Models;
using System.Collections;
using System.IO;
using System.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{

    public class ReferencedViewModel : TreeViewItemViewModel
    {
        public string Solution { get; set; }

        public ReferencedViewModel(TreeViewItemViewModel parent, ProjectDetails referencedProject) : base(parent, false)
        {
            Name = $"{referencedProject.Name}";
            FullName = $"{referencedProject.AbsolutePath}";
            ReferencedProject = referencedProject;
            Solution = $"{referencedProject.Solution}";
        }

        [ExpandableObject]
        public ProjectDetails ReferencedProject { get; }
    }
}