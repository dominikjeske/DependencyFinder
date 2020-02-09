namespace DependencyFinder.Core
{
    public class Reference
    {
        public string FileName { get; set; }

        public string FilePath { get; set; }
        public string ProjectName { get; set; }
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public string Block { get; set; }
        public int LineNumber { get; set; }
    }
}