using DependencyFinder.Core.Models;
using System.IO;

namespace DependencyFinder.UI.Models
{
    public class ProjectViewModel : TreeViewItemViewModel
    {
        private readonly Project _project;
        public bool IsNetCore { get; set; }

        public ProjectViewModel(Project project, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            _project = project;
            Name = Path.GetFileName(project.Name);
            IsNetCore = project.IsNetCore;

            Children.Add(new NugetCollectionViewModel(_project.Nugets, this, lazyLoadChildren));
            Children.Add(new ProjectCollectionViewModel(_project.ProjectReferences, this, lazyLoadChildren));
        }
    }
}