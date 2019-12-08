using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using System.ComponentModel;

namespace DependencyFinder.UI.Models
{
    public class DocumentViewModel : INotifyPropertyChanged
    {
        public static readonly DocumentViewModel Empty = new DocumentViewModel();

        public TreeViewItemViewModel AssociatedModel { get; set; }
        public TextDocument Content { get; set; }
        public IHighlightingDefinition Syntax { get; set; }
        public string Title { get; set; }
        public bool IsTemporary { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}