using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public abstract class MemberViewModel : TreeViewItemViewModel
    {
        public Member Member { get; }

        public MemberViewModel(Member member, TreeViewItemViewModel parent) : base(parent, false)
        {
            Member = member;
            Name = member.Name;
            FullName = member.Name;
        }
    }
}