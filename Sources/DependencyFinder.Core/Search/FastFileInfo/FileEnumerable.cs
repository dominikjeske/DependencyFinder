using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DependencyFinder.Search
{
    internal class FileEnumerable : IEnumerable<FastFileInfo>
    {
        private readonly String path;
        private readonly String filter;
        private readonly SearchOption searchOption;

        public FileEnumerable(String path, String filter, SearchOption searchOption)
        {
            this.path = path;
            this.filter = filter;
            this.searchOption = searchOption;
        }

        public IEnumerator<FastFileInfo> GetEnumerator()
        {
            return new FileEnumerator(path, filter, searchOption, true, false, false);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new FileEnumerator(path, filter, searchOption, true, false, false);
        }
    }
}