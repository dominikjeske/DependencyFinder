using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public interface IProjectInfo
    {
        ProjectDetails Project { get; }

        public string Name { get; }
        public string FullName { get; }
    }
}