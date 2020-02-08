namespace CommonStandard
{
    public class TestClass : ITestInterface
    {
        public string Field;

        public void Add()
        {
        }

        public static void StaticAdd()
        {
        }

        public string Name { get; set; }

        public string Age { get; set; }
    }

    public enum TestEnum
    {
        One, Two, Three
    }

    public struct TestStruct
    {
        
    }

    internal class InternalClass
    {
    }

    internal enum InternalEnum
    {
        One, Two, Three
    }
}