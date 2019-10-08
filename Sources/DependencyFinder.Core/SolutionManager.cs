using ByteDev.DotNet.Project;
using ByteDev.DotNet.Solution;
using DependencyFinder.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyFinder.Core
{
    public class SolutionManager
    {
        public async Task<IEnumerable<FileInfo>> FindSolutions(string rootDirectory, CancellationToken cancellationToken = default)
        {
            var searcher = new SolutionSearcher();
            var result = await searcher.Search(rootDirectory, cancellationToken);
            return result;
        }

        public async Task<IEnumerable<Project>> OpenSolution(string solutionPath)
        {
            var solution = DotNetSolution.Load(solutionPath);
            var solutionDirectory = Path.GetDirectoryName(solutionPath);

            var projects = solution.Projects.Select(p => new Project
            {
                Id = p.Id,
                Name = p.Name,
                RelativePath = p.Path,
                AbsolutePath = Path.GetFullPath(Path.Combine(solutionDirectory, p.Path)),
                Type = p.Type?.Description,
                Solution = solutionPath
            });

            var result = projects.AsParallel().Select(async p =>
            {
                var project = DotNetProject.Load(p.AbsolutePath);
                p.IsNetCore = project.Format == ProjectFormat.New;

                if (project.Format == ProjectFormat.Old)
                {
                    var nugets = await NugetConfigReader.GetNugetPackages(p.AbsolutePath);
                    p.Nugets.AddRange(nugets);
                }
                else
                {
                    p.Nugets.AddRange(project.PackageReferences.Select(p => new NugetPackage
                    {
                        Name = p.Name,
                        Version = new Version(p.Version)
                    }));
                }

                return p;
            }).ToList();

            var projectsResult = await Task.WhenAll(result);
            return projectsResult;
        }
    }
}