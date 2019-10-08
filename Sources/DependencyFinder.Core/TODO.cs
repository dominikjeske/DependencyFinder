using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyFinder.Core
{
    //In addition, by default MSBuildWorkspace will attempt to load any project references that it finds when loading a project.
    //This can be controlled with the MSBuildWorkspace.LoadMetadataForReferencedProjects property.If set to true,
    //MSBuildWorkspace will avoid loading a project reference if that project's output binary already exists on disk. If it does,
    //MSBuildWorkspace will reference the binary instead of the project.

    //SolutionFile solutionInfo = SolutionFile.Parse(@"/path-to-sln.sln");
    //IEnumerable<ProjectInSolution> projectList = solutionInfo.ProjectsInOrder;

    //From here you could use workspace.OpenProjectAsync(project.AbsolutePath) to load available projects. (Note, workspace is a MSBuildWorkspace and project is a ProjectInSolution).

    // https://github.com/dotnet/project-system/blob/master/docs/design-time-builds.md
    // https://josephwoodward.co.uk/2015/10/using-roslyn-to-look-for-code-smells
    // https://www.michalkomorowski.com/2017/03/why-i-hate-roslyn.html
    // https://buildalyzer.netlify.com/

    //public async Task OpenSolution(string solutionPath)
    //{
    //    //AnalyzerManager manager = new AnalyzerManager();
    //    //var xx = manager.GetProject(@"E:\Projects\DependencyFinder\Test\ConsoleApp\ConsoleApp\ConsoleApp.csproj");

    //    //E:\Projects\DependencyFinder\Test\ConsoleApp\ConsoleApp.Core\ConsoleApp.Core.csproj

    //    //E:\Projects\DependencyFinder\Test\Nasted\ConsoleFull\ConsoleFull\packages.config

    //    var nugets = new NugetConfigReader();
    //    //var packages = nugets.GetNugetPackages(@"E:\Projects\DependencyFinder\Test\ConsoleApp\ConsoleApp\ConsoleApp.csproj");
    //    var packages = nugets.GetNugetPackages(@"E:\Projects\DependencyFinder\Test\WPF\WPF\WPF.csproj");

    //    MSBuildLocator.RegisterDefaults();

    //    using (var workspace = MSBuildWorkspace.Create())
    //    {
    //        workspace.LoadMetadataForReferencedProjects = true;

    //        workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

    //        var tst = new Stopwatch();

    //        tst.Start();
    //        //
    //       // var result = DotNetSolution.Load(solutionPath);
    //        var result2 = DotNetProject.Load(@"E:\Projects\DependencyFinder\Test\WPF\WPF\WPF.csproj");

    //        var xxx = result2.PackageReferences.ToList();
    //        var xxx2 = result2.ProjectReferences.ToList();
    //        var xxx3 = result2.ProjectTargets.ToList();


    //        //CommonFullSecond

    //        tst.Stop();

    //        var solution = await workspace.OpenSolutionAsync(solutionPath);

    //        foreach (var project in solution.Projects)
    //        {
    //            var ss = project.MetadataReferences[0];

    //            var compilation = await project.GetCompilationAsync();

    //            Console.WriteLine(project.Name);
    //        }

    //        foreach (var diagnostic in workspace.Diagnostics)
    //        {
    //            if (diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
    //            {
    //                ConsoleEx.WriteErrorLine(diagnostic.Message);
    //            }
    //            else if (diagnostic.Kind == WorkspaceDiagnosticKind.Warning)
    //            {
    //                ConsoleEx.WriteWarningLine(diagnostic.Message);
    //            }
    //        }

    //        //var project = await workspace.OpenProjectAsync("MyProject.csproj");
    //        //var compilation = await project.GetCompilationAsync();
    //    }
    //}

    //private void Workspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
    //{
    //}
}
