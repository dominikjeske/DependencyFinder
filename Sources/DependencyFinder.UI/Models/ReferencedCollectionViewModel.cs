using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public class ReferencedCollectionViewModel : TreeViewItemViewModel
    {
        public ReferencedCollectionViewModel(TreeViewItemViewModel parent) : base(parent, false)
        {
            RefreshName();
            HasPreview = false;
            CanBeFiltered = false;
        }

        public void AddReference(ProjectDetails referencedProject)
        {
            Children.Add(new ReferencedViewModel(this, referencedProject));

            RefreshName();
        }

        private void RefreshName()
        {
            Name = $"Used By [{Children.Count}]";
        }
    }
}