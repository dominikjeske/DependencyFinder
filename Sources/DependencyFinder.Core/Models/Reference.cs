using ByteDev.DotNet.Project;
using ByteDev.DotNet.Solution;
using DependencyFinder.Core.Models;
using DependencyFinder.Utils;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyFinder.Core
{

    public class Reference
    {
        public string FileName { get; set; }
        public string ProjectName { get; set; }
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public string Block { get; set; }
        public int LineNumber { get; set; }

    }
}