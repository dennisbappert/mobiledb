#region Copyright (C) 2014 Dennis Bappert
// The MIT License (MIT)

// Copyright (c) 2014 Dennis Bappert

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobileDB.Common;
using MobileDB.Common.Utilities;
using MobileDB.FileSystem.Contracts;

namespace MobileDB.FileSystem
{
    public class PhysicalFileSystem : IFileSystem
    {
        public PhysicalFileSystem(string connectionString)
        {
            var physicalRoot = connectionString.ConnectionStringPart(ConnectionStringConstants.Path);

            if (!Path.IsPathRooted(physicalRoot))
                physicalRoot = Path.GetFullPath(physicalRoot);
            if (physicalRoot[physicalRoot.Length - 1] != Path.DirectorySeparatorChar)
                physicalRoot = physicalRoot + Path.DirectorySeparatorChar;
            PhysicalRoot = physicalRoot;
        }

        public string PhysicalRoot { get; private set; }

        public async Task<IEnumerable<FileSystemPath>> GetEntities(FileSystemPath path, CancellationToken cancellationToken)
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            var physicalPath = GetPhysicalPath(path);
            var directories = Directory.GetDirectories(physicalPath);
            var files = Directory.GetFiles(physicalPath);
            var virtualDirectories =
                directories.Select(GetVirtualDirectoryPath);
            var virtualFiles =
                files.Select(GetVirtualFilePath);
            return new EnumerableCollection<FileSystemPath>(virtualDirectories.Concat(virtualFiles),
                directories.Length + files.Length);
        }

        public async Task<bool> Exists(FileSystemPath path, CancellationToken cancellationToken)
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            return path.IsFile ? File.Exists(GetPhysicalPath(path)) : Directory.Exists(GetPhysicalPath(path));
        }

        public async Task<Stream> CreateFile(FileSystemPath path, CancellationToken cancellationToken)
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            if (!path.IsFile)
                throw new ArgumentException("The specified path is not a file.", "path");
            return File.Create(GetPhysicalPath(path));
        }

        public async Task<Stream> OpenFile(FileSystemPath path, DesiredFileAccess access, CancellationToken cancellationToken)
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            if (!path.IsFile)
                throw new ArgumentException("The specified path is not a file.", "path");
            return File.Open(GetPhysicalPath(path), FileMode.Open, access.ToFileAccess());
        }

        public async Task CreateDirectory(FileSystemPath path, CancellationToken cancellationToken)
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            if (!path.IsDirectory)
                throw new ArgumentException("The specified path is not a directory.", "path");
            Directory.CreateDirectory(GetPhysicalPath(path));
        }

        public async Task Delete(FileSystemPath path, CancellationToken cancellationToken)
        {
            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            if (path.IsFile)
                File.Delete(GetPhysicalPath(path));
            else
                Directory.Delete(GetPhysicalPath(path), true);
        }

        public void Dispose()
        {
        }

        public string GetPhysicalPath(FileSystemPath path)
        {
            return Path.Combine(PhysicalRoot,
                path.ToString().Remove(0, 1).Replace(FileSystemPath.DirectorySeparator, Path.DirectorySeparatorChar));
        }

        public FileSystemPath GetVirtualFilePath(string physicalPath)
        {
            if (!physicalPath.StartsWith(PhysicalRoot, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("The specified path is not member of the PhysicalRoot.", "physicalPath");
            var virtualPath = FileSystemPath.DirectorySeparator +
                              physicalPath.Remove(0, PhysicalRoot.Length)
                                  .Replace(Path.DirectorySeparatorChar, FileSystemPath.DirectorySeparator);
            return FileSystemPath.Parse(virtualPath);
        }

        public FileSystemPath GetVirtualDirectoryPath(string physicalPath)
        {
            if (!physicalPath.StartsWith(PhysicalRoot, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("The specified path is not member of the PhysicalRoot.", "physicalPath");
            var virtualPath = FileSystemPath.DirectorySeparator +
                              physicalPath.Remove(0, PhysicalRoot.Length)
                                  .Replace(Path.DirectorySeparatorChar, FileSystemPath.DirectorySeparator);
            if (virtualPath[virtualPath.Length - 1] != FileSystemPath.DirectorySeparator)
                virtualPath += FileSystemPath.DirectorySeparator;
            return FileSystemPath.Parse(virtualPath);
        }
    }
}
