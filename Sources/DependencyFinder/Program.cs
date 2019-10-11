using CommandLine;
using DependencyFinder.Core;
using DependencyFinder.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DependencyFinder
{
    internal static class Program
    {
        private static void AddFiles(string path, string pattern, IList<string> files)
        {
            try
            {
                Directory.GetFiles(path, pattern)
                    .ToList()
                    .ForEach(s => files.Add(s));

                Directory.GetDirectories(path)
                    .ToList()
                    .ForEach(s => AddFiles(s, pattern, files));
            }
            catch (UnauthorizedAccessException ex)
            {
                // ok, so we are not allowed to dig into that directory. Move on.
            }
        }

        private static async Task Main(string[] args)
        {
            var sw = new Stopwatch();
            var test = new AsyncFileFinder();

            sw.Reset();
            sw.Start();

            int i = 0;
            await foreach(var sln in test.Find(@"c:\", "*.dll", Enumerable.Empty<string>()))
            {
                i++;
            }

            sw.Stop();

            Console.WriteLine($"Async: {sw.Elapsed.TotalSeconds} found {i} files");


            sw.Reset();
            sw.Start();

            var list = new List<string>();
            AddFiles(@"c:\", "*.dll", list);

            sw.Stop();

            Console.WriteLine($"Sync: {sw.Elapsed.TotalSeconds} found {list.Count} files");


            sw.Reset();
            sw.Start();

            var searcher = new SolutionSearcher();
            var result = await searcher.Search(@"c:\");
            
            sw.Stop();

            Console.WriteLine($"Async2: {sw.Elapsed.TotalSeconds} found {result.Count()} files");

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

            return;
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