using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DependencyFinder.UI.Models
{
    public class ProjectViewModel : TreeViewItemViewModel
    {
        public ProjectViewModel(TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
        }   
    }

    public class SolutionViewModel : TreeViewItemViewModel
    {
        public SolutionViewModel(string solution, bool lazyLoadChildren = false) : base(null, lazyLoadChildren)
        {
            Name = Path.GetFileName(solution);
        }
    }
}
