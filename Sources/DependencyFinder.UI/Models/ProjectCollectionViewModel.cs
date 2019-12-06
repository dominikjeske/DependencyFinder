﻿using DependencyFinder.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace DependencyFinder.UI.Models
{
    public class ProjectCollectionViewModel : TreeViewItemViewModel
    {
        public ProjectCollectionViewModel(IEnumerable<ProjectReference> projects, TreeViewItemViewModel parent, bool lazyLoadChildren) : base(parent, lazyLoadChildren)
        {
            Name = $"Projects [{projects.Count()}]";

            foreach (var project in projects)
            {
                var pvm = new ProjectRefViewModel(project, this, false);

                Children.Add(pvm);
            }
        }
    }
}