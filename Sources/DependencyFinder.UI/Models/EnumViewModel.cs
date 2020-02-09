using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class EnumViewModel : TypeViewModel
    {
        public EnumViewModel(EnumDetails type, TreeViewItemViewModel parent) : base(type, parent)
        {
        }
    }
}