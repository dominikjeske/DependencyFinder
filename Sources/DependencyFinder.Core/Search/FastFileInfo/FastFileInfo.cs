using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DependencyFinder.Search
{
    /// <summary>
    /// A faster way to get file information than System.IO.FileInfo.
    /// </summary>
    /// <remarks>
    /// This enumerator is substantially faster than using <see cref="Directory.GetFiles(string)"/>
    /// and then creating a new FileInfo object for each path.  Use this version when you
    /// will need to look at the attibutes of each file returned (for example, you need
    /// to check each file in a directory to see if it was modified after a specific date).
    /// </remarks>
    [Serializable]
    public class FastFileInfo
    {
        public readonly FileAttributes Attributes;

        public DateTime CreationTime
        {
            get { return this.CreationTimeUtc.ToLocalTime(); }
        }

        /// <summary>
        /// File creation time in UTC
        /// </summary>
        public readonly DateTime CreationTimeUtc;

        /// <summary>
        /// Gets the last access time in local time.
        /// </summary>
        public DateTime LastAccesTime
        {
            get { return this.LastAccessTimeUtc.ToLocalTime(); }
        }

        /// <summary>
        /// File last access time in UTC
        /// </summary>
        public readonly DateTime LastAccessTimeUtc;

        /// <summary>
        /// Gets the last access time in local time.
        /// </summary>
        public DateTime LastWriteTime
        {
            get { return this.LastWriteTimeUtc.ToLocalTime(); }
        }

        /// <summary>
        /// File last write time in UTC
        /// </summary>
        public readonly DateTime LastWriteTimeUtc;

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public readonly long Length;

        /// <summary>
        /// Name of the file
        /// </summary>
        public readonly String Name;

        /// <summary>
        /// Shortened version of Name that has the tidle character
        /// </summary>
        public readonly String AlternateName;

        /// <summary>
        /// Full path to the file.
        /// </summary>
        public readonly String FullName;

        public String DirectoryName
        {
            get
            {
                return System.IO.Path.GetDirectoryName(FullName);
            }
        }

        public bool Exists
        {
            get
            {
                return File.Exists(FullName);
            }
        }

        public override String ToString()
        {
            return this.Name;
        }

        public FastFileInfo(String filename) : this(new FileInfo(filename))
        {
        }

        public FastFileInfo(FileInfo file)
        {
            this.Name = file.Name;
            this.FullName = file.FullName;
            if (file.Exists)
            {
                this.Length = file.Length;
                this.Attributes = file.Attributes;
                this.CreationTimeUtc = file.CreationTimeUtc;
                this.LastAccessTimeUtc = file.LastAccessTimeUtc;
                this.LastWriteTimeUtc = file.LastWriteTimeUtc;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="FastFileInfo"/> class.</summary>
        /// <param name="dir">The directory that the file is stored at</param>
        /// <param name="findData">WIN32_FIND_DATA structure that this object wraps.</param>
        internal FastFileInfo(String dir, WIN32_FIND_DATA findData)
        {
            this.Attributes = findData.dwFileAttributes;
            this.CreationTimeUtc = ConvertDateTime(findData.ftCreationTime_dwHighDateTime, findData.ftCreationTime_dwLowDateTime);
            this.LastAccessTimeUtc = ConvertDateTime(findData.ftLastAccessTime_dwHighDateTime, findData.ftLastAccessTime_dwLowDateTime);
            this.LastWriteTimeUtc = ConvertDateTime(findData.ftLastWriteTime_dwHighDateTime, findData.ftLastWriteTime_dwLowDateTime);
            this.Length = CombineHighLowInts(findData.nFileSizeHigh, findData.nFileSizeLow);
            this.Name = findData.cFileName;
            this.AlternateName = findData.cAlternateFileName;
            this.FullName = System.IO.Path.Combine(dir, findData.cFileName);
        }

        private static long CombineHighLowInts(uint high, uint low)
        {
            return (((long)high) << 0x20) | low;
        }

        private static DateTime ConvertDateTime(uint high, uint low)
        {
            long fileTime = CombineHighLowInts(high, low);
            return DateTime.FromFileTimeUtc(fileTime);
        }

        //---------------------------------
        // static methods:

        /// <summary>
        /// Gets <see cref="FastFileInfo"/> for all the files in a directory.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and
        /// allows you to enumerate the files in the given directory.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is a null reference (Nothing in VB)
        /// </exception>
        public static IEnumerable<FastFileInfo> EnumerateFiles(String path)
        {
            return EnumerateFiles(path, "*");
        }

        /// <summary>
        /// Gets <see cref="FastFileInfo"/> for all the files in a directory that match a
        /// specific filter.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <param name="searchPattern">The search string to match against files in the path.</param>
        /// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and
        /// allows you to enumerate the files in the given directory.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is a null reference (Nothing in VB)
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="filter"/> is a null reference (Nothing in VB)
        /// </exception>
        public static IEnumerable<FastFileInfo> EnumerateFiles(String path, String searchPattern)
        {
            return EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Gets <see cref="FastFileInfo"/> for all the files in a directory that match a specific filter, optionally including all sub directories.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <param name="searchPattern">The search string to match against files in the path.</param>
        /// <param name="searchOption">
        /// One of the SearchOption values that specifies whether the search
        /// operation should include all subdirectories or only the current directory.
        /// </param>
        /// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and allows enumerating files in the specified directory.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is a null reference (Nothing in VB)
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="filter"/> is a null reference (Nothing in VB)
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="searchOption"/> is not one of the valid values of the
        /// <see cref="System.IO.SearchOption"/> enumeration.
        /// </exception>
        public static IEnumerable<FastFileInfo> EnumerateFiles(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");

            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption");

            String fullPath = System.IO.Path.GetFullPath(path);

            return new FileEnumerable(fullPath, searchPattern, searchOption);
        }

        public static IList<FastFileInfo> GetFiles2(String path, String searchPattern = "*", bool searchSubfolders = false)
        {
            SearchOption searchOption = (searchSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            return GetFiles(path, searchPattern, searchOption);
        }

        /// <summary>
        /// Gets <see cref="FastFileInfo"/> for all the files in a directory that match a specific filter.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <param name="searchPattern">The search string to match against files in the path. Multiple can be specified separated by the pipe character.</param>
        /// <returns>An list of FastFileInfo objects that match the specified search pattern.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is a null reference (Nothing in VB)
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="filter"/> is a null reference (Nothing in VB)
        /// </exception>
        public static IList<FastFileInfo> GetFiles(String path, String searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            List<FastFileInfo> list = new List<FastFileInfo>();
            String[] arr = searchPattern.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            Hashtable ht = (arr.Length > 1 ? new Hashtable() : null); // don't need to worry about case since it should be consistent
            foreach (String sp in arr)
            {
                String sp2 = sp.Trim();
                if (sp2.Length == 0)
                    continue;

                IEnumerable<FastFileInfo> e = EnumerateFiles(path, sp2, searchOption);
                if (ht == null)
                    list.AddRange(e);
                else
                {
                    var e2 = e.GetEnumerator();
                    if (ht.Count == 0)
                    {
                        while (e2.MoveNext())
                        {
                            FastFileInfo f = e2.Current;
                            list.Add(f);
                            ht[f.FullName] = f;
                        }
                    }
                    else
                    {
                        while (e2.MoveNext())
                        {
                            FastFileInfo f = e2.Current;
                            if (!ht.Contains(f.FullName))
                            {
                                list.Add(f);
                                ht[f.FullName] = f;
                            }
                        }
                    }
                }
            }

            return list;
        }
    }
}