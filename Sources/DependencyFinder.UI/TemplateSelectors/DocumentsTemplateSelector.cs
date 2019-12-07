using DependencyFinder.UI.Models;
using System.Windows;
using System.Windows.Controls;

namespace DependencyFinder.UI.TemplateSelectors
{
    internal class DocumentsTemplateSelector : DataTemplateSelector
    {
        public DocumentsTemplateSelector()
        {
        }

        public DataTemplate ProjectTemplate
        {
            get;
            set;
        }

        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            if (item is DocumentViewModel)
                return ProjectTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}