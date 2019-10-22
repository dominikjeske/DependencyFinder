using DependencyFinder.Core;
using System.IO;
using System.Threading.Tasks;

namespace DependencyFinder
{
    public class SolutionRunner
    {
        public async Task Run(SolutionOptions so)
        {
            if (string.IsNullOrWhiteSpace(so.RootPath))
            {
                so.RootPath = Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(so.RootPath))
            {
                ConsoleEx.WriteErrorLine($"Path {so.RootPath} not exists");
                return;
            }

            var sm = new SolutionManager(null);
            var solutions = sm.FindSolutions(so.RootPath);

            int i = 0;
            await foreach (var s in solutions)
            {
                bool solution = false;
                
                if (so.ListProjects || so.ListNugets)
                {
                    var projects = await sm.OpenSolution(s);
                    foreach (var p in projects)
                    {
                        if ((so.ShowOnlyCore && !p.IsNetCore) || (so.ShowOnlyFull && p.IsNetCore))
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(so.ProjectName) && p.Name.IndexOf(so.ProjectName.Trim(), System.StringComparison.InvariantCultureIgnoreCase) == -1)
                        {
                            continue;
                        }

                        if(so.ListProjects)
                        {
                            if(!solution)
                            {
                                solution = true;
                                ConsoleEx.WriteOKLine($"{i}. {s}");
                            }

                            ConsoleEx.WriteTitleLine($"  - {p.Name}");
                        }
                        else if (so.ListNugets)
                        {
                            bool project = false;

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

                                if (!solution)
                                {
                                    solution = true;
                                    ConsoleEx.WriteOKLine($"{i}. {s}");
                                }
                                if (!project)
                                {
                                    project = true;
                                    ConsoleEx.WriteTitleLine($"  - {p.Name}");
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