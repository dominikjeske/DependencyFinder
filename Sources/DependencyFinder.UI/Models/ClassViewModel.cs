using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class ClassViewModel : TypeViewModel
    {
        public ClassViewModel(ClassDetails type, TreeViewItemViewModel parent) : base(type, parent)
        {
        }
    }
}