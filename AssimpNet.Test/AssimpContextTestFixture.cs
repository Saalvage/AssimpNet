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
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Numerics;
using Assimp.Configs;
using Assimp.Unmanaged;
using NUnit.Framework;

namespace Assimp.Test
{
    [TestFixture]
    public class AssimpContextTestFixture
    {
        [OneTimeSetUp]
        public void Setup()
        {
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output");

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            IEnumerable<String> filePaths = Directory.GetFiles(outputPath);

            foreach(String filePath in filePaths)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test]
        public void TestExportBadFormatId()
        {
            AssimpContext importer = new AssimpContext();
            NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
            importer.SetConfig(config);

            LogStream logStream = new LogStream(delegate (string msg, string userData)
            {
                Console.WriteLine(msg);
            });
            logStream.Attach();

            Scene collada = importer.ImportFile(Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae"));

            bool success = importer.ExportFile(collada, Path.Combine(TestHelper.RootPath, "TestFiles/output/exportedCollada.dae"), "dae");

            Assert.IsFalse(success);

            success = importer.ExportFile(collada, Path.Combine(TestHelper.RootPath, "TestFiles/output/exportedCollada.dae"), "collada");

            Assert.IsTrue(success);
        }

        [Test]
        public void TestExportToBlob()
        {
            String colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            AssimpContext context = new AssimpContext();
            Scene ducky = context.ImportFile(colladaPath);
            ExportDataBlob blob = context.ExportToBlob(ducky, "obj");

            Assert.IsTrue(blob.HasData);
            Assert.IsTrue(blob.NextBlob != null);
            Assert.IsTrue(blob.NextBlob.Name.Equals("mtl"));
        }

        [Test]
        public void TestImportExportFile()
        {
            String colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            String plyPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck.ply");

            AssimpContext context = new AssimpContext();
            Scene ducky = context.ImportFile(colladaPath);
            context.ExportFile(ducky, plyPath, "ply");
        }

        [Test]
        public void TestImportExportImportFile()
        {
            String colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            String plyPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck2.dae");

            AssimpContext context = new AssimpContext();
            Scene ducky = context.ImportFile(colladaPath);
            context.ExportFile(ducky, plyPath, "collada");

            Scene ducky2 = context.ImportFile(plyPath);
            Assert.IsNotNull(ducky2);
        }

        [Test]
        public void TestExportToFile()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/ExportedTriangle.obj");

            //Create a very simple scene a single node with a mesh that has a single face, a triangle and a default material
            Scene scene = new Scene();
            scene.RootNode = new Node("Root");

            Mesh triangle = new Mesh("", PrimitiveType.Triangle);
            triangle.Vertices.Add(new Vector3(1, 0, 0));
            triangle.Vertices.Add(new Vector3(5, 5, 0));
            triangle.Vertices.Add(new Vector3(10, 0, 0));
            triangle.Faces.Add(new Face(new int[] { 0, 1, 2 }));
            triangle.MaterialIndex = 0;

            scene.Meshes.Add(triangle);
            scene.RootNode.MeshIndices.Add(0);

            Material mat = new Material();
            mat.Name = "MyMaterial";
            scene.Materials.Add(mat);

            //Export the scene then read it in and compare!

            AssimpContext context = new AssimpContext();
            Assert.IsTrue(context.ExportFile(scene, path, "obj"));

            Scene importedScene = context.ImportFile(path);
            Assert.IsTrue(importedScene.MeshCount == scene.MeshCount);
            Assert.IsTrue(importedScene.MaterialCount == 2); //Always has the default material, should also have our material

            //Compare the meshes
            Mesh importedTriangle = importedScene.Meshes[0];

            Assert.IsTrue(importedTriangle.VertexCount == triangle.VertexCount);
            for(int i = 0; i < importedTriangle.VertexCount; i++)
            {
                Assert.IsTrue(importedTriangle.Vertices[i].Equals(triangle.Vertices[i]));
            }

            Assert.IsTrue(importedTriangle.FaceCount == triangle.FaceCount);
            for(int i = 0; i < importedTriangle.FaceCount; i++)
            {
                Face importedFace = importedTriangle.Faces[i];
                Face face = triangle.Faces[i];

                for(int j = 0; j < importedFace.IndexCount; j++)
                {
                    Assert.IsTrue(importedFace.Indices[j] == face.Indices[j]);
                }
            }
        }

        [Test]
        public void TestFreeLogStreams()
        {
            ConsoleLogStream console1 = new ConsoleLogStream();
            ConsoleLogStream console2 = new ConsoleLogStream();
            ConsoleLogStream console3 = new ConsoleLogStream();

            console1.Attach();
            console2.Attach();
            console3.Attach();

            AssimpLibrary.Instance.FreeLibrary();

            IEnumerable<LogStream> logs = LogStream.GetAttachedLogStreams();

            Assert.IsEmpty(logs);
            Assert.IsFalse(console1.IsAttached);
            Assert.IsFalse(console2.IsAttached);
            Assert.IsFalse(console3.IsAttached);
        }

        [Test]
        public void TestImportFromFile()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/sphere.obj");

            AssimpContext importer = new AssimpContext();

            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.Scale = .5f;
            importer.XAxisRotation = 25.0f;
            importer.YAxisRotation = 50.0f;
            LogStream.IsVerboseLoggingEnabled = true;

            Assert.IsTrue(importer.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName));

            importer.RemoveConfigs();

            Assert.IsFalse(importer.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName));

            importer.SetConfig(new NormalSmoothingAngleConfig(65.0f));
            importer.SetConfig(new NormalSmoothingAngleConfig(22.5f));
            importer.RemoveConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName);

            Assert.IsFalse(importer.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName));

            importer.SetConfig(new NormalSmoothingAngleConfig(65.0f));

            Scene scene = importer.ImportFile(path, PostProcessPreset.TargetRealTimeMaximumQuality);

            Assert.IsNotNull(scene);
            Assert.IsTrue((scene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);
        }

        [Test]
        public void TestImportFromStream()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            FileStream fs = File.OpenRead(path);

            AssimpContext importer = new AssimpContext();
            LogStream.IsVerboseLoggingEnabled = true;

            LogStream logstream = new LogStream(delegate(String msg, String userData)
            {
                Console.WriteLine(msg);
            });

            logstream.Attach();

            Scene scene = importer.ImportFileFromStream(fs, ".dae");

            fs.Close();

            Assert.IsNotNull(scene);
            Assert.IsTrue((scene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);
        }

        [Test]
        public void TestImportFromStreamNoFormatHint()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            FileStream fs = File.OpenRead(path);

            AssimpContext importer = new AssimpContext();
            LogStream.IsVerboseLoggingEnabled = true;

            LogStream logstream = new LogStream(delegate (String msg, String userData)
            {
                Console.WriteLine(msg);
            });

            logstream.Attach();

            Scene scene = importer.ImportFileFromStream(fs, String.Empty); //null also seems to work well

            fs.Close();

            Assert.IsNotNull(scene);
            Assert.IsTrue((scene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);
        }

        [Test]
        public void TestImporterDescriptions()
        {
            AssimpContext importer = new AssimpContext();
            ImporterDescription[] importerDescrs = importer.GetImporterDescriptions();

            Assert.IsNotNull(importerDescrs);
            Assert.IsTrue(importerDescrs.Length > 0);

            ImporterDescription descr = importer.GetImporterDescriptionFor("obj");
            ImporterDescription descr2 = importer.GetImporterDescriptionFor(".obj");

            Assert.IsNotNull(descr);
            Assert.IsNotNull(descr2);
            Assert.IsTrue(descr.Name == descr2.Name);
        }

        [Test]
        public void TestSupportedFormats()
        {
            AssimpContext importer = new AssimpContext();
            ExportFormatDescription[] exportDescs = importer.GetSupportedExportFormats();

            String[] importFormats = importer.GetSupportedImportFormats();

            Assert.IsNotNull(exportDescs);
            Assert.IsNotNull(importFormats);
            Assert.IsTrue(exportDescs.Length >= 1);
            Assert.IsTrue(importFormats.Length >= 1);

            Assert.IsTrue(importer.IsExportFormatSupported(exportDescs[0].FileExtension));
            Assert.IsTrue(importer.IsImportFormatSupported(importFormats[0]));

            Assert.IsTrue(importer.IsExportFormatSupported("obj"));
            Assert.IsTrue(importer.IsExportFormatSupported(".obj"));
        }

        [Test]
        public void TestConvertFromFile()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/Bob.dae");

            AssimpContext importer = new AssimpContext();
            importer.ConvertFromFileToFile(path, outputPath, "collada");

            ExportDataBlob blob = importer.ConvertFromFileToBlob(path, "collada");
        }

        [Test]
        public void TestConvertFromStreamNoFormatHint()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duckNoHint.obj");

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            FileStream fs = File.OpenRead(path);

            new ConsoleLogStream().Attach();

            AssimpContext importer = new AssimpContext();
            bool success = importer.ConvertFromStreamToFile(fs, ".dae", outputPath, "obj");
            Assert.IsTrue(success);

            Assert.IsTrue(File.Exists(outputPath));
        }

        [Test]
        public void TestConvertFromStream()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck.obj");
            String outputPath2 = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck-fromBlob.obj");

            FileStream fs = File.OpenRead(path);

            new ConsoleLogStream().Attach();

            AssimpContext importer = new AssimpContext();
            bool success = importer.ConvertFromStreamToFile(fs, ".dae", outputPath, "obj");
            Assert.IsTrue(success);

            fs.Position = 0;

            ExportDataBlob blob = importer.ConvertFromStreamToBlob(fs, ".dae", "collada");
            Assert.IsNotNull(blob);

            fs.Close();

            //Take ExportDataBlob's data, write it to a memory stream and export that back to an obj and write it

            MemoryStream memStream = new MemoryStream();
            memStream.Write(blob.Data, 0, blob.Data.Length);

            memStream.Position = 0;

            success = importer.ConvertFromStreamToFile(memStream, ".dae", outputPath2, "obj");

            memStream.Close();

            LogStream.DetachAllLogstreams();

            Assert.IsTrue(success);
        }

        [Test]
        public void TestMultipleImportersMultipleThreads()
        {
            LogStream.IsVerboseLoggingEnabled = true;

            Thread threadA = new Thread(new ThreadStart(LoadSceneB));
            Thread threadB = new Thread(new ThreadStart(LoadSceneB));
            Thread threadC = new Thread(new ThreadStart(ConvertSceneC));

            threadB.Start();
            threadA.Start();
            threadC.Start();

            threadC.Join();
            threadA.Join();
            threadB.Join();

            LogStream.DetachAllLogstreams();
        }

        private void LoadSceneA()
        {
            Console.WriteLine("Thread A: Starting import.");
            AssimpContext importer = new AssimpContext();
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");

            new ConsoleLogStream("Thread A:").Attach();
            Console.WriteLine("Thread A: Importing");
            Scene scene = importer.ImportFile(path);
            Console.WriteLine("Thread A: Done importing");
        }

        private void LoadSceneB()
        {
            Console.WriteLine("Thread B: Starting import.");
            AssimpContext importer = new AssimpContext();
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            new ConsoleLogStream("Thread B:").Attach();
            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            Console.WriteLine("Thread B: Importing");
            Scene scene = importer.ImportFile(path);
            Console.WriteLine("Thread B: Done importing");
        }

        private void ConvertSceneC()
        {
            Console.WriteLine("Thread C: Starting convert.");
            AssimpContext importer = new AssimpContext();
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck2.obj");

            new ConsoleLogStream("Thread C:").Attach();
            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.SetConfig(new FavorSpeedConfig(true));

            Console.WriteLine("Thread C: Converting");
            ExportDataBlob blob = importer.ConvertFromFileToBlob(path, "obj");

            Console.WriteLine("Thread C: Done converting");
        }
    }
}
