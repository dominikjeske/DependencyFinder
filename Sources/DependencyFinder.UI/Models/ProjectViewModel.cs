using DependencyFinder.Core.Models;
using System.IO;

namespace DependencyFinder.UI.Models
{
    public class ProjectViewModel : TreeViewItemViewModel
    {
        private readonly Project _project;

        public ProjectViewModel(Project project, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            _project = project;
            Name = Path.GetFileName(project.Name);
        }

        public void AddNuget(NugetPackage nugetPackage)
        {
            var pvm = new NugetViewModel(nugetPackage, this, false);

            Children.Add(pvm);
        }
    }
}