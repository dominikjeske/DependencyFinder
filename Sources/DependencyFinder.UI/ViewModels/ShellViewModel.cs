using Caliburn.Micro;
using DependencyFinder.Core;
using DependencyFinder.UI.Models;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace DependencyFinder.UI.ViewModels
{
    public class ShellViewModel : Screen, IDisposable
    {
        public string SolutionsRoot { get; set; }
        public ICollectionView Solutions { get; set; }

        public List<SolutionViewModel> SolutionsCache { get; set; } = new List<SolutionViewModel>();

        public ObservableCollection<DocumentViewModel> OpenDocuments { get; set; } = new ObservableCollection<DocumentViewModel>();
        public ObservableCollection<Reference> FindReferencesResult { get; set; } = new ObservableCollection<Reference>();
        public string Filter { get; set; }
        public TreeViewItemViewModel SelectedSolutionItem { get; set; }
        public DocumentViewModel ActiveDocument { get; set; }
        public string SolutionsStatus { get; set; }
        public Reference SelectedSearchResult { get; set; }


        private readonly ISolutionManager _solutionManager;
        private readonly Notifier _notifier;

        public bool IsSearching { get; set; }
        private DispatcherTimer _searchTimer = new DispatcherTimer();

        public ShellViewModel(ISolutionManager solutionManager)
        {
            //TODO fix after testing
            //SolutionsRoot = Path.Combine((new DirectoryInfo(Directory.GetCurrentDirectory())).Parent.Parent.Parent.Parent.Parent.ToString(), "Test");
            SolutionsRoot = @"C:\Source\ArcheoFork\humbak_archeo";

            _solutionManager = solutionManager;
            _searchTimer.Interval = TimeSpan.FromMilliseconds(500);
            _searchTimer.Tick += SearchTimer_Tick;

            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.TopRight,
                    offsetX: 10,
                    offsetY: 50);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });
        }

      

        public void OnLoaded()
        {
            _notifier.ShowInformation("Start loading solutions");
            Solutions = GetLoadingView();

            Task.Run(() => LoadSolutions());
        }

        public override Task TryCloseAsync(bool? dialogResult = null)
        {
            Dispose();

            return base.TryCloseAsync(dialogResult);
        }

        private async Task LoadSolutions()
        {
            try
            {
                var solutions = _solutionManager.FindSolutions(SolutionsRoot);
                var list = new List<SolutionViewModel>();

                await foreach (var s in solutions)
                {
                    var projects = await _solutionManager.ReadSolution(s);
                    var solutionViewModel = new SolutionViewModel(s, _solutionManager, false);
                    foreach (var p in projects)
                    {
                        solutionViewModel.AddProject(p);
                    }
                    list.Add(solutionViewModel);
                }

                foreach (var solution in list)
                {
                    foreach (ProjectViewModel project in solution.Children)
                    {
                        foreach (var reference in _solutionManager.GetReferencingProjects(project.Project))
                        {
                            project.References.AddReference(reference);
                        }
                    }
                }

                await Application.Current.Dispatcher.BeginInvoke((System.Action)(() =>
                {
                    SolutionsCache = list;
                    GetFinalView();
                    
                    SolutionsStatus = $"Solutions loaded: {SolutionsCache.Count} | Projects loaded: {_solutionManager.GetNumberOfCachedProjects()}";
                    _notifier.ShowInformation("Solution loaded");
                }));
            }
            catch (Exception ee)
            {
                await Application.Current.Dispatcher.BeginInvoke((System.Action)(() =>
                {
                    _notifier.ShowError("Solution loading failed");
                }));
            }
        }

        public void OnFilterChanged()
        {
            if (Filter.Length < 3) return;

            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            IsSearching = true;
            _searchTimer.Stop();
            _notifier.ShowInformation("Searching..");

            Solutions = GetLoadingView();

            Task.Run(() => ValidateList(SolutionsCache)).ContinueWith(_ =>
            {
                GetFinalView();

                _notifier.ShowInformation("Search done");
                IsSearching = false;
            });
        }

        private void GetFinalView()
        {
            var source = CollectionViewSource.GetDefaultView(SolutionsCache);
            source.Filter = item => ((TreeViewItemViewModel)item).IsVisible;
            source.Refresh();
            Solutions = source;
        }

        private ICollectionView GetLoadingView()
        {
            return CollectionViewSource.GetDefaultView(new List<TreeViewItemViewModel> { new SolutionViewModel("Loading...", _solutionManager) });
        }

        public void OnSelectedSolutionItemChanged()
        {
            if (!(SelectedSolutionItem?.HasPreview ?? false)) return;

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
            //TODO
            //Filter = "";
            SearchTimer_Tick(null, null);
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
            else if (SelectedSolutionItem is NugetReferenceViewModel nugetRef)
            {
                ShowProject(nugetRef.Map.Project.Solution, nugetRef.Map.Project.Name);
            }
        }

        private void ShowProject(string solutionPath, string projectName)
        {
            //TODO check if this is actual after changing to collectionView
            var solution = SolutionsCache.Single(x => x.FullName == solutionPath);
            solution.IsExpanded = true;

            var project = solution.Children.FirstOrDefault(x => x.Name == projectName);
            project.IsExpanded = true;
            project.IsSelected = true;

            SelectedSolutionItem = solution.Children.FirstOrDefault(x => x.Name == projectName);
        }

        public bool CanGoToProjectClick => SelectedSolutionItem is ReferencedViewModel
                                        || SelectedSolutionItem is ProjectRefViewModel
                                        || SelectedSolutionItem is NugetReferenceViewModel;

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

        public void Dispose()
        {
            _notifier.Dispose();
        }
    }
}