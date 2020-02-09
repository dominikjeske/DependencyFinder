using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public abstract class MemberViewModel : TreeViewItemViewModel
    {
        public MemberViewModel(Member member, TreeViewItemViewModel parent) : base(parent, false)
        {
            Name = member.Name;
            FullName = member.Name;
        }
    }
}