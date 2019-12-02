using DependencyFinder.Core.Models;
using System.IO;

namespace DependencyFinder.UI.Models
{
    public class SolutionViewModel : TreeViewItemViewModel
    {
        public SolutionViewModel(string solution, bool lazyLoadChildren = false) : base(null, lazyLoadChildren)
        {
            Name = Path.GetFileName(solution);
        }

        public ProjectViewModel AddProject(Project project)
        {
            var pvm = new ProjectViewModel(project, this, false);

            Children.Add(pvm);

            return pvm;
        }
    }
}