using Caliburn.Micro;
using DependencyFinder.Core;
using DependencyFinder.UI.Models;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyFinder.UI.ViewModels
{
    public class ShellViewModel : Screen
    {
        public string SolutionsRoot { get; set; }
        public ObservableCollection<SolutionViewModel> Solutions { get; set; } = new ObservableCollection<SolutionViewModel>();
        public ObservableCollection<DocumentViewModel> OpenDocuments { get; set; } = new ObservableCollection<DocumentViewModel>();
        public string Filter { get; set; }
        public TreeViewItemViewModel SelectedSolutionItem { get; set; }
        public DocumentViewModel ActiveDocument { get; set; }

        private readonly ISolutionManager _solutionManager;

        public ShellViewModel(ISolutionManager solutionManager)
        {
            SolutionsRoot = Path.Combine((new DirectoryInfo(Directory.GetCurrentDirectory())).Parent.Parent.Parent.Parent.Parent.ToString(), "Test");
            //SolutionsRoot = @"E:\Projects\Dependency\DependencyFinder\Test\WPF2\WpfAppStandalone";

            _solutionManager = solutionManager;
        }

        private async Task LoadSolutions()
        {
            var solutions = _solutionManager.FindSolutions(SolutionsRoot);

            await foreach (var s in solutions)
            {
                var solutionViewModel = new SolutionViewModel(s, _solutionManager, false);
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

        public void OnSelectedSolutionItemChanged()
        {
            var alreadyOpenDocument = OpenDocuments.FirstOrDefault(d => d.AssociatedModel == SelectedSolutionItem);
            if (alreadyOpenDocument != null)
            {
                ActiveDocument = alreadyOpenDocument;
                return;
            }

            var currentTemporary = OpenDocuments.FirstOrDefault(d => d.IsTemporary);

            var document = OpenDocument(SelectedSolutionItem);

            if (currentTemporary == null)
            {
                currentTemporary = new DocumentViewModel
                {
                    IsTemporary = true
                };

                OpenDocuments.Add(currentTemporary);
            }

            currentTemporary.AssociatedModel = document.AssociatedModel;
            currentTemporary.Content = document.Content;
            currentTemporary.Syntax = document.Syntax;
            currentTemporary.Title = $"*{document.Title}";

            ActiveDocument = currentTemporary;
        }

        public void FilterClearClick()
        {
            Filter = "";
        }

        public void ExploreClick()
        {
            var path = Path.GetDirectoryName(SelectedSolutionItem.FullName);
            Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", path);
        }

        public bool CanExploreClick => !string.IsNullOrWhiteSpace(SelectedSolutionItem?.FullName) && Directory.Exists(Path.GetDirectoryName(SelectedSolutionItem.FullName));

        public void OpenClick()
        {
            Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", SelectedSolutionItem.FullName);
        }

        public bool CanOpenClick => !string.IsNullOrWhiteSpace(SelectedSolutionItem?.FullName);

        public void FindClick()
        {

        }

        public bool CanFindClick => SelectedSolutionItem is TypeViewModel || SelectedSolutionItem is MemberViewModel;

        public void ProjectDoubleClick()
        {
            var alreadyOpenDocument = OpenDocuments.FirstOrDefault(d => d.AssociatedModel == SelectedSolutionItem);

            if (alreadyOpenDocument?.IsTemporary == true)
            {
                OpenDocuments.Remove(alreadyOpenDocument);
                alreadyOpenDocument = null;
            }

            if (alreadyOpenDocument != null)
            {
                ActiveDocument = alreadyOpenDocument;
            }
            else
            {
                var document = OpenDocument(SelectedSolutionItem);

                OpenDocuments.Add(document);
                ActiveDocument = document;
            }
        }

       

        private DocumentViewModel OpenDocument(TreeViewItemViewModel model)
        {
            if (model == null) return DocumentViewModel.Empty;

            var filePath = model.FullName;

            if (string.IsNullOrWhiteSpace(filePath)) return DocumentViewModel.Empty;
            if (!File.Exists(filePath)) return DocumentViewModel.Empty;

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (StreamReader reader = FileReader.OpenStream(fs, Encoding.UTF8))
                {
                    var syntax = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(filePath));
                    var content = new TextDocument(reader.ReadToEnd());
                    var title = Path.GetFileName(filePath);

                    var document = new DocumentViewModel
                    {
                        AssociatedModel = model,
                        Content = content,
                        Syntax = syntax,
                        Title = title
                    };

                    return document;

                    //TODO read only support
                    //if ((System.IO.File.GetAttributes(this._filePath) & FileAttributes.ReadOnly) != 0)
                    //{
                    //    this.IsReadOnly = true;
                    //    this.IsReadOnlyReason = "This file cannot be edit because another process is currently writting to it.\n" +
                    //                            "Change the file access permissions or save the file in a different location if you want to edit it.";
                    //}
                }
            }
        }

        private bool ValidateList(IEnumerable<TreeViewItemViewModel> list)
        {
            //TODO check this
            if (list == null) return false;

            bool anyChildVisible = false;

            foreach (var item in list)
            {
                if (ValidateItem(item))
                {
                    anyChildVisible = true;
                }
            }

            return anyChildVisible;
        }

        private bool ValidateItem(TreeViewItemViewModel item)
        {
            var anyChildVisible = ValidateList(item.Children);

            var shouldBeVisible = item.Name?.IndexOf(Filter, StringComparison.InvariantCultureIgnoreCase) > -1;

            item.IsVisible = shouldBeVisible || anyChildVisible;

            if (item.IsVisible && item.Parent != null)
            {
                item.Parent.IsExpanded = true;
            }

            return item.IsVisible;
        }
    }
}