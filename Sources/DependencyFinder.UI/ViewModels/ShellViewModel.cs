using Caliburn.Micro;
using DependencyFinder.Core;
using DependencyFinder.UI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DependencyFinder.UI.ViewModels
{

    public class ShellViewModel : Screen
    {
        public string SolutionsRoot { get; set; }
        public ObservableCollection<SolutionViewModel> Solutions { get; set; } = new ObservableCollection<SolutionViewModel>();
        public string Filter { get; set; }
        public TreeViewItemViewModel SelectedSolutionItem { get; set; }

        private ICollectionView _collectionView;
        private readonly ISolutionManager _solutionManager;

        public ShellViewModel(ISolutionManager solutionManager)
        {
            SolutionsRoot = @"E:\Projects\Dependency\DependencyFinder\Test";

            _solutionManager = solutionManager;
            _collectionView = CollectionViewSource.GetDefaultView(Solutions);
            _collectionView.Filter = FilterPredicate;
        }

        private async Task LoadSolutions()
        {
            var solutions = _solutionManager.FindSolutions(SolutionsRoot);

            await foreach (var s in solutions)
            {
                var solutionViewModel = new SolutionViewModel(s);
                Solutions.Add(solutionViewModel);

                var projects = await _solutionManager.OpenSolution(s);
                foreach (var p in projects)
                {
                    solutionViewModel.AddProject(p);
                }
            }
        }

        public async Task OnLoaded()
        {
            await LoadSolutions();
        }

        public void OnFilterChanged()
        {
            ValidateList(Solutions);
        }

        public void FilterClearClick()
        {
            Filter = "";
        }

        public void ExploreClick()
        {
            try
            {
                var path = Path.GetDirectoryName(SelectedSolutionItem.FullName);
                Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", path);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString());
            }
        }

        public bool CanExploreClick => !string.IsNullOrWhiteSpace(SelectedSolutionItem?.FullName) && Directory.Exists(Path.GetDirectoryName(SelectedSolutionItem.FullName));
        

        private bool ValidateList(IEnumerable<TreeViewItemViewModel> list)
        {
            bool anyChildVisible = false;

            foreach (var item in list)
            {
                if(ValidateItem(item))
                {
                    anyChildVisible = true;
                }
            }

            return anyChildVisible;
        }

        private bool ValidateItem(TreeViewItemViewModel item)
        {
            var anyChildVisible = ValidateList(item.Children);

            var shouldBeVisible = item.Name.IndexOf(Filter, StringComparison.InvariantCultureIgnoreCase) > -1;

            item.IsVisible = shouldBeVisible || anyChildVisible;

            if(item.IsVisible && item.Parent != null)
            {
                item.Parent.IsExpanded = true;
            }

            return item.IsVisible;
        }

        public bool FilterPredicate(object item)
        {
            TreeViewItemViewModel treeItem = (TreeViewItemViewModel)item;

            return true;
        }
    }
}