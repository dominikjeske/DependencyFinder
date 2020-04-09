using DependencyFinder.Core;
using DependencyFinder.UI.Models;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Utils;
using Notifications.Wpf.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DependencyFinder.UI.ViewModels
{
    public class ShellViewModel : Caliburn.Micro.Screen
    {
        private readonly ISolutionManager _solutionManager;
        private readonly NotificationManager _notificationManager = new NotificationManager();
        private readonly DispatcherTimer _searchTimer = new DispatcherTimer();
        private readonly ILogger _logger;

        public string SolutionsRoot { get; set; }
        public string Filter { get; set; }
        public TreeViewItemViewModel SelectedSolutionItem { get; set; }
        public DocumentViewModel ActiveDocument { get; set; }
        public string SolutionsStatus { get; set; }
        public Reference SelectedSearchResult { get; set; }
        public bool IsSearching { get; set; }
        public bool IsFindAllReferencesVisible { get; set; }
        public int RibbonSelectedTabIndex { get; set; }
        public SolutionFilterModel SolutionTreeFilter { get; set; } = new SolutionFilterModel();

        public IEnumerable<TreeViewItemViewModel> Solutions { get; set; } = new List<SolutionViewModel>();
        public List<SolutionViewModel> SolutionsCache { get; set; } = new List<SolutionViewModel>();
        public ObservableCollection<DocumentViewModel> OpenDocuments { get; set; } = new ObservableCollection<DocumentViewModel>();
        public ObservableCollection<Reference> FindReferencesResult { get; set; } = new ObservableCollection<Reference>();

        public ObservableCollection<ErrorViewModel> Errors { get; set; } = new ObservableCollection<ErrorViewModel>();
        public ObservableCollection<IProjectInfo> SelectedProjects { get; set; } = new ObservableCollection<IProjectInfo>();

        #region Init

        public ShellViewModel(ISolutionManager solutionManager, AppSettings appSettings, ILogger logger)
        {
            if (Debugger.IsAttached)
            {
                SolutionsRoot = Path.Combine((new DirectoryInfo(Directory.GetCurrentDirectory())).Parent.Parent.Parent.Parent.Parent.ToString(), "Test");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(appSettings.SolutionsLocation) && Directory.Exists(appSettings.SolutionsLocation))
                {
                    SolutionsRoot = appSettings.SolutionsLocation;
                }
                else
                {
                    OpenSolutionFolderPicker();
                }
            }

            _solutionManager = solutionManager;
            _searchTimer.Interval = TimeSpan.FromMilliseconds(500);
            _searchTimer.Tick += SearchTimer_Tick;
            SolutionTreeFilter.FilterChanged += SolutionTreeFilter_FilterChanged;
            _logger = logger;
            _logger.Error += _logger_Error;
        }

        private void _logger_Error(Exception obj)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() => Errors.Add(new ErrorViewModel { Message = obj.Message, StackTrace = obj.StackTrace })));
        }

        public void OnLoaded()
        {
            StartLoadingSolutions();
        }

        private void StartLoadingSolutions()
        {
            ShowToastInfo("Start loading solutions");

            Task.Run(LoadSolutions);
        }

        private async void ShowToastInfo(string text)
        {
            var content = new NotificationContent { Title = "", Message = text, Type = NotificationType.Information };

            await _notificationManager.ShowAsync(content, "WindowArea");
        }

        private async void ShowToastWarrning(string text)
        {
            var content = new NotificationContent { Title = "", Message = text, Type = NotificationType.Warning };

            await _notificationManager.ShowAsync(content, "WindowArea");
        }

        private async void ShowToastError(string text)
        {
            var content = new NotificationContent { Title = "", Message = text, Type = NotificationType.Error };

            await _notificationManager.ShowAsync(content, "WindowArea");
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

                await System.Windows.Application.Current.Dispatcher.BeginInvoke((System.Action)(() =>
                {
                    SolutionsCache = list.OrderBy(x => x.Name).ToList();
                    Solutions = SolutionsCache;

                    SolutionsStatus = $"Solutions: {SolutionsCache.Count} | Projects: {_solutionManager.GetNumberOfCachedProjects()} | Location: {SolutionsRoot}";
                    ShowToastInfo("Solution loaded");
                }));
            }
            catch (Exception ee)
            {
                _logger.LogError(ee);

                await System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() => ShowToastError("Solution loading failed")));
            }
        }

        #endregion Init

        #region Ribbon

        public void LocationClick()
        {
            OpenSolutionFolderPicker();
        }

        private void OpenSolutionFolderPicker()
        {
            using var fbd = new System.Windows.Forms.FolderBrowserDialog();
            var result = fbd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                SolutionsRoot = fbd.SelectedPath;
                StartLoadingSolutions();
            }
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
            ShowToastInfo("Searching for references");

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

            ShowToastInfo("Searching finish");
            IsFindAllReferencesVisible = true;
        }

        public bool CanFindClick => SelectedSolutionItem is TypeViewModel || SelectedSolutionItem is MemberViewModel;

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

        public bool CanAddProjectClick => SelectedSolutionItem is IProjectInfo;

        public void AddProjectClick()
        {
            if (SelectedProjects.Any(x => x.FullName == SelectedSolutionItem.FullName))
            {
                ShowToastWarrning($"Project {SelectedSolutionItem.Name} already on list");
                return;
            }

            SelectedProjects.Add(SelectedSolutionItem as IProjectInfo);
        }

        public void ClearProjectsClick()
        {
            SelectedProjects.Clear();
        }

        public async void TestClick()
        {
            try
            {
                var pi = SelectedSolutionItem as IProjectInfo;
                if (pi != null)
                {
                    await _solutionManager.Test(pi.Project, SelectedProjects.Select(x => x.Project));
                }
            }
            catch (Exception ee)
            {
            }
        }

        #endregion Ribbon

        #region Search

        private void SolutionTreeFilter_FilterChanged()
        {
            StartSearch();
        }

        public void SearchFilterGetFocus()
        {
            RibbonSelectedTabIndex = 1;
        }

        public void FilterClearClick()
        {
            Filter = "";
            Solutions = SolutionsCache;

            foreach (var solution in Solutions)
            {
                solution.CollapseAll();
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
            StartSearch();
        }

        private void StartSearch()
        {
            IsSearching = true;
            _searchTimer.Stop();
            ShowToastInfo("Searching..");

            Task.Run(() => ValidateList(SolutionsCache)).ContinueWith(solutions =>
            {
                ShowToastInfo("Search done");
                IsSearching = false;
                Solutions = solutions.Result;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private IEnumerable<TreeViewItemViewModel> ValidateList(IEnumerable<TreeViewItemViewModel> list)
        {
            return list.AsParallel().Select(l => l.Filter(Filter, SolutionTreeFilter)).Where(x => x != null);
        }

        #endregion Search

        #region Solution Tree

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

        public void ProjectDoubleClick()
        {
            if (SelectedSolutionItem == null || !SelectedSolutionItem.HasPreview) return;

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

        #endregion Solution Tree

        #region Find References

        public void SearchResultDoubleClick()
        {
            var document = OpenFile(null, SelectedSearchResult.FilePath);

            document.SelectionStart = SelectedSearchResult.SelectionStart;
            document.SelectionLength = SelectedSearchResult.SelectionLenght;

            OpenDocuments.Add(document);
            ActiveDocument = document;
        }

        #endregion Find References
    }
}