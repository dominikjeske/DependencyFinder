using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace DependencyFinder.UI.Models
{
    /// <summary>
    /// Base class for all ViewModel classes displayed by TreeViewItems.
    /// This acts as an adapter between a raw data object and a TreeViewItem.
    /// </summary>
    public class TreeViewItemViewModel : INotifyPropertyChanged
    {
        private static readonly TreeViewItemViewModel DummyChild = new TreeViewItemViewModel();
        
        private bool _isExpanded;
        private bool _isSelected;

        [Browsable(false)]
        public bool IsVisible { get; set; } = true;

        public bool HasPreview { get; set; } = true;

        public string Name { get; set; }
        public string FullName { get; set; }

        protected TreeViewItemViewModel(TreeViewItemViewModel parent, bool lazyLoadChildren)
        {
            Parent = parent;

            Children = new ObservableCollection<TreeViewItemViewModel>();

            if (lazyLoadChildren)
                Children.Add(DummyChild);
        }

        private TreeViewItemViewModel()
        {
        }

        protected virtual void LoadChildren()
        {
        }

        [Browsable(false)]
        public ObservableCollection<TreeViewItemViewModel> Children { get; }

        [Browsable(false)]
        public bool HasDummyChild => Children.Count == 1 && Children[0] == DummyChild;

        [Browsable(false)]
        public TreeViewItemViewModel Parent { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [Browsable(false)]
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }

                // Expand all the way up to the root.
                if (_isExpanded && Parent != null)
                    Parent.IsExpanded = true;

                // Lazy load the child items, if necessary.
                if (HasDummyChild)
                {
                    Children.Remove(DummyChild);
                    LoadChildren();
                }
            }
        }

        [Browsable(false)]
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        //public TreeViewItemViewModel Filter(string filter)
        //{
        //    var children = Children.Select(child => child.Filter(filter));

        //    if(children != null || children.Count() > 0)
        //}

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}