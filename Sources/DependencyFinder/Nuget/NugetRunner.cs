using Alba.CsConsoleFormat;
using DependencyFinder.Core;
using DependencyFinder.Core.Models;
using DependencyFinder.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.ConsoleColor;

namespace DependencyFinder
{
    public class NugetRunner
    {
        public async Task Run(NugetOptions no)
        {
            if (string.IsNullOrWhiteSpace(no.RootPath))
            {
                no.RootPath = Directory.GetCurrentDirectory();
            }
            if (!Directory.Exists(no.RootPath))
            {
                ConsoleEx.WriteErrorLine($"Path {no.RootPath} not exists");
                return;
            }

            var sm = new SolutionManager(null);

            var projects = new List<Project>();
            await foreach(var solution in sm.FindSolutions(no.RootPath))
            {
                projects.AddRange(await sm.OpenSolution(solution));
            }

            var nugets = projects.Distinct().SelectMany(p => p.Nugets.Select(n => new
            {
                Name = n.Name,
                Version = n.Version,
                Project = p.Name
            }));

            if (no.IgnoreSystemNugets)
            {
                nugets = nugets.Where(x => !x.Name.IsSystemNuget());
            }

            var grouped = nugets.GroupBy(n => n.Name).OrderBy(n => n.Key).Select(nuget => new NugetGroup
            {
                Min = nuget.Min(n => n.Version),
                Max = nuget.Max(n => n.Version),
                Count = nuget.Count(),
                Name = nuget.Key
            });

            if (no.OnlyDiff)
            {
                grouped = grouped.Where(x => x.Min != x.Max);
            }

            var headerThickness = new LineThickness(LineWidth.Double, LineWidth.Single);

            var doc = new Document(new Span("Nugets:") { Color = Yellow }, "\n",
                                    new Grid
                                    {
                                        Color = Gray,
                                        Columns = { GridLength.Star(1), GridLength.Auto, GridLength.Auto, GridLength.Auto },
                                        Children = {
                                            new Cell("Name") { Stroke = headerThickness},
                                            new Cell("Min") { Stroke = headerThickness },
                                            new Cell("Max") { Stroke = headerThickness },
                                            new Cell("Count") { Stroke = headerThickness },
                                            grouped.Select(item => new[]
                                            {
                                                new Cell(item.Name) { Color = ColumnColor(item) },
                                                new Cell(item.Min) { Color = ColumnColor(item) },
                                                new Cell(item.Max) { Color = ColumnColor(item) },
                                                new Cell(item.Count) { Color = ColumnColor(item) },
                                            })
                                        }
                                    }
                                );

            ConsoleRenderer.RenderDocument(doc);
        }

        private ConsoleColor ColumnColor(NugetGroup nugetGroup)
        {
            if (nugetGroup.Min != nugetGroup.Max) return ConsoleColor.Red;
            return ConsoleColor.Gray;
        }
    }

    public class NugetGroup
    {
        public VersionEx Min { get; set; }
        public VersionEx Max { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }
}