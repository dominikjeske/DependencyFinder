using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class PropertyViewModel : MemberViewModel
    {
        public PropertyViewModel(PropertyMember member, TreeViewItemViewModel parent) : base(member, parent)
        {
        }
    }
}