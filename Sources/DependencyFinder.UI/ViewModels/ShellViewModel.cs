using Caliburn.Micro;
using DependencyFinder.Core;
using DependencyFinder.UI.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DependencyFinder.UI.ViewModels
{
    public class ShellViewModel : Screen
    {
        public string SolutionsRoot { get; set; }
        public ObservableCollection<SolutionViewModel> Solutions { get; set; } = new ObservableCollection<SolutionViewModel>();

        private readonly ISolutionManager _solutionManager;

        public ShellViewModel(ISolutionManager solutionManager)
        {
            SolutionsRoot = @"E:\Projects\Dependency\DependencyFinder\Test";
            _solutionManager = solutionManager;
        }

        private async Task LoadSolutions()
        {
            var solutions = _solutionManager.FindSolutions(SolutionsRoot);

            await foreach (var s in solutions)
            {
                var solutionViewModel = new SolutionViewModel(s);
                Solutions.Add(solutionViewModel);

                var projects = await _solutionManager.OpenSolution(s);
                foreach (var p in projects)
                {
                    foreach (var n in p.Nugets)
                    {
                    }
                }
            }
        }

        public async Task OnLoaded()
        {
            await LoadSolutions();
        }
    }
}