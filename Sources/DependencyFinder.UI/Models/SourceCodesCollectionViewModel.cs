using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DependencyFinder.UI.Models
{

    public class SourceCodesCollectionViewModel : TreeViewItemViewModel
    {
        public SourceCodesCollectionViewModel(TreeViewItemViewModel parent, IEnumerable<string> sources) : base(parent, false)
        {
            Name = $"Sources";
            HasPreview = false;

            foreach (var source in sources.OrderBy(x => x))
            {
                Children.Add(new SourceCodeViewModel(this, source));
            }
        }
    }
}