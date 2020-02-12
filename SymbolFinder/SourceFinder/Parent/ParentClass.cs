using Slave;
using System;

namespace Parent
{
    public class ParentClass
    {
        public void ParentMethod()
        {
            var c = new TestClass();
            c.Test();
        }
    }
}
