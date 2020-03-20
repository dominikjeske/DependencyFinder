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
        private readonly ConcurrentDictionary<string, Dictionary<string, ProjectDetails>> _projectUsedByCache = new ConcurrentDictionary<string, Dictionary<string, ProjectDetails>>();
        private readonly ConcurrentDictionary<string, Dictionary<string, NugetProjectMap>> _nugetCache = new ConcurrentDictionary<string, Dictionary<string, NugetProjectMap>>();
        private readonly ConcurrentDictionary<string, AsyncLazy<ProjectDetails>> _projectsCache = new ConcurrentDictionary<string, AsyncLazy<ProjectDetails>>();

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
            //var projectName = Path.GetFileNameWithoutExtension(projectPath);

            var solution = await OpenSolution(solutionPath);

            var types = solution.Projects
                                .Where(x => x.FilePath == projectPath)
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
            var resultCache = new List<Reference>();

            var projects = _projectUsedByCache[project.AbsolutePath];
            foreach (var solution in projects.Values.Select(p => p.Solution).Distinct())
            {
                var solutionWorkspace = await OpenSolution(solution);

                await foreach (var result in FindSymbol(searchElement, solutionWorkspace, project))
                {
                    if (resultCache.Contains(result)) continue;

                    resultCache.Add(result);
                    yield return result;
                }
            }
        }

        public IAsyncEnumerable<string> FindSolutions(string rootDirectory)
        {
            var asyncFinder = new AsyncFileFinder();
            return asyncFinder.Find(rootDirectory, "*.sln");
        }

        public int GetNumberOfCachedProjects() => _projectsCache.Count;

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

            return result;
        }

        public IEnumerable<ProjectDetails> GetReferencingProjects(ProjectDetails project)
        {
            if (!_projectUsedByCache.ContainsKey(project.AbsolutePath)) return Enumerable.Empty<ProjectDetails>();

            return _projectUsedByCache[project.AbsolutePath].Values;
        }

        private async Task<IEnumerable<ProjectDetails>> ReadProjectDetails(IEnumerable<ProjectDetails> projects)
        {
            var tasks = projects.AsParallel().Select(project =>
            {
                if (File.Exists(project.AbsolutePath))
                {
                    return _projectsCache.GetOrAdd(project.AbsolutePath, key => new AsyncLazy<ProjectDetails>(async () =>
                    {
                        var projectInfo = DotNetProject.Load(key);
                        project.IsNetCore = projectInfo.Format == ProjectFormat.New;
                        project.IsMultiTarget = projectInfo.IsMultiTarget;
                        project.AssemblyInfo = projectInfo.GetAssemblyInfo();
                        await ExtractNugetInfo(project, projectInfo);
                        ExtractProjectReferences(project, projectInfo);
                        ExtractTargets(project, projectInfo);
                        ExtractDirectReferences(project, projectInfo);
                        ExtractSourceCode(project);

                        return project;
                    })).Task;
                }

                return Task.FromResult(project);
            }).ToList();

            try
            {
                var result = await Task.WhenAll(tasks);
                return result;
            }
            catch (Exception ee)
            {

            }

            return Enumerable.Empty<ProjectDetails>();

        }

        private static void ExtractDirectReferences(ProjectDetails project, DotNetProject projectInfo)
        {
            foreach (var pr in projectInfo.References.Where(x => !string.IsNullOrWhiteSpace(x.Path) && string.IsNullOrWhiteSpace(x.Version)))
            {
                var projectAbsolutePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.AbsolutePath), pr.Path));
                project.DirectReferences.Add(new Models.ProjectReference { FilePath = projectAbsolutePath, Name = pr.Name });
            }
        }

        private static void ExtractTargets(ProjectDetails project, DotNetProject projectInfo)
        {
            foreach (var targ in projectInfo.ProjectTargets)
            {
                project.ProjectTargets.Add(new ProjectTarget { Description = targ.Description, Type = targ.Type.ToString(), TargetValue = targ.TargetValue, Version = targ.Version });
            }
        }

        private static void ExtractSourceCode(ProjectDetails project)
        {
            var projectDirectory = Path.GetDirectoryName(project.AbsolutePath);

            if (File.Exists(project.AbsolutePath))
            {
                var sourceFiles = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                                           .Where(f => f.IndexOf("\\obj\\") == -1);

                project.SourceCodes.AddRange(sourceFiles);
            }
            else
            {
                //TODO
            }
        }

        private void ExtractProjectReferences(ProjectDetails project, DotNetProject projectInfo)
        {
            foreach (var pr in projectInfo.ProjectReferences)
            {
                var projectAbsolutePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.AbsolutePath), pr.FilePath));
                project.ProjectReferences.Add(new Models.ProjectReference { FilePath = projectAbsolutePath, Name = Path.GetFileNameWithoutExtension(pr.FilePath) });
            }

            foreach (var pr in project.ProjectReferences)
            {
                _projectUsedByCache.AddOrUpdate(pr.FilePath, new Dictionary<string, ProjectDetails> { [project.AbsolutePath] = project }, (pKey, references) =>
                {
                    if (!references.ContainsKey(project.AbsolutePath))
                    {
                        references.Add(project.AbsolutePath, project);
                    }
                    return references;
                });
            }
        }

        private async Task ExtractNugetInfo(ProjectDetails project, DotNetProject projectInfo)
        {
            var nugets = await ReadNuget(project.AbsolutePath, projectInfo);

            project.Nugets.AddRange(nugets);

            foreach (var nuget in project.Nugets)
            {
                var nugetMap = new NugetProjectMap
                {
                    Nuget = nuget,
                    Project = project
                };

                _nugetCache.AddOrUpdate(nuget.Name, new Dictionary<string, NugetProjectMap> { [nugetMap.Project.AbsolutePath] = nugetMap }, (pKey, nugets) =>
                {
                    if (!nugets.ContainsKey(nugetMap.Project.AbsolutePath))
                    {
                        nugets.Add(nugetMap.Project.AbsolutePath, nugetMap);
                    }

                    return nugets;
                });
            }
        }

        public IEnumerable<NugetProjectMap> GetNugetUsage(string nugetName) => _nugetCache[nugetName].Values;

        private async Task<IEnumerable<NugetPackage>> ReadNuget(string projectPath, DotNetProject project)
        {
            if (project.Format == ProjectFormat.Old)
            {
                var nugets = await NugetConfigReader.GetNugetPackages(projectPath);
                return nugets;
            }
            else
            {
                return project.PackageReferences.Where(x => !string.IsNullOrWhiteSpace(x.Name))
                                                .Select(p => new NugetPackage
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
                           .Where(x => !x.Type.IsSolutionFolder)
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
                await foreach (var result in FindSymbol(searchedSymbol, solution, null))
                {
                    yield return result;
                }
            }
        }

        private async IAsyncEnumerable<Reference> FindSymbol(ISymbol symbol, Solution solution, ProjectDetails pd)
        {
            var project = solution.Projects.FirstOrDefault(p => p.FilePath == pd.AbsolutePath);
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
                        SolutionName = Path.GetFileNameWithoutExtension(solution.FilePath),
                        ProjectName = doc.Project.Name,
                        ClassName = definitionClassName,
                        Namespace = @namespace,
                        Block = block,
                        LineNumber = line.EndLinePosition.Line,
                        SelectionStart = spanStart,
                        SelectionLenght = location.Location.SourceSpan.Length
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