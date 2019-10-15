using ByteDev.DotNet.Project;
using ByteDev.DotNet.Solution;
using CSharpFunctionalExtensions;
using DependencyFinder.Core.Models;
using DependencyFinder.Search;
using DependencyFinder.Utils;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DependencyFinder.Core
{
    public class SolutionManager
    {
        static SolutionManager()
        {
            MSBuildLocator.RegisterDefaults();
        }

        private readonly ILogger<SolutionManager> _logger;

        public SolutionManager(ILogger<SolutionManager> logger)
        {
            _logger = logger;
        }

        public IAsyncEnumerable<string> FindSolutions(string rootDirectory)
        {
            var asyncFinder = new AsyncFileFinder();
            return asyncFinder.Find(rootDirectory, "*.sln");
        }

        public async IAsyncEnumerable<string> FindSolutionWithProject(string rootDirectory, string projectName)
        {
            var solutions = new List<string>();
            await foreach (var solution in FindSolutions(rootDirectory))
            {
                if(ReadProjectsFromSolution(solution).Any(p => p.Name == projectName))
                {
                    yield return solution;
                }
            }
        }

        public async Task<IEnumerable<Project>> OpenSolution(string solutionPath)
        {
            var projects = ReadProjectsFromSolution(solutionPath);
            var result = await ReadProjectDetails(projects);
            return result;
        }

        private async Task<IEnumerable<Project>> ReadProjectDetails(IEnumerable<Project> projects)
        {
            var tasks = projects.AsParallel().Select(async project =>
            {
                var projectInfo = DotNetProject.Load(project.AbsolutePath);
                project.IsNetCore = projectInfo.Format == ProjectFormat.New;

                var nugets = await ReadNuget(project.AbsolutePath, projectInfo);
                project.Nugets.AddRange(nugets);

                return project;
            }).ToList();

            var result =  await Task.WhenAll(tasks);

            return result;
        }

        private async Task<IEnumerable<NugetPackage>> ReadNuget(string projectPath, DotNetProject project)
        {
            if (project.Format == ProjectFormat.Old)
            {
                var nugets = await NugetConfigReader.GetNugetPackages(projectPath);
                return nugets;
            }
            else
            {
                return project.PackageReferences.Select(p => new NugetPackage
                {
                    Name = p.Name,
                    Version = VersionEx.FromString(p.Version)
                });
            }
        }

        private IEnumerable<Project> ReadProjectsFromSolution(string solutionPath)
        {
            var solution = ReadSolutionFromDisk(solutionPath);

            if (solution.IsFailure) return Enumerable.Empty<Project>();

            var solutionDirectory = Path.GetDirectoryName(solutionPath);

            return solution.Value
                            .Projects
                            .Where(x => !x.Type.IsSolutionFolder && x.Type.Description == "C#")
                            .Select(p => new Project
                            {
                                Id = p.Id,
                                Name = p.Name,
                                RelativePath = p.Path,
                                AbsolutePath = Path.GetFullPath(Path.Combine(solutionDirectory, p.Path)),
                                Type = p.Type?.Description,
                                Solution = solutionPath
                            });
        }

        private Result<DotNetSolution> ReadSolutionFromDisk(string solutionPath)
        {
            try
            {
                var solution = DotNetSolution.Load(solutionPath);
                return Result.Ok(solution);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Cannot load solution {solutionPath}");
                return Result.Failure<DotNetSolution>("");
            }
        }

        public async IAsyncEnumerable<Reference> FindAllReferences(string rootPath, string sourceProject, string className)
        {
            var projectName = Path.GetFileNameWithoutExtension(sourceProject);

            await foreach(var solution in FindSolutionWithProject(rootPath, projectName))
            {
                await foreach(var reference in FindReferenceInSolution(solution, projectName, className))
                {
                    yield return reference;
                }
            }
        }

        private async IAsyncEnumerable<Reference> FindReferenceInSolution(string solutionPath, string projectName, string className)
        {
            using (var workspace = MSBuildWorkspace.Create())
            {
                var solution = await workspace.OpenSolutionAsync(solutionPath);
                var project = solution.Projects.FirstOrDefault(p => p.Name == projectName); //TODO check for null

    
                var compilation = await project.GetCompilationAsync();

                var searchedSymbol = compilation.GetTypeByMetadataName(className.Trim());

                var results = await SymbolFinder.FindReferencesAsync(searchedSymbol, solution);

                foreach (var reference in results)
                {
                    foreach (ReferenceLocation location in reference.Locations)
                    {
                        int spanStart = location.Location.SourceSpan.Start;
                        var doc = location.Document;
                        var ss = location.Location.ToString();

                        var root = await doc.GetSyntaxRootAsync();
                        var node = root.DescendantNodes()
                                        .FirstOrDefault(node => node.GetLocation().SourceSpan.Start == spanStart);

                        var line = node.SyntaxTree.GetLineSpan(location.Location.SourceSpan);

                        var definitionClassName = node.Ancestors()
                                            .OfType<ClassDeclarationSyntax>()
                                            .FirstOrDefault()
                                            ?.Identifier.Text ?? string.Empty;

                        var @namespace = node.Ancestors()
                                                .OfType<NamespaceDeclarationSyntax>()
                                                .FirstOrDefault()
                                            ?.Name.ToString() ?? String.Empty;

                        var block = node.Ancestors()
                                                .OfType<BlockSyntax>()
                                                .FirstOrDefault()
                                            ?.ToString() ?? String.Empty;

                        var objectReference = new Reference
                        {
                            FileName = doc.Name,
                            ProjectName = doc.Project.Name,
                            ClassName = definitionClassName,
                            Namespace = @namespace,
                            Block = block,
                            LineNumber = line.StartLinePosition.Line
                        };

                        yield return objectReference;

                    }
                }
                
            }
        }

        //TODO
        private static void CheckDiagnostics(MSBuildWorkspace workspace)
        {
            foreach (var diagnostic in workspace.Diagnostics)
            {
                if (diagnostic.Kind == Microsoft.CodeAnalysis.WorkspaceDiagnosticKind.Failure)
                {
                    ConsoleEx.WriteErrorLine(diagnostic.Message);
                }
                else if (diagnostic.Kind == Microsoft.CodeAnalysis.WorkspaceDiagnosticKind.Warning)
                {
                    ConsoleEx.WriteWarningLine(diagnostic.Message);
                }
            }
        }

        public IAsyncEnumerable<R> Execute<T, R>(IEnumerable<T> tasks, Func<T, Task<R>> action)
        {
            var channel = Channel.CreateUnbounded<R>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });

            var result = Parallel.ForEach(tasks, async (data) =>
            {
                var result = await action(data);
                await channel.Writer.WriteAsync(result);
            });

            var xxxx = result.IsCompleted;

            return channel.Reader.ReadAllAsync();
        }
    }
}