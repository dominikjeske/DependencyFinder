using DependencyFinder.Core.Models;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{
    public class ReferencedViewModel : TreeViewItemViewModel, IProjectInfo
    {
        public string Solution { get; set; }

        public ReferencedViewModel(TreeViewItemViewModel parent, ProjectDetails referencedProject) : base(parent, false)
        {
            Name = $"{referencedProject.Name}";
            FullName = $"{referencedProject.AbsolutePath}";
            Project = referencedProject;
            Solution = $"{referencedProject.Solution}";
        }

        [ExpandableObject]
        public ProjectDetails Project { get; }
    }
}