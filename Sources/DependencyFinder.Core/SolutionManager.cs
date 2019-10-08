using ByteDev.DotNet.Project;
using ByteDev.DotNet.Solution;
using DependencyFinder.Core.Models;
using DependencyFinder.Utils;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
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

        public async Task FindAllReferences()
        {
            MSBuildLocator.RegisterDefaults();

            using (var workspace = MSBuildWorkspace.Create())
            {
                var solution = await workspace.OpenSolutionAsync(@"E:\Projects\Dependency\DependencyFinder\Test\WPF\WPF.sln");

                var project = solution.Projects.FirstOrDefault(x => x.Name == "WPF");

                var compilation = await project.GetCompilationAsync();

                var searchedSymbol = compilation.GetTypeByMetadataName("CommonFull.TestClass");

                var results = await SymbolFinder.FindReferencesAsync(searchedSymbol, solution);

                foreach (var indexReference in results)
                {
                    foreach (ReferenceLocation indexReferenceLocation in indexReference.Locations)
                    {
                        int spanStart = indexReferenceLocation.Location.SourceSpan.Start;
                        var doc = indexReferenceLocation.Document;

                        var indexerInvokation = doc.GetSyntaxRootAsync().Result
                            .DescendantNodes()
                            .FirstOrDefault(node => node.GetLocation().SourceSpan.Start == spanStart);

                        var className = indexerInvokation.Ancestors()
                            .OfType<ClassDeclarationSyntax>()
                            .FirstOrDefault()
                            ?.Identifier.Text ?? String.Empty;

                        var @namespace = indexerInvokation.Ancestors()
                            .OfType<NamespaceDeclarationSyntax>()
                            .FirstOrDefault()
                            ?.Name.ToString() ?? String.Empty;


                        Console.WriteLine($"{@namespace}.{className} : {indexerInvokation.GetText()}");
                    }
                }

                //CheckDiagnostics(workspace);
            }
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
    }
}