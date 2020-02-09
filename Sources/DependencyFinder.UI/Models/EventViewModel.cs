using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class EventViewModel : MemberViewModel
    {
        public EventViewModel(EventMember member, TreeViewItemViewModel parent) : base(member, parent)
        {
        }
    }
}