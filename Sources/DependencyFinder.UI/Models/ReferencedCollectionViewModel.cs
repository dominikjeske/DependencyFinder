using DependencyFinder.Core;
using DependencyFinder.Core.Models;
using System.Collections;
using System.IO;
using System.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DependencyFinder.UI.Models
{

    public class ReferencedCollectionViewModel : TreeViewItemViewModel
    {
        public ReferencedCollectionViewModel(TreeViewItemViewModel parent) : base(parent, false)
        {
            RefreshName();
            HasPreview = false;
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