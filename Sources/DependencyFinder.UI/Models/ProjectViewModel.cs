using DependencyFinder.Core.Models;
using System.Collections.Generic;
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

    public class NugetCollectionViewModel : TreeViewItemViewModel
    {
        public NugetCollectionViewModel(IEnumerable<NugetPackage> nugets, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            Name = "Nugets";

            foreach(var nuget in nugets)
            {
                var pvm = new NugetViewModel(nuget, this, false);

                Children.Add(pvm);
            }
        }
    }


    public class ProjectRefViewModel : TreeViewItemViewModel
    {
        private readonly ProjectReference _projectReference;

        public ProjectRefViewModel(ProjectReference projectRef, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            _projectReference = projectRef;
            Name = _projectReference.Name;
        }
    }

    public class ProjectCollectionViewModel : TreeViewItemViewModel
    {
        public ProjectCollectionViewModel(IEnumerable<ProjectReference> projects, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            Name = "Projects";

            foreach (var project in projects)
            {
                var pvm = new ProjectRefViewModel(project, this, false);

                Children.Add(pvm);
            }

        }
    }
}
