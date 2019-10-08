using net.r_eg.MvsSln;
using net.r_eg.MvsSln.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleFull
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var sln = new Sln( @"E:\Projects\DependencyFinder\Test\ConsoleApp\ConsoleApp.sln", SlnItems.All))
            {
                //sln.Result.Env.XProjectByGuid(
                //    sln.Result.ProjectDependencies.FirstBy(BuildType.Rebuild).pGuid,
                //    new ConfigItem("Debug", "Any CPU")
                //);

                //var p = slnEnv.GetOrLoadProject(
                //    sln.ProjectItems.FirstOrDefault(p => p.name == name)
                //);

                //var paths = sln.Result.ProjectItems
                //                        .Select(p => new { p.pGuid, p.fullPath })
                //                        .ToDictionary(p => p.pGuid, p => p.fullPath);

                // {[{27152FD4-7B94-4AF0-A7ED-BE7E7A196D57}, D:\projects\Conari\Conari\Conari.csproj]}
                // {[{0AEEC49E-07A5-4A55-9673-9346C3A7BC03}, D:\projects\Conari\ConariTest\ConariTest.csproj]}

                foreach (IXProject xp in sln.Result.Env.Projects)
                {
                    
                }

                
            } // release all loaded projects
        }
    }
}
