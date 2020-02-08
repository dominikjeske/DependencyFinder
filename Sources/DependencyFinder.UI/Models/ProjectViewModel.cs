using DependencyFinder.Core;
using DependencyFinder.Core.Models;
using System.IO;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{
    public class ProjectViewModel : TreeViewItemViewModel
    {
        [ExpandableObject]
        public ProjectDetails Project { get; }

        public bool IsNetCore { get; set; }

        public ProjectViewModel(ProjectDetails project, TreeViewItemViewModel parent, bool lazyLoadChildren, ISolutionManager solutionManeger) : base(parent, lazyLoadChildren)
        {
            Project = project;
            Name = Path.GetFileName(project.Name);
            FullName = project.AbsolutePath;

            IsNetCore = project.IsNetCore;

            Children.Add(new NugetCollectionViewModel(Project.Nugets, this, lazyLoadChildren));
            Children.Add(new ProjectCollectionViewModel(Project.ProjectReferences, this, lazyLoadChildren));
            Children.Add(new ReferencesCollectionViewModel(Project.DirectReferences, this, lazyLoadChildren));
            Children.Add(new TypesCollectionViewModel(this, project.AbsolutePath, parent.FullName, solutionManeger));
        }
    }
}