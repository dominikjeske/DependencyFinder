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

        IAsyncEnumerable<Reference> FindReferenceInSolutions(ProjectDetails project, ISymbol searchElement, IProgress<string> progress);
        IEnumerable<ProjectDetails> GetReferencingProjects(ProjectDetails project);
        IEnumerable<NugetProjectMap> GetNugetUsage(string nugetName);
        int GetNumberOfCachedProjects();
        Task Test(ProjectDetails project, IEnumerable<ProjectDetails> destinationProjects);
        Task<ProjectDetails> GetProject(string projectFullPath);
        List<string> GetSolutions(string projectFullPath);
    }
}