using DependencyFinder.Core;
using DependencyFinder.Utils;
using System.IO;
using System.Threading.Tasks;

namespace DependencyFinder
{
    public class ReferencesRunner
    {
        public async Task Run(ReferencesOptions no)
        {
            if (!Directory.Exists(no.RootPath))
            {
                ConsoleEx.WriteErrorLine($"Path {no.RootPath} not exists");
                return;
            }

            var sm = new SolutionManager();
            await sm.FindAllReferences();
            
        }
    }
}