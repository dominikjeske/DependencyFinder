using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public abstract class TypeViewModel : TreeViewItemViewModel
    {
        public TypeDetails TypeDetails { get; set; }

        public TypeViewModel(TypeDetails type, TreeViewItemViewModel parent) : base(parent, false)
        {
            Name = type.Name;
            FullName = type.Name;
            TypeDetails = type;

            foreach (var member in type.Members)
            {
                if (member is PropertyMember pm)
                {
                    Children.Add(new PropertyViewModel(pm, this));
                }
                else if (member is FieldMember fm)
                {
                    Children.Add(new FieldViewModel(fm, this));
                }
                if (member is MethodMember mm)
                {
                    Children.Add(new MethodViewModel(mm, this));
                }
                if (member is EventMember em)
                {
                    Children.Add(new EventViewModel(em, this));
                }
            }
        }
    }
}