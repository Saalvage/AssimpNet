﻿/*
* Copyright (c) 2012-2020 AssimpNet - Nicholas Woodfield
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Assimp.Unmanaged;

namespace Assimp
{
    /// <summary>
    /// Metadata and feature support information for a given importer.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public sealed class ImporterDescription
    {
        private string m_name;
        private string m_author;
        private string m_maintainer;
        private string m_comments;
        private ImporterFeatureFlags m_featureFlags;
        private Version m_minVersion;
        private Version m_maxVersion;
        private string[] m_fileExtensions;

        /// <summary>
        /// Gets the name of the importer (e.g. Blender3D Importer)
        /// </summary>
        public string Name => m_name;

        /// <summary>
        /// Gets the original author (blank if unknown or assimp team).
        /// </summary>
        public string Author => m_author;

        /// <summary>
        /// Gets the name of the current maintainer, if empty then the author maintains.
        /// </summary>
        public string Maintainer => m_maintainer;

        /// <summary>
        /// Gets any implementation comments.
        /// </summary>
        public string Comments => m_comments;

        /// <summary>
        /// Gets the features supported by the importer.
        /// </summary>
        public ImporterFeatureFlags FeatureFlags => m_featureFlags;

        /// <summary>
        /// Gets the minimum version of the file format supported. If no version scheme, forwards compatible, or importer doesn't care, major/min will be zero.
        /// </summary>
        public Version MinVersion => m_minVersion;

        /// <summary>
        /// Gets the maximum version of the file format supported. If no version scheme, forwards compatible, or importer doesn't care, major/min will be zero.
        /// </summary>
        public Version MaxVersion => m_maxVersion;

        /// <summary>
        /// Gets the list of file extensions the importer can handle. All entries are lower case and do NOT have a leading dot.
        /// </summary>
        public string[] FileExtensions => m_fileExtensions;

        internal ImporterDescription(in AiImporterDesc descr)
        {
            m_name = Marshal.PtrToStringAnsi(descr.Name);
            m_author = Marshal.PtrToStringAnsi(descr.Author);
            m_maintainer = Marshal.PtrToStringAnsi(descr.Maintainer);
            m_comments = Marshal.PtrToStringAnsi(descr.Comments);
            m_featureFlags = descr.Flags;
            m_minVersion = new Version((int) descr.MinMajor, (int) descr.MinMinor);
            m_maxVersion = new Version((int) descr.MaxMajor, (int) descr.MaxMajor);

            string fileExts = Marshal.PtrToStringAnsi(descr.FileExtensions);
            if(string.IsNullOrEmpty(fileExts))
            {
                m_fileExtensions = [];
            }
            else
            {
                m_fileExtensions = fileExts.Split(' ');
            }
        }
    }
}
