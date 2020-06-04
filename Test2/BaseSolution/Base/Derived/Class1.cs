using Base;
using System;

namespace Derived
{
    public interface IDerived : IBase
    {
        void DerivedFirst();

        void DerivedSecond();
    }

    public interface IOther : IBase
    {
        void OtherFirst();

        void OtherSecond();
    }

    public class MyClass : IBase
    {

    }


    public class xxx
    {
        public xxx(IBase test)
        {

        }
    }
}
