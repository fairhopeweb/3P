#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (ZipInfo.cs) is part of 3P.
// 
// 3P is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// 3P is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with 3P. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WixToolset.Dtf.Compression.Zip {
    /// <summary>
    /// Object representing a zip file on disk; provides access to
    /// file-based operations on the zip file.
    /// </summary>
    /// <remarks>
    /// Generally, the methods on this class are much easier to use than the
    /// stream-based interfaces provided by the <see cref="ZipEngine"/> class.
    /// </remarks>
    [Serializable]
    public class ZipInfo : ArchiveInfo {
        /// <summary>
        /// Creates a new CabinetInfo object representing a zip file in a specified path.
        /// </summary>
        /// <param name="path">The path to the zip file. When creating a zip file, this file does not
        /// necessarily exist yet.</param>
        public ZipInfo(string path)
            : base(path) {}

        /// <summary>
        /// Initializes a new instance of the CabinetInfo class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected ZipInfo(SerializationInfo info, StreamingContext context)
            : base(info, context) {}

        /// <summary>
        /// Creates a compression engine that does the low-level work for
        /// this object.
        /// </summary>
        /// <returns>A new <see cref="ZipEngine"/> instance.</returns>
        /// <remarks>
        /// Each instance will be <see cref="CompressionEngine.Dispose()"/>d
        /// immediately after use.
        /// </remarks>
        protected override CompressionEngine CreateCompressionEngine() {
            return new ZipEngine();
        }

        /// <summary>
        /// Gets information about the files contained in the archive.
        /// </summary>
        /// <returns>A list of <see cref="ZipFileInfo"/> objects, each
        /// containing information about a file in the archive.</returns>
        public new IList<ZipFileInfo> GetFiles() {
            IList<ArchiveFileInfo> files = base.GetFiles();
            List<ZipFileInfo> zipFiles = new List<ZipFileInfo>(files.Count);
            foreach (ZipFileInfo zipFile in files) zipFiles.Add(zipFile);
            return zipFiles.AsReadOnly();
        }

        /// <summary>
        /// Gets information about the certain files contained in the archive file.
        /// </summary>
        /// <param name="searchPattern">The search string, such as
        /// &quot;*.txt&quot;.</param>
        /// <returns>A list of <see cref="ZipFileInfo"/> objects, each containing
        /// information about a file in the archive.</returns>
        public new IList<ZipFileInfo> GetFiles(string searchPattern) {
            IList<ArchiveFileInfo> files = base.GetFiles(searchPattern);
            List<ZipFileInfo> zipFiles = new List<ZipFileInfo>(files.Count);
            foreach (ZipFileInfo zipFile in files) zipFiles.Add(zipFile);
            return zipFiles.AsReadOnly();
        }
    }
}