using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SourceFinder
{
    class Program
    {
        static async Task Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();

            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(@"E:\Projects\Dependency\DependencyFinder\SymbolFinder\SourceFinder\SourceFinder.sln");

            var project = solution.Projects.FirstOrDefault(p => p.Name == "Parent");
            var compilation = await project.GetCompilationAsync();

            var containingTypeSymbol = compilation.GetTypeByMetadataName("Slave.TestClass");
            var searchedSymbol = containingTypeSymbol.GetMembers().FirstOrDefault(x => x.Name == "Test");
        
            var results = await SymbolFinder.FindReferencesAsync(searchedSymbol, solution);

            foreach(var result in results)
            {
                foreach(var location in result.Locations)
                {
                    Console.WriteLine(location.Document.FilePath);
                }
            }

        }
    }
}
