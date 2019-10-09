using CommandLine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DependencyFinder
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            //args = new string[] { "list", "--help" };
            //args = new string[] {"list", @"E:\Projects\Dependency\DependencyFinder\Test" };
            //args = new string[] {"list", @"E:\Projects\Dependency\DependencyFinder\Test", "-p" };
            //args = new string[] {"list", @"E:\Projects\Dependency\DependencyFinder\Test", "-n" };
            //args = new string[] {"list", @"E:\Projects\Dependency\DependencyFinder\Test", "-n", "-i" };
            //args = new string[] {"list", @"E:\Projects\Dependency\DependencyFinder\Test", "-n", "-c" };
            //args = new string[] { "list", @"E:\Projects\Dependency\DependencyFinder\Test", "-n", "-f" };
            //args = new string[] { "list", @"E:\Projects\Dependency\DependencyFinder\Test", "-p", "-m wpf" };
            //args = new string[] { "list", @"E:\Projects\Dependency\DependencyFinder\Test", "-n", "-g automa" };

            //args = new string[] {"nuget", @"E:\Projects\Dependency\DependencyFinder\Test" };
            //args = new string[] { "nuget", @"E:\Projects\Dependency\DependencyFinder\Test", "-i" };
            //args = new string[] { "nuget", @"E:\Projects\Dependency\DependencyFinder\Test", "-d" };

            //args = new string[] {"reference", @"E:\Projects\Dependency\DependencyFinder\Test" };

            await Parser.Default.ParseArguments<SolutionOptions, NugetOptions, ReferencesOptions>(args)
                                .MapResult((SolutionOptions so) => SolutionRun(so),
                                           (NugetOptions no) => NugetRun(no),
                                           (ReferencesOptions ro) => ReferenceRun(ro),
                                ErrorHandler);
        }

        private static Task SolutionRun(SolutionOptions so) => Task.Run(async () => await new SolutionRunner().Run(so));

        private static Task NugetRun(NugetOptions no) => Task.Run(async () => await new NugetRunner().Run(no));

        private static Task ReferenceRun(ReferencesOptions ro) => Task.Run(async () => await new ReferencesRunner().Run(ro));

        private static Task ErrorHandler(IEnumerable<Error> errors) => Task.CompletedTask;
    }
}