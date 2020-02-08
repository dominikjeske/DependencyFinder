using DependencyFinder.Core;
using System.IO;

namespace DependencyFinder.UI.Models
{
    public class SolutionViewModel : TreeViewItemViewModel
    {
        private readonly ISolutionManager _solutionManager;

        public SolutionViewModel(string solution, ISolutionManager solutionManager, bool lazyLoadChildren = false) : base(null, lazyLoadChildren)
        {
            Name = Path.GetFileName(solution);
            FullName = solution;
            _solutionManager = solutionManager;
        }

        public ProjectViewModel AddProject(Core.Models.ProjectDetails project)
        {
            var pvm = new ProjectViewModel(project, this, false, _solutionManager);

            Children.Add(pvm);

            return pvm;
        }
    }
}