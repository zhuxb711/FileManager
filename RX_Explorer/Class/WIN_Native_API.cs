﻿using Microsoft.Win32.SafeHandles;
using ShareClassLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using FileAttributes = System.IO.FileAttributes;

namespace RX_Explorer.Class
{
    public static class WIN_Native_API
    {
        private enum FINDEX_INFO_LEVELS
        {
            FindExInfoStandard = 0,
            FindExInfoBasic = 1
        }

        private enum FINDEX_SEARCH_OPS
        {
            FindExSearchNameMatch = 0,
            FindExSearchLimitToDirectories = 1,
            FindExSearchLimitToDevices = 2
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA : IEquatable<WIN32_FIND_DATA>
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;

            public override bool Equals(object obj)
            {
                return cFileName.Equals(obj);
            }

            public override int GetHashCode()
            {
                return cFileName.GetHashCode();
            }

            public static bool operator ==(WIN32_FIND_DATA left, WIN32_FIND_DATA right)
            {
                return left.cFileName.Equals(right.cFileName);
            }

            public static bool operator !=(WIN32_FIND_DATA left, WIN32_FIND_DATA right)
            {
                return !(left.cFileName == right.cFileName);
            }

            public bool Equals(WIN32_FIND_DATA other)
            {
                return cFileName.Equals(other.cFileName);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            [MarshalAs(UnmanagedType.U2)] public short Year;
            [MarshalAs(UnmanagedType.U2)] public short Month;
            [MarshalAs(UnmanagedType.U2)] public short DayOfWeek;
            [MarshalAs(UnmanagedType.U2)] public short Day;
            [MarshalAs(UnmanagedType.U2)] public short Hour;
            [MarshalAs(UnmanagedType.U2)] public short Minute;
            [MarshalAs(UnmanagedType.U2)] public short Second;
            [MarshalAs(UnmanagedType.U2)] public short Milliseconds;

            public SYSTEMTIME(DateTime dt)
            {
                dt = dt.ToUniversalTime();
                Year = (short)dt.Year;
                Month = (short)dt.Month;
                DayOfWeek = (short)dt.DayOfWeek;
                Day = (short)dt.Day;
                Hour = (short)dt.Hour;
                Minute = (short)dt.Minute;
                Second = (short)dt.Second;
                Milliseconds = (short)dt.Millisecond;
            }
        }

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindFirstFileExFromApp(string lpFileName,
                                                            FINDEX_INFO_LEVELS fInfoLevelId,
                                                            out WIN32_FIND_DATA lpFindFileData,
                                                            FINDEX_SEARCH_OPS fSearchOp,
                                                            IntPtr lpSearchFilter,
                                                            FINDEX_ADDITIONAL_FLAGS dwAdditionalFlags);
        private enum FINDEX_ADDITIONAL_FLAGS
        {
            NONE = 0,
            FIND_FIRST_EX_CASE_SENSITIVE = 1,
            FIND_FIRST_EX_LARGE_FETCH = 2,
            FIND_FIRST_EX_ON_DISK_ENTRIES_ONLY = 4
        }

        [DllImport("api-ms-win-core-file-l1-1-0.dll", CharSet = CharSet.Unicode)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("api-ms-win-core-file-l1-1-0.dll")]
        private static extern bool FindClose(IntPtr hFindFile);

        [DllImport("api-ms-win-core-timezone-l1-1-0.dll", SetLastError = true)]
        public static extern bool FileTimeToSystemTime(ref FILETIME lpFileTime, out SYSTEMTIME lpSystemTime);

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateFileFromApp(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr SecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("api-ms-win-core-handle-l1-1-0.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("api-ms-win-core-file-l2-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool ReadDirectoryChangesW(IntPtr hDirectory, IntPtr lpBuffer, uint nBufferLength, bool bWatchSubtree, uint dwNotifyFilter, out uint lpBytesReturned, IntPtr lpOverlapped, IntPtr lpCompletionRoutine);

        [DllImport("api-ms-win-core-io-l1-1-1.dll")]
        private static extern bool CancelIoEx(IntPtr hFile, IntPtr lpOverlapped);

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateDirectoryFromAppW(string lpPathName, IntPtr lpSecurityAttributes);

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DeleteFileFromApp(string lpPathName);

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool RemoveDirectoryFromApp(string lpPathName);

        [DllImport("api-ms-win-core-namedpipe-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafePipeHandle CreateNamedPipe(string lpName,
                                                             PIPE_ACCESS dwOpenMode,
                                                             PIPE_TYPE dwPipeMode,
                                                             uint nMaxInstances,
                                                             uint nOutBufferSize,
                                                             uint nInBufferSize,
                                                             uint nDefaultTimeOut,
                                                             SECURITY_ATTRIBUTES securityAttributes);
        [DllImport("api-ms-win-security-sddl-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string StringSecurityDescriptor, SDDL_REVISION StringSDRevision, out IntPtr SecurityDescriptor, out uint SecurityDescriptorSize);

        [StructLayout(LayoutKind.Sequential)]
        public class SECURITY_ATTRIBUTES
        {
            public int nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>();

            public IntPtr pSecurityDescriptor;

            [MarshalAs(UnmanagedType.Bool)]
            public bool bInheritHandle;
        }

        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint FILE_LIST_DIRECTORY = 0x1;
        const uint FILE_NO_SHARE = 0x0;
        const uint FILE_SHARE_READ = 0x1;
        const uint FILE_SHARE_WRITE = 0x2;
        const uint FILE_SHARE_DELETE = 0x4;
        const uint CREATE_NEW = 1;
        const uint CREATE_ALWAYS = 2;
        const uint OPEN_EXISTING = 3;
        const uint OPEN_ALWAYS = 4;
        const uint TRUNCATE_EXISTING = 5;
        const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;
        const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        const uint FILE_NOTIFY_CHANGE_FILE_NAME = 0x1;
        const uint FILE_NOTIFY_CHANGE_DIR_NAME = 0x2;
        const uint FILE_NOTIFY_CHANGE_LAST_WRITE = 0x10;
        const uint FILE_NOTIFY_CHANGE_SIZE = 0x8;
        const uint FILE_NOTIFY_CHANGE_ATTRIBUTES = 0x4;

        private enum SDDL_REVISION
        {
            /// <summary>SDDL revision 1.</summary>
            SDDL_REVISION_1 = 1
        }

        private enum StateChangeType
        {
            Unknown_Action = 0,
            Added_Action = 1,
            Removed_Action = 2,
            Modified_Action = 3,
            Rename_Action_OldName = 4,
            Rename_Action_NewName = 5
        }

        [Flags]
        public enum PIPE_TYPE : uint
        {
            PIPE_WAIT = 0x00000000,
            PIPE_NOWAIT = 0x00000001,
            PIPE_READMODE_BYTE = 0x00000000,
            PIPE_READMODE_MESSAGE = 0x00000002,
            PIPE_TYPE_BYTE = 0x00000000,
            PIPE_TYPE_MESSAGE = 0x00000004,
            PIPE_ACCEPT_REMOTE_CLIENTS = 0x00000000,
            PIPE_REJECT_REMOTE_CLIENTS = 0x00000008,
            PIPE_CLIENT_END = 0x00000000,
            PIPE_SERVER_END = 0x00000001,
        }

        [Flags]
        public enum PIPE_ACCESS : uint
        {
            PIPE_ACCESS_DUPLEX = 0x00000003,
            PIPE_ACCESS_INBOUND = 0x00000001,
            PIPE_ACCESS_OUTBOUND = 0x00000002,
            FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000,
            FILE_FLAG_WRITE_THROUGH = 0x80000000,
            FILE_FLAG_OVERLAPPED = 0x40000000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            ACCESS_SYSTEM_SECURITY = 0x01000000
        }

        public static FileStream CreateFileStreamFromExistingPath(string Path, AccessMode AccessMode)
        {
            IntPtr Handle = IntPtr.Zero;

            switch (AccessMode)
            {
                case AccessMode.Read:
                    {
                        Handle = CreateFileFromApp(Path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                        break;
                    }
                case AccessMode.Write:
                    {
                        Handle = CreateFileFromApp(Path, GENERIC_WRITE, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                        break;
                    }
                case AccessMode.ReadWrite:
                    {
                        Handle = CreateFileFromApp(Path, GENERIC_WRITE | GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                        break;
                    }
                case AccessMode.Exclusive:
                    {
                        Handle = CreateFileFromApp(Path, GENERIC_WRITE | GENERIC_READ, FILE_NO_SHARE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                        break;
                    }
            }

            SafeFileHandle SHandle = new SafeFileHandle(Handle, true);

            if (SHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            switch (AccessMode)
            {
                case AccessMode.Exclusive:
                case AccessMode.ReadWrite:
                    {
                        return new FileStream(SHandle, FileAccess.ReadWrite);
                    }
                case AccessMode.Write:
                    {
                        return new FileStream(SHandle, FileAccess.Write);
                    }
                case AccessMode.Read:
                    {
                        return new FileStream(SHandle, FileAccess.Read);
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        public static bool CreateDirectoryFromPath(string Path, CreateOption Option, out string NewFolderPath)
        {
            try
            {
                PathAnalysis Analysis = new PathAnalysis(Path, string.Empty);

                while (true)
                {
                    string NextPath = Analysis.NextFullPath();

                    if (Analysis.HasNextLevel)
                    {
                        if (!CheckExist(NextPath))
                        {
                            if (!CreateDirectoryFromAppW(NextPath, IntPtr.Zero))
                            {
                                NewFolderPath = string.Empty;
                                LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "An exception was threw when createdirectory");
                                return false;
                            }
                        }
                    }
                    else
                    {
                        switch (Option)
                        {
                            case CreateOption.GenerateUniqueName:
                                {
                                    string UniquePath = GenerateUniquePath(NextPath, StorageItemTypes.Folder);

                                    if (CreateDirectoryFromAppW(UniquePath, IntPtr.Zero))
                                    {
                                        NewFolderPath = UniquePath;
                                        return true;
                                    }
                                    else
                                    {
                                        NewFolderPath = string.Empty;
                                        LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "An exception was threw when createdirectory");
                                        return false;
                                    }
                                }
                            case CreateOption.OpenIfExist:
                                {
                                    if (CheckExist(NextPath))
                                    {
                                        NewFolderPath = NextPath;
                                        return true;
                                    }
                                    else
                                    {
                                        if (CreateDirectoryFromAppW(NextPath, IntPtr.Zero))
                                        {
                                            NewFolderPath = NextPath;
                                            return true;
                                        }
                                        else
                                        {
                                            NewFolderPath = string.Empty;
                                            LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "An exception was threw when createdirectory");
                                            return false;
                                        }
                                    }
                                }
                            default:
                                {
                                    throw new ArgumentException("Argument is invalid", nameof(Option));
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NewFolderPath = string.Empty;
                LogTracer.Log(ex, "An exception was threw when createdirectory");
                return false;
            }
        }

        public static bool CreateFileFromPath(string Path, CreateOption Option, out string NewPath)
        {
            try
            {
                switch (Option)
                {
                    case CreateOption.GenerateUniqueName:
                        {
                            if (CheckExist(Path))
                            {
                                string UniquePath = GenerateUniquePath(Path, StorageItemTypes.File);

                                IntPtr Handle = CreateFileFromApp(UniquePath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                                if (Handle.CheckIfValidPtr())
                                {
                                    CloseHandle(Handle);
                                    NewPath = UniquePath;
                                    return true;
                                }
                                else
                                {
                                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "Could not create a new file");
                                    NewPath = string.Empty;
                                    return false;
                                }
                            }
                            else
                            {
                                IntPtr Handle = CreateFileFromApp(Path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                                if (Handle.CheckIfValidPtr())
                                {
                                    CloseHandle(Handle);
                                    NewPath = Path;
                                    return true;
                                }
                                else
                                {
                                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "Could not create a new file");
                                    NewPath = string.Empty;
                                    return false;
                                }
                            }
                        }
                    case CreateOption.OpenIfExist:
                        {
                            IntPtr Handle = CreateFileFromApp(Path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                            if (Handle.CheckIfValidPtr())
                            {
                                CloseHandle(Handle);
                                NewPath = Path;
                                return true;
                            }
                            else
                            {
                                LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "Could not create a new file");
                                NewPath = string.Empty;
                                return false;
                            }
                        }
                    case CreateOption.ReplaceExisting:
                        {
                            IntPtr Handle = CreateFileFromApp(Path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                            if (Handle.CheckIfValidPtr())
                            {
                                CloseHandle(Handle);
                                NewPath = Path;
                                return true;
                            }
                            else
                            {
                                LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "Could not create a new file");
                                NewPath = string.Empty;
                                return false;
                            }
                        }
                    default:
                        {
                            NewPath = string.Empty;
                            return false;
                        }
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex, "Could not create a new file");
                NewPath = string.Empty;
                return false;
            }
        }

        private static string GenerateUniquePath(string Path, StorageItemTypes ItemType)
        {
            string UniquePath = Path;

            if (ItemType == StorageItemTypes.File)
            {
                string NameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(Path);
                string Extension = System.IO.Path.GetExtension(Path);
                string Directory = System.IO.Path.GetDirectoryName(Path);

                for (ushort Count = 1; CheckExist(UniquePath); Count++)
                {
                    if (Regex.IsMatch(NameWithoutExt, @".*\(\d+\)"))
                    {
                        UniquePath = System.IO.Path.Combine(Directory, $"{NameWithoutExt.Substring(0, NameWithoutExt.LastIndexOf("(", StringComparison.InvariantCultureIgnoreCase))}({Count}){Extension}");
                    }
                    else
                    {
                        UniquePath = System.IO.Path.Combine(Directory, $"{NameWithoutExt} ({Count}){Extension}");
                    }
                }
            }
            else
            {
                string Directory = System.IO.Path.GetDirectoryName(Path);
                string Name = System.IO.Path.GetFileName(Path);

                for (ushort Count = 1; CheckExist(UniquePath); Count++)
                {
                    if (Regex.IsMatch(Name, @".*\(\d+\)"))
                    {
                        UniquePath = System.IO.Path.Combine(Directory, $"{Name.Substring(0, Name.LastIndexOf("(", StringComparison.InvariantCultureIgnoreCase))}({Count})");
                    }
                    else
                    {
                        UniquePath = System.IO.Path.Combine(Directory, $"{Name} ({Count})");
                    }
                }
            }

            return UniquePath;
        }

        public static void StopDirectoryWatcher(ref IntPtr hDir)
        {
            CancelIoEx(hDir, IntPtr.Zero);
            CloseHandle(hDir);
            hDir = IntPtr.Zero;
        }

        public static IntPtr CreateDirectoryWatcher(string FolderPath, Action<string> Added = null, Action<string> Removed = null, Action<string, string> Renamed = null, Action<string> Modified = null)
        {
            try
            {
                IntPtr hDir = CreateFileFromApp(FolderPath, FILE_LIST_DIRECTORY, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);

                if (!hDir.CheckIfValidPtr())
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                Task.Factory.StartNew((Arguement) =>
                {
                    ValueTuple<IntPtr, Action<string>, Action<string>, Action<string, string>, Action<string>> Package = (ValueTuple<IntPtr, Action<string>, Action<string>, Action<string, string>, Action<string>>)Arguement;

                    while (true)
                    {
                        IntPtr BufferPointer = Marshal.AllocHGlobal(4096);

                        try
                        {
                            if (ReadDirectoryChangesW(Package.Item1, BufferPointer, 4096, false, FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_SIZE | FILE_NOTIFY_CHANGE_ATTRIBUTES, out uint BytesReturned, IntPtr.Zero, IntPtr.Zero))
                            {
                                if (BytesReturned > 0)
                                {
                                    IntPtr CurrentPointer = BufferPointer;
                                    int Offset = 0;
                                    string OldPath = null;

                                    do
                                    {
                                        CurrentPointer = (IntPtr)(Offset + CurrentPointer.ToInt64());

                                        // Read file length (in bytes) at offset 8
                                        int FileNameLength = Marshal.ReadInt32(CurrentPointer, 8);
                                        // Read file name (fileLen/2 characters) from offset 12
                                        string FileName = Marshal.PtrToStringUni((IntPtr)(12 + CurrentPointer.ToInt64()), FileNameLength / 2);
                                        // Read action at offset 4
                                        int ActionIndex = Marshal.ReadInt32(CurrentPointer, 4);

                                        if (ActionIndex < 1 || ActionIndex > 5)
                                        {
                                            ActionIndex = 0;
                                        }

                                        switch ((StateChangeType)ActionIndex)
                                        {
                                            case StateChangeType.Unknown_Action:
                                                {
                                                    break;
                                                }
                                            case StateChangeType.Added_Action:
                                                {
                                                    Package.Item2?.Invoke(Path.Combine(FolderPath, FileName));
                                                    break;
                                                }
                                            case StateChangeType.Removed_Action:
                                                {
                                                    Package.Item3?.Invoke(Path.Combine(FolderPath, FileName));
                                                    break;
                                                }
                                            case StateChangeType.Modified_Action:
                                                {
                                                    Package.Item5?.Invoke(Path.Combine(FolderPath, FileName));
                                                    break;
                                                }
                                            case StateChangeType.Rename_Action_OldName:
                                                {
                                                    OldPath = Path.Combine(FolderPath, FileName);
                                                    break;
                                                }
                                            case StateChangeType.Rename_Action_NewName:
                                                {
                                                    Package.Item4?.Invoke(OldPath, Path.Combine(FolderPath, FileName));
                                                    break;
                                                }
                                        }

                                        // Read NextEntryOffset at offset 0 and move pointer to next structure if needed
                                        Offset = Marshal.ReadInt32(CurrentPointer);
                                    }
                                    while (Offset != 0);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            LogTracer.Log("Exception happened when watching directory. Message: " + e.Message);
                        }
                        finally
                        {
                            if (BufferPointer != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(BufferPointer);
                            }
                        }
                    }
                }, (hDir, Added, Removed, Renamed, Modified), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                return hDir;
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex, "An exception was threw when creating directory watcher");
                return IntPtr.Zero;
            }
        }

        public static bool CheckContainsAnyItem(string FolderPath, bool IncludeHiddenItem, bool IncludeSystemItem, BasicFilters Filter)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FolderPath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.NONE);

            try
            {
                if (Ptr.CheckIfValidPtr())
                {
                    do
                    {
                        if (Data.cFileName != "." && Data.cFileName != "..")
                        {
                            FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                            if ((IncludeHiddenItem || !Attribute.HasFlag(FileAttributes.Hidden)) && (IncludeSystemItem || !Attribute.HasFlag(FileAttributes.System)))
                            {
                                if (Attribute.HasFlag(FileAttributes.Directory))
                                {
                                    if (Filter.HasFlag(BasicFilters.Folder))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                else if (Filter.HasFlag(BasicFilters.File))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    while (FindNextFile(Ptr, out Data));

                    return false;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return false;
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static bool CheckExist(string Path)
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                throw new ArgumentException("Argument could not be empty", nameof(Path));
            }

            IntPtr Ptr = FindFirstFileExFromApp(System.IO.Path.GetPathRoot(Path) == Path ? System.IO.Path.Combine(Path, "*") : Path.TrimEnd('\\'), FINDEX_INFO_LEVELS.FindExInfoBasic, out _, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.NONE);

            try
            {
                if (Ptr.CheckIfValidPtr())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static bool CheckIfHidden(string ItemPath)
        {
            if (string.IsNullOrWhiteSpace(ItemPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(ItemPath));
            }

            if (Path.GetPathRoot(ItemPath) == ItemPath)
            {
                return false;
            }

            IntPtr Ptr = FindFirstFileExFromApp(ItemPath.TrimEnd('\\'), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.NONE);

            try
            {
                if (Ptr.CheckIfValidPtr())
                {
                    if (((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Hidden))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static ulong CalulateSize(string Path, CancellationToken CancelToken = default)
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                throw new ArgumentException("Argument could not be empty", nameof(Path));
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.TrimEnd('\\'), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.CheckIfValidPtr())
                {
                    if (((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Directory))
                    {
                        return CalculateFolderSize(Path, CancelToken);
                    }
                    else
                    {
                        return CalculateFileSize(Path);
                    }
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return 0;
                }
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        private static ulong CalculateFolderSize(string FolderPath, CancellationToken CancelToken = default)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FolderPath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.CheckIfValidPtr())
                {
                    ulong TotalSize = 0;

                    do
                    {
                        if (Data.cFileName != "." && Data.cFileName != "..")
                        {
                            if (((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Directory))
                            {
                                TotalSize += CalculateFolderSize(Path.Combine(FolderPath, Data.cFileName), CancelToken);
                            }
                            else
                            {
                                TotalSize += ((ulong)Data.nFileSizeHigh << 32) + Data.nFileSizeLow;
                            }
                        }
                    }
                    while (FindNextFile(Ptr, out Data) && !CancelToken.IsCancellationRequested);

                    return TotalSize;
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return 0;
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return 0;
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        private static ulong CalculateFileSize(string FilePath)
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FilePath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(FilePath.TrimEnd('\\'), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.CheckIfValidPtr())
                {
                    if (!((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Directory))
                    {
                        return ((ulong)Data.nFileSizeHigh << 32) + Data.nFileSizeLow;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return 0;
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return 0;
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static (uint, uint) CalculateFolderAndFileCount(string FolderPath, CancellationToken CancelToken = default)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FolderPath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.CheckIfValidPtr())
                {
                    uint FolderCount = 0;
                    uint FileCount = 0;

                    do
                    {
                        if (Data.cFileName != "." && Data.cFileName != "..")
                        {
                            if (((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Directory))
                            {
                                (uint SubFolderCount, uint SubFileCount) = CalculateFolderAndFileCount(Path.Combine(FolderPath, Data.cFileName), CancelToken);
                                FolderCount += ++SubFolderCount;
                                FileCount += SubFileCount;
                            }
                            else
                            {
                                FileCount++;
                            }
                        }
                    }
                    while (FindNextFile(Ptr, out Data) && !CancelToken.IsCancellationRequested);

                    return (FolderCount, FileCount);
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return (0, 0);
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return (0, 0);
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static IReadOnlyList<FileSystemStorageItemBase> Search(string FolderPath, string SearchWord, bool SearchInSubFolders = false, bool IncludeHiddenItem = false, bool IncludeSystemItem = false, bool IsRegexExpresstion = false, bool IgnoreCase = true, CancellationToken CancelToken = default)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FolderPath));
            }

            if (string.IsNullOrEmpty(SearchWord))
            {
                throw new ArgumentException("Argument could not be empty", nameof(SearchWord));
            }

            try
            {
                List<FileSystemStorageItemBase> SearchResult = new List<FileSystemStorageItemBase>();

                IntPtr SearchPtr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.FIND_FIRST_EX_LARGE_FETCH);

                try
                {
                    if (SearchPtr.CheckIfValidPtr())
                    {
                        do
                        {
                            if (Data.cFileName != "." && Data.cFileName != "..")
                            {
                                FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                                if ((IncludeHiddenItem || !Attribute.HasFlag(FileAttributes.Hidden)) && (IncludeSystemItem || !Attribute.HasFlag(FileAttributes.System)))
                                {
                                    if (IsRegexExpresstion ? Regex.IsMatch(Data.cFileName, SearchWord, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None)
                                                           : Data.cFileName.Contains(SearchWord, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                                    {
                                        string CurrentDataPath = Path.Combine(FolderPath, Data.cFileName);

                                        if (Attribute.HasFlag(FileAttributes.Directory))
                                        {
                                            if (Attribute.HasFlag(FileAttributes.Hidden))
                                            {
                                                SearchResult.Add(new HiddenStorageFolder(CurrentDataPath, Data));
                                            }
                                            else
                                            {
                                                SearchResult.Add(new FileSystemStorageFolder(CurrentDataPath, Data));
                                            }
                                        }
                                        else
                                        {
                                            if (Attribute.HasFlag(FileAttributes.Hidden))
                                            {
                                                SearchResult.Add(new HiddenStorageFile(CurrentDataPath, Data));
                                            }
                                            else if (Data.cFileName.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                                            {
                                                SearchResult.Add(new UrlStorageFile(CurrentDataPath, Data));
                                            }
                                            else if (Data.cFileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                                            {
                                                SearchResult.Add(new LinkStorageFile(CurrentDataPath, Data));
                                            }
                                            else
                                            {
                                                SearchResult.Add(new FileSystemStorageFile(CurrentDataPath, Data));
                                            }
                                        }
                                    }

                                    if (Attribute.HasFlag(FileAttributes.Directory) && SearchInSubFolders)
                                    {
                                        SearchResult.AddRange(Search(Path.Combine(FolderPath, Data.cFileName), SearchWord, true, IncludeHiddenItem, IncludeSystemItem, IsRegexExpresstion, IgnoreCase, CancelToken));
                                    }
                                }
                            }
                        }
                        while (FindNextFile(SearchPtr, out Data) && !CancelToken.IsCancellationRequested);
                    }

                    return SearchResult;
                }
                finally
                {
                    FindClose(SearchPtr);
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return new List<FileSystemStorageItemBase>(0);
            }
        }

        public static bool CheckLocationAvailability(string FolderPath)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                return false;
            }
            else
            {
                IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out _, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.NONE);

                try
                {
                    return Ptr.CheckIfValidPtr();
                }
                finally
                {
                    FindClose(Ptr);
                }
            }
        }

        public static IReadOnlyList<FileSystemStorageItemBase> GetStorageItems(string FolderPath, bool IncludeHiddenItem, bool IncludeSystemItem, uint MaxNumLimit, BasicFilters Filter = BasicFilters.File | BasicFilters.Folder, Func<string, bool> AdvanceFilter = null)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FolderPath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.CheckIfValidPtr())
                {
                    List<FileSystemStorageItemBase> Result = new List<FileSystemStorageItemBase>();

                    do
                    {
                        if (Data.cFileName != "." && Data.cFileName != "..")
                        {
                            if (AdvanceFilter != null && !AdvanceFilter(Data.cFileName))
                            {
                                continue;
                            }

                            FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                            if ((IncludeHiddenItem || !Attribute.HasFlag(FileAttributes.Hidden)) && (IncludeSystemItem || !Attribute.HasFlag(FileAttributes.System)))
                            {
                                if (Attribute.HasFlag(FileAttributes.Directory))
                                {
                                    if (Filter.HasFlag(BasicFilters.Folder))
                                    {
                                        string CurrentDataPath = Path.Combine(FolderPath, Data.cFileName);

                                        if (Attribute.HasFlag(FileAttributes.Hidden))
                                        {
                                            Result.Add(new HiddenStorageFolder(CurrentDataPath, Data));
                                        }
                                        else
                                        {
                                            Result.Add(new FileSystemStorageFolder(CurrentDataPath, Data));
                                        }
                                    }
                                }
                                else
                                {
                                    if (Filter.HasFlag(BasicFilters.File))
                                    {
                                        string CurrentDataPath = Path.Combine(FolderPath, Data.cFileName);

                                        if (Attribute.HasFlag(FileAttributes.Hidden))
                                        {
                                            Result.Add(new HiddenStorageFile(CurrentDataPath, Data));
                                        }
                                        else if (Data.cFileName.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Result.Add(new UrlStorageFile(CurrentDataPath, Data));
                                        }
                                        else if (Data.cFileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Result.Add(new LinkStorageFile(CurrentDataPath, Data));
                                        }
                                        else
                                        {
                                            Result.Add(new FileSystemStorageFile(CurrentDataPath, Data));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    while (FindNextFile(Ptr, out Data) && Result.Count < MaxNumLimit);

                    return Result;
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return new List<FileSystemStorageItemBase>(0);
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return new List<FileSystemStorageItemBase>(0);
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static FileSystemStorageItemBase GetStorageItem(string ItemPath)
        {
            if (string.IsNullOrWhiteSpace(ItemPath))
            {
                throw new ArgumentNullException(nameof(ItemPath), "Argument could not be null");
            }

            try
            {
                IntPtr Ptr = FindFirstFileExFromApp(ItemPath.TrimEnd('\\'), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.NONE);

                try
                {
                    if (Ptr.CheckIfValidPtr())
                    {
                        if (Data.cFileName != "." && Data.cFileName != "..")
                        {
                            FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                            if (Attribute.HasFlag(FileAttributes.Directory))
                            {
                                if (Attribute.HasFlag(FileAttributes.Hidden))
                                {
                                    return new HiddenStorageFolder(ItemPath, Data);
                                }
                                else
                                {
                                    return new FileSystemStorageFolder(ItemPath, Data);
                                }
                            }
                            else
                            {
                                if (Attribute.HasFlag(FileAttributes.Hidden))
                                {
                                    return new HiddenStorageFile(ItemPath, Data);
                                }
                                else if (Data.cFileName.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                                {
                                    return new UrlStorageFile(ItemPath, Data);
                                }
                                else if (Data.cFileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                                {
                                    return new LinkStorageFile(ItemPath, Data);
                                }
                                else
                                {
                                    return new FileSystemStorageFile(ItemPath, Data);
                                }
                            }
                        }

                        return null;
                    }
                    else
                    {
                        LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                        return null;
                    }
                }
                finally
                {
                    FindClose(Ptr);
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return null;
            }
        }

        public static IReadOnlyList<FileSystemStorageItemBase> GetStorageItemInBatch(params string[] PathArray)
        {
            if (PathArray.Length == 0 || PathArray.Any((Item) => string.IsNullOrWhiteSpace(Item)))
            {
                throw new ArgumentException("Argument could not be empty", nameof(PathArray));
            }

            try
            {
                List<FileSystemStorageItemBase> Result = new List<FileSystemStorageItemBase>(PathArray.Length);

                foreach (string Path in PathArray)
                {
                    IntPtr Ptr = FindFirstFileExFromApp(Path.TrimEnd('\\'), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.FIND_FIRST_EX_LARGE_FETCH);

                    try
                    {
                        if (Ptr.CheckIfValidPtr())
                        {
                            if (Data.cFileName != "." && Data.cFileName != "..")
                            {
                                FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                                if (Attribute.HasFlag(FileAttributes.Directory))
                                {
                                    if (Attribute.HasFlag(FileAttributes.Hidden))
                                    {
                                        Result.Add(new HiddenStorageFolder(Path, Data));
                                    }
                                    else
                                    {
                                        Result.Add(new FileSystemStorageFolder(Path, Data));
                                    }
                                }
                                else
                                {
                                    if (Attribute.HasFlag(FileAttributes.Hidden))
                                    {
                                        Result.Add(new HiddenStorageFile(Path, Data));
                                    }
                                    else if (Data.cFileName.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Result.Add(new UrlStorageFile(Path, Data));
                                    }
                                    else if (Data.cFileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Result.Add(new LinkStorageFile(Path, Data));
                                    }
                                    else
                                    {
                                        Result.Add(new FileSystemStorageFile(Path, Data));
                                    }
                                }
                            }
                        }
                        else
                        {
                            LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                        }
                    }
                    finally
                    {
                        FindClose(Ptr);
                    }
                }

                return Result;
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return new List<FileSystemStorageItemBase>();
            }
        }

        public static WIN32_FIND_DATA GetStorageItemRawData(string ItemPath)
        {
            if (string.IsNullOrWhiteSpace(ItemPath))
            {
                throw new ArgumentNullException(nameof(ItemPath), "Argument could not be null");
            }

            try
            {
                IntPtr Ptr = FindFirstFileExFromApp(ItemPath.TrimEnd('\\'), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FINDEX_ADDITIONAL_FLAGS.NONE);

                try
                {
                    if (Ptr.CheckIfValidPtr())
                    {
                        return Data;
                    }
                    else
                    {
                        LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                        return default;
                    }
                }
                finally
                {
                    FindClose(Ptr);
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return default;
            }
        }

        public static SafePipeHandle CreateHandleForNamedPipe(string PipeName, NamedPipeMode Mode)
        {
            SECURITY_ATTRIBUTES SA = new SECURITY_ATTRIBUTES();

            if (ConvertStringSecurityDescriptorToSecurityDescriptor("D:(A;;GA;;;WD)(A;;GA;;;AC)S:(ML;;;;;LW)", SDDL_REVISION.SDDL_REVISION_1, out SA.pSecurityDescriptor, out _))
            {
                SafePipeHandle SPipeHandle = CreateNamedPipe(@$"\\.\pipe\local\{PipeName}", (Mode == NamedPipeMode.Read ? PIPE_ACCESS.PIPE_ACCESS_INBOUND : PIPE_ACCESS.PIPE_ACCESS_OUTBOUND)
                                                                                            | PIPE_ACCESS.WRITE_DAC
                                                                                            | PIPE_ACCESS.WRITE_OWNER
                                                                                            | PIPE_ACCESS.FILE_FLAG_WRITE_THROUGH,

                                                                                            PIPE_TYPE.PIPE_TYPE_BYTE
                                                                                            | PIPE_TYPE.PIPE_WAIT
                                                                                            | PIPE_TYPE.PIPE_READMODE_BYTE,

                                                                                            1, 1024, 1024, 500, SA);

                if (SPipeHandle.IsInvalid)
                {
                    return new SafePipeHandle(new IntPtr(-1), true);
                }
                else
                {
                    return SPipeHandle;
                }
            }
            else
            {
                return new SafePipeHandle(new IntPtr(-1), true);
            }
        }
    }
}
