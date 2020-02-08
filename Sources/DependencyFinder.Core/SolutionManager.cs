using ByteDev.DotNet.Project;
using ByteDev.DotNet.Solution;
using CSharpFunctionalExtensions;
using DependencyFinder.Core.Models;
using DependencyFinder.Search;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DependencyFinder.Core
{
    public class SolutionManager : ISolutionManager
    {
        private readonly ConcurrentDictionary<string, AsyncLazy<Solution>> _solutionCache = new ConcurrentDictionary<string, AsyncLazy<Solution>>();

        private Task<Solution> GetSolution(string SolutionPath)
        {
            var solution = _solutionCache.GetOrAdd(SolutionPath, s =>
            {
                return new AsyncLazy<Solution>(async () =>
                {
                    var workspace = MSBuildWorkspace.Create();
                    var solution = await workspace.OpenSolutionAsync(s);

                    return solution;
                });
            });

            return solution.Task;
        }

        public async Task<IEnumerable<TypeDetails>> GetProjectTypes(string projectPath, string solutionPath)
        {
            var projectName = Path.GetFileNameWithoutExtension(projectPath);

            var solution = await GetSolution(solutionPath);

            var types = solution.Projects
                           .Where(x => x.Name == projectName)
                           .SelectMany(project => project.Documents)
                           .Select(document =>
                               new
                               {
                                   Model = document.GetSemanticModelAsync().Result,
                                   Declarations = document.GetSyntaxRootAsync()
                                                          .Result
                                                          .DescendantNodes()
                                                          .OfType<BaseTypeDeclarationSyntax>()
                               }
                           )
                           .SelectMany(pair => pair.Declarations.Select(declaration => pair.Model.GetDeclaredSymbol(declaration) as INamedTypeSymbol))
                           .Where(t => t.DeclaredAccessibility == Accessibility.Public);

            var xx = types.Where(t => t.TypeKind == TypeKind.Class);

            
           IEnumerable <TypeDetails> interfaces = types.Where(t => t.TypeKind == TypeKind.Interface)
                                                       .Select(x =>
                                                       new InterfaceDetails
                                                       {
                                                           Name = x.Name,
                                                           Kind = x.TypeKind.ToString(),
                                                           Members = x.GetMembers()
                                                                      .Where(x => x.CanBeReferencedByName)
                                                                      .Select(y => new MethodMember
                                                                      {
                                                                          Name = y.Name
                                                                      }).ToList()
                                                       }
                                                       );

            IEnumerable<TypeDetails> classes =  types.Where(t => t.TypeKind == TypeKind.Class)
                                                    .Select(x =>
                                                   new ClassDetails
                                                   {
                                                       Name = x.Name,
                                                       Kind = x.TypeKind.ToString(),
                                                       Members =  ((IEnumerable<Member>)(x.GetMembers()
                                                                   .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Method)
                                                                   .Select(y => new MethodMember { Name = y.Name })
                                                                  )).Union
                                                                  (x.GetMembers()
                                                                   .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Property)
                                                                   .Select(y => new PropertyMember { Name = y.Name }))
                                                                  .Union
                                                                  (x.GetMembers()
                                                                   .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Field)
                                                                   .Select(y => new FieldMember { Name = y.Name }))
                                                                  .Union
                                                                  (x.GetMembers()
                                                                   .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Event)
                                                                   .Select(y => new EventMember { Name = y.Name }))
                                                   }
                                               );

            IEnumerable<TypeDetails> enums = types.Where(t => t.TypeKind == TypeKind.Enum)
                                                    .Select(x =>
                                                   new EnumDetails
                                                   {
                                                       Name = x.Name,
                                                       Kind = x.TypeKind.ToString(),
                                                       Members = x.GetMembers()
                                                                  .Where(x => x.CanBeReferencedByName)
                                                                  .Select(y => new PropertyMember
                                                                  {
                                                                      Name = y.Name
                                                                  }).ToList()
                                                   }
                                               );

            IEnumerable<TypeDetails> structs = types.Where(t => t.TypeKind == TypeKind.Struct)
                                                    .Select(x =>
                                                   new StructDetails
                                                   {
                                                       Name = x.Name,
                                                       Kind = x.TypeKind.ToString(),
                                                       Members = ((IEnumerable<Member>)(x.GetMembers()
                                                                   .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Method)
                                                                   .Select(y => new MethodMember { Name = y.Name })
                                                                  )).Union
                                                                  (x.GetMembers()
                                                                   .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Property)
                                                                   .Select(y => new PropertyMember { Name = y.Name }))
                                                                  .Union
                                                                  (x.GetMembers()
                                                                   .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Field)
                                                                   .Select(y => new FieldMember { Name = y.Name }))
                                                                  .Union
                                                                  (x.GetMembers()
                                                                   .Where(x => x.CanBeReferencedByName && x.Kind == SymbolKind.Event)
                                                                   .Select(y => new EventMember { Name = y.Name }))
                                                   }
                                               );

            return interfaces.Union(classes).Union(enums).Union(structs);

        }

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
                if (ReadProjectsFromSolution(solution).Any(p => p.Name == projectName))
                {
                    yield return solution;
                }
            }
        }

        public async Task<IEnumerable<ProjectDetails>> OpenSolution(string solutionPath)
        {
            var projects = ReadProjectsFromSolution(solutionPath);
            var result = await ReadProjectDetails(projects);
            return result;
        }

        private async Task<IEnumerable<ProjectDetails>> ReadProjectDetails(IEnumerable<ProjectDetails> projects)
        {
            var tasks = projects.AsParallel().Select(async project =>
            {
                try
                {
                    if (File.Exists(project.AbsolutePath))
                    {
                        var projectInfo = DotNetProject.Load(project.AbsolutePath);
                        project.IsNetCore = projectInfo.Format == ProjectFormat.New;

                        var nugets = await ReadNuget(project.AbsolutePath, projectInfo);
                        project.Nugets.AddRange(nugets);

                        foreach (var pr in projectInfo.ProjectReferences)
                        {
                            project.ProjectReferences.Add(new Models.ProjectReference { FilePath = pr.FilePath, Name = Path.GetFileName(pr.FilePath) });
                        }

                        foreach (var targ in projectInfo.ProjectTargets)
                        {
                            project.ProjectTargets.Add(new ProjectTarget { Description = targ.Description, Type = targ.Type.ToString(), TargetValue = targ.TargetValue, Version = targ.Version });
                        }

                        foreach (var pr in projectInfo.References.Where(x => !string.IsNullOrWhiteSpace(x.Path) && string.IsNullOrWhiteSpace(x.Version)))
                        {
                            project.DirectReferences.Add(new Models.ProjectReference { FilePath = pr.Path, Name = pr.Name });
                        }

                        project.IsMultiTarget = projectInfo.IsMultiTarget;
                        project.AssemblyInfo = new AssemblyInfo
                        {
                            Company = projectInfo.AssemblyInfo.Company,
                            Configuration = projectInfo.AssemblyInfo.Configuration,
                            Copyright = projectInfo.AssemblyInfo.Copyright,
                            Description = projectInfo.AssemblyInfo.Description,
                            FileVersion = projectInfo.AssemblyInfo.FileVersion,
                            InformationalVersion = projectInfo.AssemblyInfo.InformationalVersion,
                            NeutralLanguage = projectInfo.AssemblyInfo.NeutralLanguage,
                            Product = projectInfo.AssemblyInfo.Product,
                            AssemblyTitle = projectInfo.AssemblyInfo.Title,
                            AssemblyVersion = projectInfo.AssemblyInfo.Version
                        };
                    }
                }
                //TODO
                catch (InvalidDotNetProjectException ex)
                {
                }
                catch (Exception ee)
                {
                }

                return project;
            }).ToList();

            var result = await Task.WhenAll(tasks);

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

        private IEnumerable<ProjectDetails> ReadProjectsFromSolution(string solutionPath)
        {
            var solution = ReadSolutionFromDisk(solutionPath);

            if (solution.IsFailure) return Enumerable.Empty<ProjectDetails>();

            var solutionDirectory = Path.GetDirectoryName(solutionPath);

            return solution.Value
                            .Projects
                            .Where(x => !x.Type.IsSolutionFolder) //x.Type.Description == "C#"
                            .Select(p => new ProjectDetails
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

            await foreach (var solution in FindSolutionWithProject(rootPath, projectName))
            {
                await foreach (var reference in FindReferenceInSolution(solution, projectName, className))
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

        public void Dispose()
        {
            //TODO maybe it could be safer way
            foreach (var solution in _solutionCache.Values.Where(c => c.IsStarted).Select(x => x.Task.Result))
            {
                solution.Workspace.Dispose();
            }
        }

        //TODO
        //private static void CheckDiagnostics(MSBuildWorkspace workspace)
        //{
        //    foreach (var diagnostic in workspace.Diagnostics)
        //    {
        //        if (diagnostic.Kind == Microsoft.CodeAnalysis.WorkspaceDiagnosticKind.Failure)
        //        {
        //            ConsoleEx.WriteErrorLine(diagnostic.Message);
        //        }
        //        else if (diagnostic.Kind == Microsoft.CodeAnalysis.WorkspaceDiagnosticKind.Warning)
        //        {
        //            ConsoleEx.WriteWarningLine(diagnostic.Message);
        //        }
        //    }
        //}

        //public IAsyncEnumerable<R> Execute<T, R>(IEnumerable<T> tasks, Func<T, Task<R>> action)
        //{
        //    var channel = Channel.CreateUnbounded<R>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });

        //    var result = Parallel.ForEach(tasks, async (data) =>
        //    {
        //        var result = await action(data);
        //        await channel.Writer.WriteAsync(result);
        //    });

        //    var xxxx = result.IsCompleted;

        //    return channel.Reader.ReadAllAsync();
        //}
    }
}