using ByteDev.DotNet.Project;
using DependencyFinder.Core.Models;

namespace DependencyFinder
{
    public static class DotNetProjectExtensions
    {
        public static AssemblyInfo GetAssemblyInfo(this DotNetProject projectInfo)
        {
            return new AssemblyInfo
            {
                Company = projectInfo.AssemblyInfo.Company,
                Configuration = projectInfo.AssemblyInfo.Configuration,
                Copyright = projectInfo.AssemblyInfo.Copyright,
                Description = projectInfo.AssemblyInfo.Description,
                FileVersion = projectInfo.AssemblyInfo.FileVersion,
                InformationalVersion = projectInfo.AssemblyInfo.InformationalVersion,
                NeutralLanguage = projectInfo.AssemblyInfo.NeutralLanguage,
                Product = projectInfo.AssemblyInfo.Product,
                AssemblyTitle = projectInfo.AssemblyInfo.Title,
                AssemblyVersion = projectInfo.AssemblyInfo.Version,
                AssemblyName = projectInfo.AssemblyInfo.AssemblyName
            };
        }
    }
}