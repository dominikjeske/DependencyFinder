namespace DependencyFinder.UI.Models
{
    public class WaitingViewModel : TreeViewItemViewModel
    {
        public WaitingViewModel(TreeViewItemViewModel parent) : base(parent, false)
        {
            Name = "Loading...";
        }
    }
}