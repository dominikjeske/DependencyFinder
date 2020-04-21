using DependencyFinder.Core;
using DependencyFinder.Core.Models;
using System.IO;
using System.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{
    public class ProjectViewModel : TreeViewItemViewModel, IProjectInfo
    {
        [ExpandableObject]
        public ProjectDetails Project { get; }

        public bool IsNetCore { get; set; }

        public ReferencedCollectionViewModel References => Children.Single(x => x is ReferencedCollectionViewModel) as ReferencedCollectionViewModel;

        public ProjectViewModel(ProjectDetails project, TreeViewItemViewModel parent, bool lazyLoadChildren, ISolutionManager solutionManeger) : base(parent, lazyLoadChildren)
        {
            Project = project;
            Name = Path.GetFileName(project.Name);
            FullName = project.AbsolutePath;
            IsNetCore = project.IsNetCore;

            Children.Add(new NugetCollectionViewModel(Project.Nugets, this, solutionManeger));
            Children.Add(new ProjectCollectionViewModel(Project.ProjectReferences, this));
            Children.Add(new ReferencesCollectionViewModel(Project.DirectReferences, this));
            Children.Add(new TypesCollectionViewModel(this, project.AbsolutePath, parent.FullName, solutionManeger));
            Children.Add(new ReferencedCollectionViewModel(this));
            Children.Add(new SourceCodesCollectionViewModel(this, project.SourceCodes));
        }
    }
}