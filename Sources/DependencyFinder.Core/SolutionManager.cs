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
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DependencyFinder.Core
{
    public class SolutionManager
    {
        private readonly ILogger<SolutionManager> _logger;

        public SolutionManager(ILogger<SolutionManager> logger)
        {
            _logger = logger;
        }

        public IEnumerable<FastFileInfo> FindSolutions(string rootDirectory)
        {
            return FastFileInfo.EnumerateFiles(rootDirectory, "*.sln", SearchOption.AllDirectories);
        }

        public async Task<IEnumerable<Project>> OpenSolution(string solutionPath)
        {
            var solution = LoadSolution(solutionPath);
            if (solution.IsFailure) return null;

            var solutionDirectory = Path.GetDirectoryName(solutionPath);

            var projects = solution.Value.Projects.Select(p => new Project
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

        private Result<DotNetSolution> LoadSolution(string solutionPath)
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

        public async Task<List<Reference>> FindAllReferences()
        {
            MSBuildLocator.RegisterDefaults();
            var searchResult = new List<Reference>();

            using (var workspace = MSBuildWorkspace.Create())
            {
                var solution = await workspace.OpenSolutionAsync(@"E:\Projects\Dependency\DependencyFinder\Test\WPF\WPF.sln");

                var project = solution.Projects.FirstOrDefault(x => x.Name == "WPF");

                var compilation = await project.GetCompilationAsync();

                var searchedSymbol = compilation.GetTypeByMetadataName("CommonFull.TestClass");

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

                        var className = node.Ancestors()
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
                            ClassName = className,
                            Namespace = @namespace,
                            Block = block,
                            LineNumber = line.StartLinePosition.Line
                        };
                        searchResult.Add(objectReference);
                    }
                }

                //CheckDiagnostics(workspace);
            }

            return searchResult;
        }

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