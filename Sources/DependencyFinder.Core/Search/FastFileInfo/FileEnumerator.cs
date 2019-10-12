using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace DependencyFinder.Search
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal class FileEnumerator : IEnumerator<FastFileInfo>
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFindHandle FindFirstFile(String fileName, [In, Out]WIN32_FIND_DATA data);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool FindNextFile(SafeFindHandle hndFindFile, [In, Out, MarshalAs(UnmanagedType.LPStruct)]WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFindHandle FindFirstFileEx(String fileName, int infoLevel, [In, Out]WIN32_FIND_DATA data, int searchScope, String notUsedNull, int additionalFlags);

        private String initialFolder;
        private SearchOption searchOption;
        private String searchFilter;

        //---
        private String currentFolder;

        private SafeFindHandle hndFile;
        private WIN32_FIND_DATA findData;
        private int currentPathIndex;
        private IList<String> currentPaths;
        private IList<String> pendingFolders;
        private Queue<IList<String>> queue;
        private bool advanceNext;
        private bool usePendingFolders = false;
        private bool useGetDirectories = false;
        private bool hasCurrent = false;

        //---
        private bool useEx = false;

        private int infoLevel = 0;
        private int searchScope = 0; // always files (1 = limit to directories, 2 = limit to devices (not supported))
        private int additionalFlags = 0;

        public FileEnumerator(String initialFolder, String searchFilter, SearchOption searchOption)
        {
            init(initialFolder, searchFilter, searchOption);
        }

        // basicInfoOnly is about 30% faster. E.g. the C:\Windows\ directory takes 4.3 sec for standard info, and 3.3 sec for basic info.
        // basicInfoOnly excludes getting the cAlternateName, which is the short name with the tidle character)
        //
        // Note: case sensitive only works if \HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\obcaseinsensitive is set to 0
        // which is probably not a good idea.
        public FileEnumerator(String initialFolder, String searchFilter, SearchOption searchOption, bool basicInfoOnly, bool caseSensitive, bool largeBuffer)
        {
            init(initialFolder, searchFilter, searchOption);
            useEx = true;
            infoLevel = (basicInfoOnly ? 1 : 0); // 0 is standard (includes the cAlternateName, which is the short name with the tidle character)
            additionalFlags |= (caseSensitive ? 1 : 0);
            additionalFlags |= (largeBuffer ? 2 : 0);
        }

        private void init(String initialFolder, String searchFilter, SearchOption searchOption)
        {
            this.initialFolder = initialFolder;
            this.searchFilter = searchFilter;
            this.searchOption = searchOption;
            // usePendingFolders is 60% faster. E.g. the C:\Windows\ directory takes 7.7 seconds if using Directory.GetDirectories
            // but only takes 3.3 seconds if the folders are cached as they are encountered.
            this.usePendingFolders = (searchFilter == "*" || searchFilter == "*.*") && searchOption == SearchOption.AllDirectories;
            // The problem is that when a filter like *.txt is used, none of the directories are returned by FindNextFile.
            this.useGetDirectories = !usePendingFolders && searchOption == SearchOption.AllDirectories;
            Reset();
        }

        public FastFileInfo Current
        {
            get { return new FastFileInfo(currentFolder, findData); }
        }

        public void Dispose()
        {
            if (hndFile != null)
            {
                hndFile.Dispose();
                hndFile = null;
            }
        }

        object System.Collections.IEnumerator.Current
        {
            get
            {
                return new FastFileInfo(currentFolder, findData);
            }
        }

        public bool MoveNext()
        {
            while (true)
            {
                if (advanceNext)
                {
                    hasCurrent = FindNextFile(hndFile, findData);
                }

                if (hasCurrent || !advanceNext)
                {
                    // first skip over any directories, but store them if usePendingFolders is true
                    while (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (usePendingFolders)
                        {
                            String c = findData.cFileName;
                            if (!(c[0] == '.' && (c.Length == 1 || c[1] == '.' && c.Length == 2))) // skip folders '.' and '..'
                                pendingFolders.Add(Path.Combine(currentFolder, c));
                        }
                        hasCurrent = FindNextFile(hndFile, findData);
                        if (!hasCurrent)
                            break;
                    }
                }

                if (hasCurrent)
                {
                    advanceNext = true;
                    return true;
                }

                if (useGetDirectories)
                {
                    // even though the docs claim searchScope '1' only returns directories, it actually returns files and directories
                    var h = FindFirstFileEx(Path.Combine(currentFolder, "*"), 1, findData, 1, null, 0);
                    if (!h.IsInvalid)
                    {
                        while (true)
                        {
                            if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                            {
                                String c = findData.cFileName;
                                if (!(c[0] == '.' && (c.Length == 1 || c[1] == '.' && c.Length == 2))) // skip folders '.' and '..'
                                    pendingFolders.Add(Path.Combine(currentFolder, c));
                            }

                            if (!FindNextFile(h, findData))
                                break;
                        }
                    }
                    h.Dispose();

                    // using this code is twice as slow. E.g. the C:\Windows\ folder took 7.4 sec versus 3.8 sec.
                    //try {
                    //	pendingFolders = Directory.GetDirectories(currentFolder);
                    //} catch {} // Access to the path '...\System Volume Information' is denied.
                }

                // at this point, the current folder is exhausted. If search subfolders then enqueue them.
                if (pendingFolders.Count > 0)
                {
                    queue.Enqueue(pendingFolders);
                    pendingFolders = new List<String>();
                }

                currentPathIndex++;
                if (currentPathIndex == currentPaths.Count)
                {
                    // at the end of the current paths
                    if (queue.Count == 0)
                    {
                        currentPathIndex--; // so that calling MoveNext() after very last has no impact
                        return false; // no more paths to process
                    }
                    currentPaths = queue.Dequeue();
                    currentPathIndex = 0;
                }

                String f = currentPaths[currentPathIndex];
                InitFolder(f);
            }
        }

        // returns true if the folder can be searched
        private void InitFolder(String folder)
        {
            if (hndFile != null)
                hndFile.Dispose();

            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, folder).Demand();
            String searchPath = System.IO.Path.Combine(folder, searchFilter);
            if (useEx)
                hndFile = FindFirstFileEx(searchPath, infoLevel, findData, searchScope, null, additionalFlags);
            else
                hndFile = FindFirstFile(searchPath, findData);
            currentFolder = folder;
            advanceNext = false;
            hasCurrent = !hndFile.IsInvalid; // e.g. unaccessible C:\System Volume Information or filter like *.txt in a directory with no text files
        }

        public void Reset()
        {
            currentPathIndex = 0;
            advanceNext = false;
            hasCurrent = false;
            currentPaths = new[] { initialFolder };
            findData = new WIN32_FIND_DATA();
            pendingFolders = new List<String>();
            queue = new Queue<IList<String>>();
            InitFolder(initialFolder);
        }
    }
}