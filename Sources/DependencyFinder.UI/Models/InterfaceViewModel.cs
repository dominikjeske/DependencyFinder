using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class InterfaceViewModel : TypeViewModel
    {
        public InterfaceViewModel(InterfaceDetails type, TreeViewItemViewModel parent) : base(type, parent)
        {
        }
    }
}