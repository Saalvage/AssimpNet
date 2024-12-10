/*
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
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Assimp.Configs;
using Assimp.Unmanaged;

namespace Assimp
{
    /// <summary>
    /// Represents an Assimp Import/Export context that load or save models using the unmanaged library. Additionally, conversion
    /// functionality is offered to bypass loading model data into managed memory.
    /// </summary>
    public sealed class AssimpContext : IDisposable
    {
        private bool m_isDisposed;
        private Dictionary<string, PropertyConfig> m_configs;
        private IOSystem m_ioSystem;

        private ExportFormatDescription[] m_exportFormats;
        private string[] m_importFormats;
        private ImporterDescription[] m_importerDescrs;

        private float m_scale = 1.0f;
        private float m_xAxisRotation = 0.0f;
        private float m_yAxisRotation = 0.0f;
        private float m_zAxisRotation = 0.0f;
        private bool m_buildMatrix = false;
        private Matrix4x4 m_scaleRot = Matrix4x4.Identity;

        private IntPtr m_propStore = IntPtr.Zero;

        /// <summary>
        /// Gets if the context has been disposed.
        /// </summary>
        public bool IsDisposed => m_isDisposed;

        /// <summary>
        /// Gets or sets the uniform scale for the model. This is multiplied
        /// with the existing root node's transform. This is only used during import.
        /// </summary>
        public float Scale
        {
            get => m_scale;
            set
            {
                if(m_scale != value)
                {
                    m_scale = value;
                    m_buildMatrix = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the model's rotation about the X-Axis, in degrees. This is multiplied
        /// with the existing root node's transform. This is only used during import.
        /// </summary>
        public float XAxisRotation
        {
            get => m_xAxisRotation;
            set
            {
                if(m_xAxisRotation != value)
                {
                    m_xAxisRotation = value;
                    m_buildMatrix = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the model's rotation abut the Y-Axis, in degrees. This is multiplied
        /// with the existing root node's transform. This is only used during import.
        /// </summary>
        public float YAxisRotation
        {
            get => m_yAxisRotation;
            set
            {
                if(m_yAxisRotation != value)
                {
                    m_yAxisRotation = value;
                    m_buildMatrix = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the model's rotation about the Z-Axis, in degrees. This is multiplied
        /// with the existing root node's transform. This is only used during import.
        /// </summary>
        public float ZAxisRotation
        {
            get => m_zAxisRotation;
            set
            {
                if(m_zAxisRotation != value)
                {
                    m_zAxisRotation = value;
                    m_buildMatrix = true;
                }
            }
        }

        /// <summary>
        /// Gets whether this context is using a user-defined IO system for file handling.
        /// </summary>
        public bool UsingCustomIOSystem => m_ioSystem != null && !m_ioSystem.IsDisposed;

        /// <summary>
        /// Gets the property configurations set to this context. This is only used during import.
        /// </summary>
        public Dictionary<string, PropertyConfig> PropertyConfigurations => m_configs;

        /// <summary>
        /// Constructs a new instance of the <see cref="AssimpContext"/> class.
        /// </summary>
        public AssimpContext()
        {
            m_configs = new Dictionary<string, PropertyConfig>();
        }

        #region Import

        #region ImportFileFromStream

        /// <summary>
        /// Imports a model from the stream without running any post-process steps. The importer sets configurations
        /// and loads the model into managed memory, releasing the unmanaged memory used by Assimp. It is up to the caller to dispose of the stream.
        /// If the format is distributed across multiple files/streams, set a custom <see cref="IOSystem"/>
        /// and use the "ImportFile" family of functions.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="formatHint">Optional format extension to serve as a hint to Assimp to choose which importer to use. If null or empty, the system will
        /// try to detect what importer to use from the data which may or may not be successful.</param>
        /// <returns>The imported scene</returns>
        /// <exception cref="AssimpException">Thrown if the stream is not valid (null or write-only).</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public Scene ImportFileFromStream(Stream stream, string formatHint = null)
        {
            return ImportFileFromStream(stream, PostProcessSteps.None, formatHint);
        }

        /// <summary>
        /// Imports a model from the stream. The importer sets configurations and loads the model into managed memory, releasing the unmanaged memory 
        /// used by Assimp. It is up to the caller to dispose of the stream. If the format is distributed across multiple files/streams, set a custom <see cref="IOSystem"/>
        /// and use the "ImportFile" family of functions.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="postProcessFlags">Post processing flags, if any</param>
        /// <param name="formatHint">Optional format extension to serve as a hint to Assimp to choose which importer to use. If null or empty, the system will
        /// try to detect what importer to use from the data which may or may not be successful.</param>
        /// <returns>The imported scene</returns>
        /// <exception cref="AssimpException">Thrown if the stream is not valid (null or write-only).</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public unsafe Scene ImportFileFromStream(Stream stream, PostProcessSteps postProcessFlags, string formatHint = null)
        {
            CheckDisposed();

            if(stream == null || stream.CanRead != true)
                throw new AssimpException(nameof(stream), "Can't read from the stream it's null or write-only");

            AiScene* ptr = null;
            PrepareImport();

            var bytes = MemoryHelper.ReadStreamFully(stream, 0);

            try
            {
                fixed (byte* buffer = bytes)
                {
                    ptr = AssimpLibrary.aiImportFileFromMemoryWithProperties(buffer, (uint)bytes.Length, PostProcessSteps.None,
                        formatHint, m_propStore);
                }

                if (ptr == null)
                    throw new AssimpException("Error importing file: " + AssimpLibrary.Instance.GetErrorString());

                TransformScene(ptr);

                ptr = ApplyPostProcessing(ptr, postProcessFlags);

                return Scene.FromUnmanagedScene(ptr);
            }
            finally
            {
                CleanupImport();

                if(ptr != null)
                {
                    AssimpLibrary.aiReleaseImport(ptr);
                }
            }
        }

        #endregion

        #region ImportFile

        /// <summary>
        /// Imports a model from the specified file without running any post-process steps. The importer sets configurations
        /// and loads the model into managed memory, releasing the unmanaged memory used by Assimp.
        /// </summary>
        /// <param name="file">Full path to the file</param>
        /// <returns>The imported scene</returns>
        /// <exception cref="AssimpException">Thrown if there was a general error in importing the model.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the file could not be located.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public Scene ImportFile(string file)
        {
            return ImportFile(file, PostProcessSteps.None);
        }

        /// <summary>
        /// Imports a model from the specified file. The importer sets configurations
        /// and loads the model into managed memory, releasing the unmanaged memory used by Assimp.
        /// </summary>
        /// <param name="file">Full path to the file</param>
        /// <param name="postProcessFlags">Post processing flags, if any</param>
        /// <returns>The imported scene</returns>
        /// <exception cref="AssimpException">Thrown if there was a general error in importing the model.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the file could not be located.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public unsafe Scene ImportFile(string file, PostProcessSteps postProcessFlags)
        {
            CheckDisposed();

            AiScene* ptr = null;
            IntPtr fileIO = IntPtr.Zero;

            //Only do file checks if not using a custom IOSystem
            if(UsingCustomIOSystem)
            {
                fileIO = m_ioSystem.AiFileIO;
            }
            else if(string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                throw new FileNotFoundException("Filename was null or could not be found", file);
            }

            PrepareImport();

            try
            {
                ptr = AssimpLibrary.aiImportFileExWithProperties(file, PostProcessSteps.None, (AiFileIO*)fileIO, m_propStore);

                if(ptr == null)
                    throw new AssimpException("Error importing file: " + AssimpLibrary.Instance.GetErrorString());

                TransformScene(ptr);

                ptr = (AiScene*)ApplyPostProcessing(ptr, postProcessFlags);

                return Scene.FromUnmanagedScene(ptr);
            }
            finally
            {
                CleanupImport();

                if(ptr != null)
                {
                    AssimpLibrary.aiReleaseImport(ptr);
                }
            }
        }

        #endregion

        #endregion

        #region Export

        #region ExportFile

        /// <summary>
        /// Exports a scene to the specified format and writes it to a file.
        /// </summary>
        /// <param name="scene">Scene containing the model to export.</param>
        /// <param name="fileName">Path to the file.</param>
        /// <param name="exportFormatId">FormatID representing the format to export to.</param>
        /// <returns>True if the scene was exported successfully, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the scene is null.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public bool ExportFile(Scene scene, string fileName, string exportFormatId)
        {
            return ExportFile(scene, fileName, exportFormatId, PostProcessSteps.None);
        }

        /// <summary>
        /// Exports a scene to the specified format and writes it to a file.
        /// </summary>
        /// <param name="scene">Scene containing the model to export.</param>
        /// <param name="fileName">Path to the file.</param>
        /// <param name="exportFormatId">FormatID representing the format to export to.</param>
        /// <param name="preProcessing">Preprocessing flags to apply to the model before it is exported.</param>
        /// <returns>True if the scene was exported successfully, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the scene is null.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public bool ExportFile(Scene scene, string fileName, string exportFormatId, PostProcessSteps preProcessing)
        {
            CheckDisposed();

            IntPtr fileIO = IntPtr.Zero;
            IntPtr scenePtr = IntPtr.Zero;

            if(scene == null)
                throw new ArgumentNullException(nameof(scene), "Scene must exist.");

            if (!TestIfExportIdIsValid(exportFormatId))
                return false;

            try
            {
                scenePtr = Scene.ToUnmanagedScene(scene);

                ReturnCode status = AssimpLibrary.Instance.ExportScene(scenePtr, exportFormatId, fileName, fileIO, preProcessing);

                return status == ReturnCode.Success;
            }
            finally
            {
                if(scenePtr != IntPtr.Zero)
                    Scene.FreeUnmanagedScene(scenePtr);
            }
        }

        #endregion

        #region ExportToBlob

        /// <summary>
        /// Exports a scene to the specified format and writes it to a data blob.
        /// </summary>
        /// <param name="scene">Scene containing the model to export.</param>
        /// <param name="exportFormatId">FormatID representing the format to export to.</param>
        /// <returns>The resulting data blob, or null if the export failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the scene is null.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public ExportDataBlob ExportToBlob(Scene scene, string exportFormatId)
        {
            return ExportToBlob(scene, exportFormatId, PostProcessSteps.None);
        }

        /// <summary>
        /// Exports a scene to the specified format and writes it to a data blob.
        /// </summary>
        /// <param name="scene">Scene containing the model to export.</param>
        /// <param name="exportFormatId">FormatID representing the format to export to.</param>
        /// <param name="preProcessing">Preprocessing flags to apply to the model before it is exported.</param>
        /// <returns>The resulting data blob, or null if the export failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the scene is null.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public ExportDataBlob ExportToBlob(Scene scene, string exportFormatId, PostProcessSteps preProcessing)
        {
            CheckDisposed();

            IntPtr fileIO = IntPtr.Zero;
            IntPtr scenePtr = IntPtr.Zero;

            if(scene == null)
                throw new ArgumentNullException(nameof(scene), "Scene must exist.");

            if (!TestIfExportIdIsValid(exportFormatId))
                return null;

            try
            {
                scenePtr = Scene.ToUnmanagedScene(scene);

                return AssimpLibrary.Instance.ExportSceneToBlob(scenePtr, exportFormatId, preProcessing);
            }
            finally
            {
                if(scenePtr != IntPtr.Zero)
                    Scene.FreeUnmanagedScene(scenePtr);
            }
        }

        #endregion

        #endregion

        #region ConvertFromFile

        #region File to File

        /// <summary>
        /// Converts the model contained in the file to the specified format and save it to a file.
        /// </summary>
        /// <param name="inputFilename">Input file name to import</param>
        /// <param name="outputFilename">Output file name to export to</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <returns>True if the conversion was successful or not, false otherwise.</returns>
        /// <exception cref="AssimpException">Thrown if there was a general error in importing the model.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the file could not be located.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public bool ConvertFromFileToFile(string inputFilename, string outputFilename, string exportFormatId)
        {
            return ConvertFromFileToFile(inputFilename, PostProcessSteps.None, outputFilename, exportFormatId, PostProcessSteps.None);
        }

        /// <summary>
        /// Converts the model contained in the file to the specified format and save it to a file.
        /// </summary>
        /// <param name="inputFilename">Input file name to import</param>
        /// <param name="outputFilename">Output file name to export to</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <param name="exportProcessSteps">Pre processing steps used for the export</param>
        /// <returns>True if the conversion was successful or not, false otherwise.</returns>
        /// <exception cref="AssimpException">Thrown if there was a general error in importing the model.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the file could not be located.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public bool ConvertFromFileToFile(string inputFilename, string outputFilename, string exportFormatId, PostProcessSteps exportProcessSteps)
        {
            return ConvertFromFileToFile(inputFilename, PostProcessSteps.None, outputFilename, exportFormatId, exportProcessSteps);
        }

        /// <summary>
        /// Converts the model contained in the file to the specified format and save it to a file.
        /// </summary>
        /// <param name="inputFilename">Input file name to import</param>
        /// <param name="importProcessSteps">Post processing steps used for the import</param>
        /// <param name="outputFilename">Output file name to export to</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <param name="exportProcessSteps">Pre processing steps used for the export</param>
        /// <returns>True if the conversion was successful or not, false otherwise.</returns>
        /// <exception cref="AssimpException">Thrown if there was a general error in importing the model.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the file could not be located.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public unsafe bool ConvertFromFileToFile(string inputFilename, PostProcessSteps importProcessSteps, string outputFilename, string exportFormatId, PostProcessSteps exportProcessSteps)
        {
            CheckDisposed();

            if (!TestIfExportIdIsValid(exportFormatId))
                return false;

            IntPtr ptr = IntPtr.Zero;
            IntPtr fileIO = IntPtr.Zero;

            //Only do file checks if not using a custom IOSystem
            if(UsingCustomIOSystem)
            {
                fileIO = m_ioSystem.AiFileIO;
            }
            else if(string.IsNullOrEmpty(inputFilename) || !File.Exists(inputFilename))
            {
                throw new FileNotFoundException("Filename was null or could not be found", inputFilename);
            }

            PrepareImport();

            try
            {
                ptr = (IntPtr)AssimpLibrary.aiImportFileExWithProperties(inputFilename, PostProcessSteps.None, (AiFileIO*)fileIO, m_propStore);

                if(ptr == IntPtr.Zero)
                    throw new AssimpException("Error importing file: " + AssimpLibrary.Instance.GetErrorString());

                TransformScene(ptr);

                ptr = ApplyPostProcessing(ptr, importProcessSteps);

                ReturnCode status = AssimpLibrary.Instance.ExportScene(ptr, exportFormatId, outputFilename, fileIO, exportProcessSteps);

                return status == ReturnCode.Success;
            }
            finally
            {
                CleanupImport();

                if(ptr != IntPtr.Zero)
                    AssimpLibrary.Instance.ReleaseImport(ptr);
            }
        }

        #endregion

        #region File to Blob

        /// <summary>
        /// Converts the model contained in the file to the specified format and save it to a data blob.
        /// </summary>
        /// <param name="inputFilename">Input file name to import</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <returns>Data blob containing the exported scene in a binary form</returns>
        /// <exception cref="AssimpException">Thrown if there was a general error in importing the model.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the file could not be located.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public ExportDataBlob ConvertFromFileToBlob(string inputFilename, string exportFormatId)
        {
            return ConvertFromFileToBlob(inputFilename, PostProcessSteps.None, exportFormatId, PostProcessSteps.None);
        }

        /// <summary>
        /// Converts the model contained in the file to the specified format and save it to a data blob.
        /// </summary>
        /// <param name="inputFilename">Input file name to import</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <param name="exportProcessSteps">Pre processing steps used for the export</param>
        /// <returns>Data blob containing the exported scene in a binary form</returns>
        /// <exception cref="AssimpException">Thrown if there was a general error in importing the model.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the file could not be located.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public ExportDataBlob ConvertFromFileToBlob(string inputFilename, string exportFormatId, PostProcessSteps exportProcessSteps)
        {
            return ConvertFromFileToBlob(inputFilename, PostProcessSteps.None, exportFormatId, exportProcessSteps);
        }

        /// <summary>
        /// Converts the model contained in the file to the specified format and save it to a data blob.
        /// </summary>
        /// <param name="inputFilename">Input file name to import</param>
        /// <param name="importProcessSteps">Post processing steps used for the import</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <param name="exportProcessSteps">Pre processing steps used for the export</param>
        /// <returns>Data blob containing the exported scene in a binary form</returns>
        /// <exception cref="AssimpException">Thrown if there was a general error in importing the model.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the file could not be located.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public unsafe ExportDataBlob ConvertFromFileToBlob(string inputFilename, PostProcessSteps importProcessSteps, string exportFormatId, PostProcessSteps exportProcessSteps)
        {
            CheckDisposed();

            if (!TestIfExportIdIsValid(exportFormatId))
                return null;

            IntPtr ptr = IntPtr.Zero;
            IntPtr fileIO = IntPtr.Zero;

            //Only do file checks if not using a custom IOSystem
            if(UsingCustomIOSystem)
            {
                fileIO = m_ioSystem.AiFileIO;
            }
            else if(string.IsNullOrEmpty(inputFilename) || !File.Exists(inputFilename))
            {
                throw new FileNotFoundException("Filename was null or could not be found", inputFilename);
            }

            PrepareImport();

            try
            {
                ptr = (IntPtr)AssimpLibrary.aiImportFileExWithProperties(inputFilename, PostProcessSteps.None, (AiFileIO*)fileIO, m_propStore);

                if(ptr == IntPtr.Zero)
                    throw new AssimpException("Error importing file: " + AssimpLibrary.Instance.GetErrorString());

                TransformScene(ptr);

                ptr = ApplyPostProcessing(ptr, importProcessSteps);

                return AssimpLibrary.Instance.ExportSceneToBlob(ptr, exportFormatId, exportProcessSteps);
            }
            finally
            {
                CleanupImport();

                if(ptr != IntPtr.Zero)
                    AssimpLibrary.Instance.ReleaseImport(ptr);
            }
        }

        #endregion

        #endregion

        #region ConvertFromStream

        #region Stream to File

        /// <summary>
        /// Converts the model contained in the stream to the specified format and save it to a file. It is up to the caller to dispose of the stream.
        /// If the format is distributed across multiple files/streams, set a custom <see cref="IOSystem"/>
        /// and use the "ConvertFromFileToFile" family of functions.
        /// </summary>
        /// <param name="inputStream">Stream to read from</param>
        /// <param name="importFormatHint">Optional format extension to serve as a hint to Assimp to choose which importer to use. If null or empty, the system will
        /// try to detect what importer to use from the data which may or may not be successful</param>
        /// <param name="outputFilename">Output file name to export to</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <returns>True if the conversion was successful or not, false otherwise.</returns>
        /// <exception cref="AssimpException">Thrown if the stream is not valid (null or write-only).</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public bool ConvertFromStreamToFile(Stream inputStream, string importFormatHint, string outputFilename, string exportFormatId)
        {
            return ConvertFromStreamToFile(inputStream, importFormatHint, PostProcessSteps.None, outputFilename, exportFormatId, PostProcessSteps.None);
        }

        /// <summary>
        /// Converts the model contained in the stream to the specified format and save it to a file. It is up to the caller to dispose of the stream.
        /// If the format is distributed across multiple files/streams, set a custom <see cref="IOSystem"/>
        /// and use the "ConvertFromFileToFile" family of functions.
        /// </summary>
        /// <param name="inputStream">Stream to read from</param>
        /// <param name="importFormatHint">Optional format extension to serve as a hint to Assimp to choose which importer to use. If null or empty, the system will
        /// try to detect what importer to use from the data which may or may not be successful</param>
        /// <param name="outputFilename">Output file name to export to</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <param name="exportProcessSteps">Pre processing steps used for the export</param>
        /// <returns>True if the conversion was successful or not, false otherwise.</returns>
        /// <exception cref="AssimpException">Thrown if the stream is not valid (null or write-only).</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public bool ConvertFromStreamToFile(Stream inputStream, string importFormatHint, string outputFilename, string exportFormatId, PostProcessSteps exportProcessSteps)
        {
            return ConvertFromStreamToFile(inputStream, importFormatHint, PostProcessSteps.None, outputFilename, exportFormatId, exportProcessSteps);
        }

        /// <summary>
        /// Converts the model contained in the stream to the specified format and save it to a file. It is up to the caller to dispose of the stream.
        /// If the format is distributed across multiple files/streams, set a custom <see cref="IOSystem"/>
        /// and use the "ConvertFromFileToFile" family of functions.
        /// </summary>
        /// <param name="inputStream">Stream to read from</param>
        /// <param name="importFormatHint">Optional format extension to serve as a hint to Assimp to choose which importer to use. If null or empty, the system will
        /// try to detect what importer to use from the data which may or may not be successful</param>
        /// <param name="importProcessSteps">Post processing steps used for import</param>
        /// <param name="outputFilename">Output file name to export to</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <param name="exportProcessSteps">Pre processing steps used for the export</param>
        /// <returns>True if the conversion was successful or not, false otherwise.</returns>
        /// <exception cref="AssimpException">Thrown if the stream is not valid (null or write-only).</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public bool ConvertFromStreamToFile(Stream inputStream, string importFormatHint, PostProcessSteps importProcessSteps, string outputFilename, string exportFormatId, PostProcessSteps exportProcessSteps)
        {
            CheckDisposed();

            if(inputStream == null || inputStream.CanRead != true)
                throw new AssimpException("stream", "Can't read from the stream it's null or write-only");

            if (!TestIfExportIdIsValid(exportFormatId))
                return false;

            IntPtr ptr = IntPtr.Zero;
            PrepareImport();

            try
            {
                ptr = AssimpLibrary.Instance.ImportFileFromStream(inputStream, importProcessSteps, importFormatHint, m_propStore);

                if(ptr == IntPtr.Zero)
                    throw new AssimpException("Error importing file: " + AssimpLibrary.Instance.GetErrorString());

                TransformScene(ptr);

                ptr = ApplyPostProcessing(ptr, importProcessSteps);

                ReturnCode status = AssimpLibrary.Instance.ExportScene(ptr, exportFormatId, outputFilename, exportProcessSteps);

                return status == ReturnCode.Success;
            }
            finally
            {
                CleanupImport();

                if(ptr != IntPtr.Zero)
                    AssimpLibrary.Instance.ReleaseImport(ptr);
            }
        }

        #endregion

        #region Stream to Blob

        /// <summary>
        /// Converts the model contained in the stream to the specified format and save it to a data blob. It is up to the caller to dispose of the stream.
        /// If the format is distributed across multiple files/streams, set a custom <see cref="IOSystem"/>
        /// and use the "ConvertFromFileToBlob" family of functions.
        /// </summary>
        /// <param name="inputStream">Stream to read from</param>
        /// <param name="importFormatHint">Optional format extension to serve as a hint to Assimp to choose which importer to use. If null or empty, the system will
        /// try to detect what importer to use from the data which may or may not be successful</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <returns>Data blob containing the exported scene in a binary form</returns>
        /// <exception cref="AssimpException">Thrown if the stream is not valid (null or write-only).</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public ExportDataBlob ConvertFromStreamToBlob(Stream inputStream, string importFormatHint, string exportFormatId)
        {
            return ConvertFromStreamToBlob(inputStream, importFormatHint, PostProcessSteps.None, exportFormatId, PostProcessSteps.None);
        }

        /// <summary>
        /// Converts the model contained in the stream to the specified format and save it to a data blob. It is up to the caller to dispose of the stream.
        /// If the format is distributed across multiple files/streams, set a custom <see cref="IOSystem"/>
        /// and use the "ConvertFromFileToBlob" family of functions.
        /// </summary>
        /// <param name="inputStream">Stream to read from</param>
        /// <param name="importFormatHint">Optional format extension to serve as a hint to Assimp to choose which importer to use. If null or empty, the system will
        /// try to detect what importer to use from the data which may or may not be successful</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <param name="exportProcessSteps">Pre processing steps used for the export</param>
        /// <returns>Data blob containing the exported scene in a binary form</returns>
        /// <exception cref="AssimpException">Thrown if the stream is not valid (null or write-only).</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public ExportDataBlob ConvertFromStreamToBlob(Stream inputStream, string importFormatHint, string exportFormatId, PostProcessSteps exportProcessSteps)
        {
            return ConvertFromStreamToBlob(inputStream, importFormatHint, PostProcessSteps.None, exportFormatId, exportProcessSteps);
        }

        /// <summary>
        /// Converts the model contained in the stream to the specified format and save it to a data blob. It is up to the caller to dispose of the stream.
        /// If the format is distributed across multiple files/streams, set a custom <see cref="IOSystem"/>
        /// and use the "ConvertFromFileToBlob" family of functions.
        /// </summary>
        /// <param name="inputStream">Stream to read from</param>
        /// <param name="importFormatHint">Optional format extension to serve as a hint to Assimp to choose which importer to use. If null or empty, the system will
        /// try to detect what importer to use from the data which may or may not be successful</param>
        /// <param name="importProcessSteps">Post processing steps used for import</param>
        /// <param name="exportFormatId">Format id that specifies what format to export to</param>
        /// <param name="exportProcessSteps">Pre processing steps used for the export</param>
        /// <returns>Data blob containing the exported scene in a binary form</returns>
        /// <exception cref="AssimpException">Thrown if the stream is not valid (null or write-only).</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the context has already been disposed of.</exception>
        public ExportDataBlob ConvertFromStreamToBlob(Stream inputStream, string importFormatHint, PostProcessSteps importProcessSteps, string exportFormatId, PostProcessSteps exportProcessSteps)
        {
            CheckDisposed();

            if(inputStream == null || inputStream.CanRead != true)
                throw new AssimpException("stream", "Can't read from the stream it's null or write-only");

            if (!TestIfExportIdIsValid(exportFormatId))
                return null;

            IntPtr ptr = IntPtr.Zero;
            PrepareImport();

            try
            {
                ptr = AssimpLibrary.Instance.ImportFileFromStream(inputStream, importProcessSteps, importFormatHint, m_propStore);

                if(ptr == IntPtr.Zero)
                    throw new AssimpException("Error importing file: " + AssimpLibrary.Instance.GetErrorString());

                TransformScene(ptr);

                ptr = ApplyPostProcessing(ptr, importProcessSteps);

                return AssimpLibrary.Instance.ExportSceneToBlob(ptr, exportFormatId, exportProcessSteps);
            }
            finally
            {
                CleanupImport();

                if(ptr != IntPtr.Zero)
                    AssimpLibrary.Instance.ReleaseImport(ptr);
            }
        }

        #endregion

        #endregion

        #region IOSystem

        /// <summary>
        /// Sets a custom file system implementation that is used by this importer. If it is null, then the default assimp file system
        /// is used instead.
        /// </summary>
        /// <param name="ioSystem">Custom file system implementation</param>
        public void SetIOSystem(IOSystem ioSystem)
        {
            if(ioSystem == null || ioSystem.IsDisposed)
                ioSystem = null;

            m_ioSystem = ioSystem;
        }

        /// <summary>
        /// Removes the currently set custom file system implementation from the importer.
        /// </summary>
        public void RemoveIOSystem()
        {
            m_ioSystem = null;
        }

        #endregion

        #region Format support

        /// <summary>
        /// Gets the model formats that are supported for export by Assimp.
        /// </summary>
        /// <returns>Export formats supported</returns>
        public ExportFormatDescription[] GetSupportedExportFormats()
        {
            QueryExportFormatsIfNecessary();

            return (ExportFormatDescription[]) m_exportFormats.Clone();
        }

        /// <summary>
        /// Gets the model formats that are supported for import by Assimp.
        /// </summary>
        /// <returns>Import formats supported</returns>
        public string[] GetSupportedImportFormats()
        {
            QueryImportFormatsIfNecessary();

            return (string[]) m_importFormats.Clone();
        }

        /// <summary>
        /// Gets descriptions for each importer that assimp has registered.
        /// </summary>
        /// <returns>Descriptions of supported importers.</returns>
        public ImporterDescription[] GetImporterDescriptions()
        {
            QueryImporterDescriptionsIfNecessary();

            return (ImporterDescription[]) m_importerDescrs.Clone();
        }

        /// <summary>
        /// Gets an importer description for the specified file extension. If no importers support it, null is returned. Multiple importers may support the file extension,
        /// they are called in the order that they were registered.
        /// </summary>
        /// <param name="fileExtension">File extension to query importer support for.</param>
        /// <returns>Importer description or null if it does not exist.</returns>
        public ImporterDescription GetImporterDescriptionFor(string fileExtension)
        {
            if(string.IsNullOrEmpty(fileExtension))
                return null;

            QueryImporterDescriptionsIfNecessary();

            if(fileExtension.StartsWith(".") && fileExtension.Length >= 2)
                fileExtension = fileExtension[1..];

            foreach(ImporterDescription descr in m_importerDescrs)
            {
                foreach(string ext in descr.FileExtensions)
                {
                    if(string.Equals(fileExtension, ext, StringComparison.Ordinal))
                        return descr;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the format extension (e.g. ".dae" or ".obj") is supported for import.
        /// </summary>
        /// <param name="format">Model format</param>
        /// <returns>True if the format is supported, false otherwise</returns>
        public bool IsImportFormatSupported(string format)
        {
            return AssimpLibrary.Instance.IsExtensionSupported(format);
        }

        /// <summary>
        /// Checks if the format extension (e.g. ".dae" or ".obj") is supported for export.
        /// </summary>
        /// <param name="format">Model format</param>
        /// <returns>True if the format is supported, false otherwise</returns>
        public bool IsExportFormatSupported(string format)
        {
            if(string.IsNullOrEmpty(format))
                return false;

            QueryExportFormatsIfNecessary();

            if(format.StartsWith(".") && format.Length >= 2)
                format = format[1..];

            foreach(ExportFormatDescription desc in m_exportFormats)
            {
                if(string.Equals(desc.FileExtension, format, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        #endregion

        #region Configs

        /// <summary>
        /// Sets a configuration property to the context. This is only used during import.
        /// </summary>
        /// <param name="config">Config to set</param>
        public void SetConfig(PropertyConfig config)
        {
            if(config == null)
                return;

            string name = config.Name;
            m_configs[config.Name] = config;
        }

        /// <summary>
        /// Removes a set configuration property by name.
        /// </summary>
        /// <param name="configName">Name of the config property</param>
        public void RemoveConfig(string configName)
        {
            if(string.IsNullOrEmpty(configName))
                return;

            m_configs.Remove(configName);
        }

        /// <summary>
        /// Removes all configuration properties from the context.
        /// </summary>
        public void RemoveConfigs()
        {
            m_configs.Clear();
        }

        /// <summary>
        /// Checks if the context has a config set by the specified name.
        /// </summary>
        /// <param name="configName">Name of the config property</param>
        /// <returns>True if the config is present, false otherwise</returns>
        public bool ContainsConfig(string configName)
        {
            if(string.IsNullOrEmpty(configName))
                return false;

            return m_configs.ContainsKey(configName);
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Disposes of resources held by the context. These include IO systems still attached.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; False to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if(!m_isDisposed)
            {
                if(disposing)
                {
                    if(UsingCustomIOSystem)
                        m_ioSystem.Dispose();
                }

                m_isDisposed = true;
            }
        }

        #endregion

        #region Private methods

        private void CheckDisposed()
        {
            if(m_isDisposed)
                throw new ObjectDisposedException("Assimp Context has been disposed.");
        }

        private void QueryExportFormatsIfNecessary()
        {
            if(m_exportFormats == null)
                m_exportFormats = AssimpLibrary.Instance.GetExportFormatDescriptions();
        }

        private void QueryImportFormatsIfNecessary()
        {
            m_importFormats ??= AssimpLibrary.Instance.GetExtensionList();
        }

        private void QueryImporterDescriptionsIfNecessary()
        {
            m_importerDescrs ??= AssimpLibrary.Instance.GetImporterDescriptions();
        }

        //Build import transformation matrix
        private void BuildMatrix()
        {

            if(m_buildMatrix)
            {
                Matrix4x4 scale = Matrix4x4.CreateScale(new Vector3(m_scale, m_scale, m_scale));
                Matrix4x4 xRot = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, m_xAxisRotation * (float) (Math.PI / 180.0d));
                Matrix4x4 yRot = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, m_yAxisRotation * (float) (Math.PI / 180.0d));
                Matrix4x4 zRot = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, m_zAxisRotation * (float) (Math.PI / 180.0d));
                m_scaleRot = scale * ((xRot * yRot) * zRot);
            }

            m_buildMatrix = false;
        }

        //Transforms the root node of the scene and writes it back to the native structure
        private unsafe bool TransformScene(AiScene* scene)
        {
            BuildMatrix();

            try
            {
                if(!m_scaleRot.IsIdentity)
                {
                    if(scene->RootNode == IntPtr.Zero)
                        return false;

                    IntPtr matrixPtr = scene->RootNode + MemoryHelper.SizeOf<AiString>(); //Skip over Node Name

                    Matrix4x4 matrix = MemoryHelper.Read<Matrix4x4>(matrixPtr); //Get the root transform
                    matrix = matrix * m_scaleRot; //Transform

                    //Write back to unmanaged mem
                    MemoryHelper.Write<Matrix4x4>(matrixPtr, matrix);

                    return true;
                }
            }
            catch(Exception)
            {

            }

            return false;
        }

        private unsafe AiScene* ApplyPostProcessing(AiScene* scene, PostProcessSteps postProcessFlags)
        {
            if(postProcessFlags == PostProcessSteps.None || scene == null)
                return scene;

            scene = AssimpLibrary.aiApplyPostProcessing(scene, postProcessFlags);

            if(scene == null)
            {
                string error = AssimpLibrary.Instance.GetErrorString();
                throw new AssimpException(error.Length == 0
                    //Postprocessing steps don't usually set the error string when failing
                    ? "Error applying post processing, see log for more info."
                    : "Error applying post processing: " + error);
            }

            return scene;
        }

        //Creates all property stores and sets their values
        private void CreateConfigs()
        {
            m_propStore = AssimpLibrary.Instance.CreatePropertyStore();

            foreach(KeyValuePair<string, PropertyConfig> config in m_configs)
            {
                config.Value.ApplyValue(m_propStore);
            }
        }

        //Destroys all property stores
        private void ReleaseConfigs()
        {
            if(m_propStore != IntPtr.Zero)
                AssimpLibrary.Instance.ReleasePropertyStore(m_propStore);
        }

        //Does all the necessary prep work before we import
        private void PrepareImport()
        {
            CreateConfigs();
        }

        //Does all the necessary cleanup work after we import
        private void CleanupImport()
        {
            ReleaseConfigs();

            //Noticed that sometimes Assimp doesn't call Close() callbacks always, so ensure we clean up those up here
            if(UsingCustomIOSystem)
            {
                m_ioSystem.CloseAllFiles();
            }
        }

        //Tests if a export format ID matches any in the supported list, and if not logs a warning
        private bool TestIfExportIdIsValid(string exportFormatId)
        {
            m_exportFormats ??= AssimpLibrary.Instance.GetExportFormatDescriptions();

            foreach(ExportFormatDescription descr in m_exportFormats)
            {
                if (descr.FormatId.Equals(exportFormatId, StringComparison.Ordinal))
                    return true;
            }

            //Assimp doesn't seem to emit a logstream message, so make sure we log that the format ID is not valid
            IEnumerable<LogStream> loggers = LogStream.GetAttachedLogStreams();

            foreach(LogStream logger in loggers)
            {
                logger.Log($"Info,  Invalid export format: {exportFormatId}");
            }

            return false;
        }

        #endregion

    }
}
