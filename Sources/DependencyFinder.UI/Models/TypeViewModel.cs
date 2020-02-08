using DependencyFinder.Core.Models;

namespace DependencyFinder.UI.Models
{
    public abstract class TypeViewModel : TreeViewItemViewModel
    {
        public static TypeViewModel Empty(TreeViewItemViewModel parent) => new ClassViewModel(new ClassDetails { Name = "Loading..." }, parent);

        public TypeViewModel(TypeDetails type, TreeViewItemViewModel parent) : base(parent, false)
        {
            Name = type.Name;
            FullName = type.Name;

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

    public class InterfaceViewModel : TypeViewModel
    {
        public InterfaceViewModel(InterfaceDetails type, TreeViewItemViewModel parent) : base(type, parent)
        {
        }
    }

    public class ClassViewModel : TypeViewModel
    {
        public ClassViewModel(ClassDetails type, TreeViewItemViewModel parent) : base(type, parent)
        {
        }
    }

    public class EnumViewModel : TypeViewModel
    {
        public EnumViewModel(EnumDetails type, TreeViewItemViewModel parent) : base(type, parent)
        {
        }
    }

    public class StructViewModel : TypeViewModel
    {
        public StructViewModel(StructDetails type, TreeViewItemViewModel parent) : base(type, parent)
        {
        }
    }

    public abstract class MemberViewModel : TreeViewItemViewModel
    {
        public MemberViewModel(Member member, TreeViewItemViewModel parent) : base(parent, false)
        {
            Name = member.Name;
            FullName = member.Name;
        }
    }

    public class PropertyViewModel : MemberViewModel
    {
        public PropertyViewModel(PropertyMember member, TreeViewItemViewModel parent) : base(member, parent)
        {
        }
    }

    public class EventViewModel : MemberViewModel
    {
        public EventViewModel(EventMember member, TreeViewItemViewModel parent) : base(member, parent)
        {
        }
    }

    public class FieldViewModel : MemberViewModel
    {
        public FieldViewModel(FieldMember member, TreeViewItemViewModel parent) : base(member, parent)
        {
        }
    }

    public class MethodViewModel : MemberViewModel
    {
        public MethodViewModel(MethodMember member, TreeViewItemViewModel parent) : base(member, parent)
        {
        }
    }
}