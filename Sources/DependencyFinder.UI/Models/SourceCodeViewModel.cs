using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.IO;

namespace DependencyFinder.UI.Models
{

    public class SourceCodeViewModel : TreeViewItemViewModel
    {
        public SourceCodeViewModel(TreeViewItemViewModel parent, string sourceCode) : base(parent, false)
        {
            Name = $"{Path.GetFileName(sourceCode)}";
            FullName = $"{sourceCode}";
        }
    }
}