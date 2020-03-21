using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace DependencyFinder.UI.Models
{
    /// <summary>
    /// Base class for all ViewModel classes displayed by TreeViewItems.
    /// This acts as an adapter between a raw data object and a TreeViewItem.
    /// </summary>
    public abstract class TreeViewItemViewModel : INotifyPropertyChanged
    {
        private static readonly TreeViewItemViewModel DummyChild = new DummyViewModel();

        private bool _isExpanded;
        private bool _isSelected;

        public bool HasPreview { get; set; } = true;

        public string Name { get; set; }
        public string FullName { get; set; }

        public bool SearchResult { get; set; }

        public bool HasSearchResult => SearchResult || Children.Any(x => x.HasSearchResult);

        public void CollapseAll()
        {
            IsExpanded = false;

            foreach (var child in Children)
            {
                child.IsExpanded = false;
            }
        }

        [Browsable(false)]
        public ObservableCollection<TreeViewItemViewModel> Children { get; set; }

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

        public TreeViewItemViewModel Filter(string text, SolutionFilterModel solutionFilterModel)
        {
            var filter = solutionFilterModel.Filters().FirstOrDefault(x => x.ViewType == GetType());

            var canFilter = filter?.IsSelected ?? false;

            var foundElement = (Name?.IndexOf(text, System.StringComparison.InvariantCultureIgnoreCase) > -1) || string.IsNullOrWhiteSpace(text);
            var visible = !canFilter || foundElement;

            // When we are not visible and there is no filtering in child we hide this node
            if (!visible && (filter?.HasChildFilter == false))
            {
                return null;
            }

            var childrens = Children?.Select(x => x.Filter(text, solutionFilterModel))
                                    ?.Where(y => y != null)
                                    ?.ToList();

            if (foundElement || childrens.Any(x => x.HasSearchResult))
            {
                var clone = MemberwiseClone() as TreeViewItemViewModel;
                clone.Children = new ObservableCollection<TreeViewItemViewModel>(childrens);
                clone.SearchResult = foundElement;

                if (childrens.Any(x => x.HasSearchResult))
                {
                    clone.IsExpanded = true;
                }

                return clone;
            }

            return null;
        }

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

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}