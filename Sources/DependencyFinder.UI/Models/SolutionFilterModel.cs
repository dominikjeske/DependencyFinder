using System;
using System.Collections.Generic;
using System.Linq;

namespace DependencyFinder.UI.Models
{
    public class SolutionFilterModel
    {
        public IEnumerable<FilterElement> Filters() => new FilterElement[] { Solution, Project, ProjectReference, Reference, Types, UsedBy, Sources, Nugets };
        public event Action FilterChanged;

        public SolutionFilterModel()
        {
            Project.ChildViewTypes = new FilterElement[] { ProjectReference, Reference, Types, UsedBy, Sources, Nugets };

            Solution.ChildViewTypes = (new FilterElement[] { Project }).Union(Project.ChildViewTypes).ToArray();

            Project.Changed += Changed;
            Nugets.Changed += Changed;
            ProjectReference.Changed += Changed;
            Reference.Changed += Changed;
            Types.Changed += Changed;
            UsedBy.Changed += Changed;
            Sources.Changed += Changed;
        }

        private void Changed()
        {
            FilterChanged?.Invoke();
        }

        public FilterElement Solution { get; set; } = new FilterElement
        {
            IsSelected = true,
            ViewType = typeof(SolutionViewModel)
        };

        public FilterElement Project { get; set; } = new FilterElement { ViewType = typeof(ProjectViewModel), IsSelected = true };
        public FilterElement Nugets { get; set; } = new FilterElement { ViewType = typeof(NugetViewModel), IsSelected = true };
        public FilterElement ProjectReference { get; set; } = new FilterElement { ViewType = typeof(ProjectRefViewModel) };
        public FilterElement Reference { get; set; } = new FilterElement { ViewType = typeof(ProjectDirectRefViewModel) };
        public FilterElement Types { get; set; } = new FilterElement { ViewType = typeof(TypeViewModel) };
        public FilterElement UsedBy { get; set; } = new FilterElement { ViewType = typeof(ReferencedViewModel) };
        public FilterElement Sources { get; set; } = new FilterElement { ViewType = typeof(SourceCodeViewModel) };
    }

    public class FilterElement
    {
        private bool _isSelected;

        public event Action Changed;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                Changed?.Invoke();
            }
        }

        public Type ViewType { get; set; }

        public FilterElement[] ChildViewTypes { get; set; } = new FilterElement[] { };

        public bool HasChildFilter => ChildViewTypes.Any(x => x.IsSelected);
    }
}