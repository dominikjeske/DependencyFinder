using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class MethodViewModel : MemberViewModel
    {
        public MethodViewModel(MethodMember member, TreeViewItemViewModel parent) : base(member, parent)
        {
        }
    }
}