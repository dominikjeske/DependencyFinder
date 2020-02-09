using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.IO;

namespace DependencyFinder.UI.Models
{

    public class SourceCodesCollectionViewModel : TreeViewItemViewModel
    {
        public SourceCodesCollectionViewModel(TreeViewItemViewModel parent, IEnumerable<string> sources) : base(parent, false)
        {
            Name = $"Sources";
            HasPreview = false;

            foreach (var source in sources)
            {
                Children.Add(new SourceCodeViewModel(this, source));
            }
        }
    }
}