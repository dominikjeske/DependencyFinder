using DependencyFinder.Core;
using DependencyFinder.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DependencyFinder
{
    public class SolutionRunner
    {
        public async Task Run(SolutionOptions so)
        {
            if (!Directory.Exists(so.RootPath))
            {
                ConsoleEx.WriteErrorLine($"Path {so.RootPath} not exists");
                return;
            }

            var sm = new SolutionManager(null);
            var solutions = sm.FindSolutions(so.RootPath);

            int i = 0;
            foreach (var s in solutions)
            {
                ConsoleEx.WriteOKLine($"{i}. {s.FullName}");
                if (so.ListProjects || so.ListNugets)
                {
                    var projects = await sm.OpenSolution(s.FullName);
                    foreach (var p in projects)
                    {
                        if (so.ShowOnlyCore && !p.IsNetCore || !so.ShowOnlyCore && p.IsNetCore)
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(so.ProjectName) && p.Name.IndexOf(so.ProjectName.Trim(), System.StringComparison.InvariantCultureIgnoreCase) == -1)
                        {
                            continue;
                        }

                        ConsoleEx.WriteTitleLine($"  - {p.Name}");

                        if (so.ListNugets)
                        {
                            foreach (var n in p.Nugets)
                            {
                                if (so.IgnoreSystemNugets && n.Name.IsSystemNuget())
                                {
                                    continue;
                                }

                                if (!string.IsNullOrWhiteSpace(so.NugetName) && n.Name.IndexOf(so.NugetName.Trim(), System.StringComparison.InvariantCultureIgnoreCase) == -1)
                                {
                                    continue;
                                }

                                ConsoleEx.WriteDebugLine($"    - {n.Name} [{n.Version}]");
                            }
                        }
                    }
                }
                i++;
            }
        }


       
    }


}