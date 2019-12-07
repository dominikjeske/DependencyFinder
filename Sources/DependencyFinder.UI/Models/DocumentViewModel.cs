using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace DependencyFinder.UI.Models
{
    public class DocumentViewModel
    {
        public TreeViewItemViewModel AssociatedModel { get; set; }
        public TextDocument Content { get; set; }
        public IHighlightingDefinition Syntax { get; set; }
        public string Title { get; set; }
    }
}