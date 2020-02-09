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

        public ObservableCollection<Reference> FindReferencesResult { get; set; } = new ObservableCollection<Reference>();
        public string Filter { get; set; }
        public TreeViewItemViewModel SelectedSolutionItem { get; set; }
        public DocumentViewModel ActiveDocument { get; set; }

        public Reference SelectedSearchResult { get; set; }

        private readonly ISolutionManager _solutionManager;

        public ShellViewModel(ISolutionManager solutionManager)
        {
            //TODO fix after testing
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

                var projects = await _solutionManager.ReadSolution(s);
                foreach (var p in projects)
                {
                    solutionViewModel.AddProject(p);
                }
            }

            foreach (var solution in Solutions)
            {
                foreach (ProjectViewModel project in solution.Children)
                {
                    foreach (var reference in _solutionManager.GetReferencingProjects(project.Project))
                    {
                        project.References.AddReference(reference);
                    }
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
            if (!SelectedSolutionItem.HasPreview) return;

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

        public async Task FindClick()
        {
            FindReferencesResult.Clear();

            if (SelectedSolutionItem is TypeViewModel type)
            {
                var project = (SelectedSolutionItem.Parent.Parent as ProjectViewModel)?.Project;

                await foreach (var reference in _solutionManager.FindReferenceInSolutions(project, type.TypeDetails.Symbol))
                {
                    FindReferencesResult.Add(reference);
                }
            }
            else if (SelectedSolutionItem is MemberViewModel member)
            {
                var project = (SelectedSolutionItem.Parent.Parent.Parent as ProjectViewModel)?.Project;

                await foreach (var reference in _solutionManager.FindReferenceInSolutions(project, member.Member.Symbol))
                {
                    FindReferencesResult.Add(reference);
                }
            }
        }

        public bool CanFindClick => SelectedSolutionItem is TypeViewModel || SelectedSolutionItem is MemberViewModel;

        public void ProjectDoubleClick()
        {
            if (!SelectedSolutionItem.HasPreview) return;

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

        public void SearchResultDoubleClick()
        {
            var document = OpenFile(null, SelectedSearchResult.FilePath);

            document.SelectionStart = SelectedSearchResult.SelectionStart;
            document.SelectionLength = SelectedSearchResult.SelectionLenght;

            OpenDocuments.Add(document);
            ActiveDocument = document;
        }

        public void GoToProjectClick()
        {
            if (SelectedSolutionItem is ReferencedViewModel referenced)
            {
                ShowProject(referenced.Solution, referenced.Name);
            }
            else if (SelectedSolutionItem is ProjectRefViewModel projectRef)
            {
                ShowProject(projectRef.Parent.Parent.Parent.FullName, projectRef.Name);
            }
        }

        private void ShowProject(string solutionPath, string projectName)
        {
            var solution = Solutions.Single(x => x.FullName == solutionPath);
            solution.IsExpanded = true;

            var project = solution.Children.FirstOrDefault(x => x.Name == projectName);
            project.IsExpanded = true;
            project.IsSelected = true;

            SelectedSolutionItem = solution.Children.FirstOrDefault(x => x.Name == projectName);
        }

        public bool CanGoToProjectClick => SelectedSolutionItem is ReferencedViewModel || SelectedSolutionItem is ProjectRefViewModel;

        private DocumentViewModel OpenDocument(TreeViewItemViewModel model)
        {
            if (model == null) return DocumentViewModel.Empty;

            var filePath = model.FullName;
            return OpenFile(model, filePath);
        }

        private static DocumentViewModel OpenFile(TreeViewItemViewModel model, string filePath)
        {
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