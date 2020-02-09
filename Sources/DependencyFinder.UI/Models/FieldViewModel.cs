using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class FieldViewModel : MemberViewModel
    {
        public FieldViewModel(FieldMember member, TreeViewItemViewModel parent) : base(member, parent)
        {
        }
    }
}