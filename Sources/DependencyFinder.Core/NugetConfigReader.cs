using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DependencyFinder.Core
{
    public static class NugetConfigReader
    {
        public static async Task<IEnumerable<NugetPackage>> GetNugetPackages(string projectPath)
        {
            var projectDirectory = Path.GetDirectoryName(projectPath);
            var packagesConfig = Path.Combine(projectDirectory, "packages.config");

            if (File.Exists(packagesConfig))
            {
                var projectFile = await File.ReadAllTextAsync(packagesConfig);
                var doc = XDocument.Parse(projectFile);
                var packageReferences = doc.XPathSelectElements("//package")
                    .Select(pr => new NugetPackage
                    {
                        Name = pr.Attribute("id").Value,
                        Version = VersionEx.FromString(pr.Attribute("version").Value),
                        TargetFramework = pr.Attribute("targetFramework").Value
                    });

                return packageReferences;
            }
            else
            {
                var projectFile = await File.ReadAllTextAsync(projectPath);
                var doc = XDocument.Parse(projectFile);
                var packageReferences = doc.XPathSelectElements("//PackageReference")
                    .Select(pr => new NugetPackage
                    {
                        Name = pr.Attribute("Include").Value,
                        Version = VersionEx.FromString(pr.Attribute("Version").Value)
                    });

                return packageReferences;
            }
        }
    }
}