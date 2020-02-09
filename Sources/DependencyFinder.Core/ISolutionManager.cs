using DependencyFinder.Core.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DependencyFinder.Core
{
    public interface ISolutionManager : IDisposable
    {
        IAsyncEnumerable<Reference> FindAllReferences(string rootPath, string sourceProject, string className);

        IAsyncEnumerable<string> FindSolutions(string rootDirectory);

        IAsyncEnumerable<string> FindSolutionWithProject(string rootDirectory, string projectName);

        Task<IEnumerable<ProjectDetails>> ReadSolution(string solutionPath);

        Task<IEnumerable<TypeDetails>> GetProjectTypes(string projectPath, string solutionPath);

        Task FindReferenceInSolutions(string project, ISymbol searchElement);
    }
}