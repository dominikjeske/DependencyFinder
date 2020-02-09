using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class StructViewModel : TypeViewModel
    {
        public StructViewModel(StructDetails type, TreeViewItemViewModel parent) : base(type, parent)
        {
        }
    }
}