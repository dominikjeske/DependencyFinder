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
using Microsoft.CodeAnalysis.Text;
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
        private readonly ConcurrentDictionary<string, List<ProjectDetails>> _projectUsedByCache = new ConcurrentDictionary<string, List<ProjectDetails>>();

        private readonly ILogger<SolutionManager> _logger;

        static SolutionManager()
        {
            MSBuildLocator.RegisterDefaults();
        }

        public SolutionManager(ILogger<SolutionManager> logger)
        {
            _logger = logger;
        }

        private Task<Solution> OpenSolution(string SolutionPath)
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
            IEnumerable<TypeDetails> typesList;
            var projectName = Path.GetFileNameWithoutExtension(projectPath);

            var solution = await OpenSolution(solutionPath);

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
                                .Where(t => t.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public);

            typesList = types.Where(t => t.TypeKind == TypeKind.Interface)
                             .Select(x => new InterfaceDetails(x.Name, x.GetMethodMembers(), x));

            typesList = typesList.Union(types.Where(t => t.TypeKind == TypeKind.Class)
                                 .Select(x => new ClassDetails(x.Name, x.GetClassMembers(), x)));

            typesList = typesList.Union(types.Where(t => t.TypeKind == TypeKind.Enum)
                                 .Select(x => new EnumDetails(x.Name, x.GetPropertyMembers(), x)));

            typesList = typesList.Union(types.Where(t => t.TypeKind == TypeKind.Struct)
                                 .Select(x => new StructDetails(x.Name, x.GetClassMembers(), x)));

            return typesList;
        }

        public async IAsyncEnumerable<Reference> FindReferenceInSolutions(ProjectDetails project, ISymbol searchElement)
        {
            //TODO what if project that is using project form argument is used in many solutions

            var projects = _projectUsedByCache[project.AbsolutePath];
            foreach (var p in projects)
            {
                var solutionWorkspace = await OpenSolution(p.Solution);

                await foreach(var result in FindSymbol(searchElement, solutionWorkspace, project))
                {
                    yield return result;
                }
            }
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

        public async Task<IEnumerable<ProjectDetails>> ReadSolution(string solutionPath)
        {
            var projects = ReadProjectsFromSolution(solutionPath);
            var result = await ReadProjectDetails(projects);

            foreach (var project in result)
            {
                foreach (var pr in project.ProjectReferences)
                {
                    _projectUsedByCache.AddOrUpdate(pr.FilePath, new List<ProjectDetails> { project }, (pKey, references) =>
                    {
                        references.Add(project);
                        return references;
                    });
                }
            }

            return result;
        }

        public IEnumerable<ProjectDetails> GetReferencingProjects(ProjectDetails project)
        {
            if (!_projectUsedByCache.ContainsKey(project.AbsolutePath)) return Enumerable.Empty<ProjectDetails>();

            return _projectUsedByCache[project.AbsolutePath];
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
                            var projectAbsolutePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.AbsolutePath), pr.FilePath));
                            project.ProjectReferences.Add(new Models.ProjectReference { FilePath = projectAbsolutePath, Name = Path.GetFileName(pr.FilePath) });
                        }

                        foreach (var targ in projectInfo.ProjectTargets)
                        {
                            project.ProjectTargets.Add(new ProjectTarget { Description = targ.Description, Type = targ.Type.ToString(), TargetValue = targ.TargetValue, Version = targ.Version });
                        }

                        foreach (var pr in projectInfo.References.Where(x => !string.IsNullOrWhiteSpace(x.Path) && string.IsNullOrWhiteSpace(x.Version)))
                        {
                            var projectAbsolutePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.AbsolutePath), pr.Path));
                            project.DirectReferences.Add(new Models.ProjectReference { FilePath = projectAbsolutePath, Name = pr.Name });
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
                            AssemblyVersion = projectInfo.AssemblyInfo.Version,
                            AssemblyName = projectInfo.AssemblyInfo.AssemblyName
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

                //TODO fix
                await foreach(var result in FindSymbol(searchedSymbol, solution, null))
                {
                    yield return result;
                         
                }
            }
        }

        private async IAsyncEnumerable<Reference> FindSymbol(ISymbol symbol, Solution solution, ProjectDetails pd)
        {
            var project = solution.Projects.FirstOrDefault(p => p.Name == pd.Name);
            var compilation = await project.GetCompilationAsync();
            ISymbol searchedSymbol;

            if (symbol.Kind == SymbolKind.NamedType)
            {
                searchedSymbol = compilation.GetTypeByMetadataName(symbol.ToString());
            }
            else
            {
                var containingTypeSymbol = compilation.GetTypeByMetadataName(symbol.ContainingType.ToString());
                searchedSymbol = containingTypeSymbol.GetMembers().FirstOrDefault(x => x.Kind == symbol.Kind && x.Name == symbol.Name);
            }
            
            var results = await SymbolFinder.FindReferencesAsync(searchedSymbol, solution);

            foreach (var reference in results)
            {
                foreach (ReferenceLocation location in reference.Locations)
                {
                    int spanStart = location.Location.SourceSpan.Start;
                    var doc = location.Document;

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
                        FilePath = doc.FilePath,
                        ProjectName = doc.Project.Name,
                        ClassName = definitionClassName,
                        Namespace = @namespace,
                        Block = block,
                        LineNumber = line.EndLinePosition.Line
                    };

                    yield return objectReference;
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
    }

}
