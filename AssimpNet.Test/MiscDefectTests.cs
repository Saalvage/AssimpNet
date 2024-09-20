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
using Assimp.Configs;
using Assimp.Unmanaged;
using NUnit.Framework;

namespace Assimp.Test
{
    [TestFixture]
    public class MiscDefectTests
    {

        [TestCase]
        public void TestBaseMaterialProperty()
        {
            Material mat = new Material();
            MaterialProperty prop = new MaterialProperty(null, false);

            bool success = mat.AddProperty(prop);
            Assert.That(success, Is.False);
            Assert.That(String.IsNullOrEmpty(prop.FullyQualifiedName), Is.True);
        }

        [TestCase]
        public void TestEmbeddedTexture()
        {
            Scene scene = new Scene();
            EmbeddedTexture tex1 = new EmbeddedTexture();
            tex1.Filename = "Terrain.bmp";
            scene.Textures.Add(tex1);

            EmbeddedTexture tex2 = new EmbeddedTexture();
            tex2.Filename = "Grass.png";
            scene.Textures.Add(tex2);

            EmbeddedTexture texQuery = scene.GetEmbeddedTexture("*1");
            Assert.That(texQuery == tex2, Is.True);

            texQuery = scene.GetEmbeddedTexture("C:/TextureFolder/Terrains/Terrain.bmp");
            Assert.That(texQuery == tex1, Is.True);
        }
    }
}
