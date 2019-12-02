using System.Collections.Generic;
using System.Threading.Tasks;
using DependencyFinder.Core.Models;

namespace DependencyFinder.Core
{
    public interface ISolutionManager
    {
        IAsyncEnumerable<Reference> FindAllReferences(string rootPath, string sourceProject, string className);
        IAsyncEnumerable<string> FindSolutions(string rootDirectory);
        IAsyncEnumerable<string> FindSolutionWithProject(string rootDirectory, string projectName);
        Task<IEnumerable<Project>> OpenSolution(string solutionPath);
    }
}