using System.Numerics;
using System.Text;
using CppSharp;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Parser;
using CppSharp.Passes;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

ConsoleDriver.Run(new AssimpLibrary());

public class AssimpLibrary : ILibrary {
    public void Setup(Driver driver) {
        var options = driver.Options;
        options.GeneratorKind = GeneratorKind.CSharp;
        //options.UseSpan = true;

        //driver.ParserOptions.NoBuiltinIncludes = true;
        //driver.ParserOptions.SkipFunctionBodies = true;
        //driver.ParserOptions.SkipPrivateDeclarations = true;
        driver.ParserOptions.LanguageVersion = LanguageVersion.C99;

        var rootDir = Path.GetFullPath(Directory.GetCurrentDirectory() + "/../../../../");
        var genDir = rootDir + "AssimpNet.Generator/";
        var outDir = rootDir + "AssimpNet.Unmanaged/Source";
        foreach (var file in Directory.EnumerateFiles(outDir)) {
            File.Delete(file);
        }

        var libDir = rootDir + "Libraries/";

        var includeDir = libDir + "assimp/include/";
        var assimpInclude = includeDir + "assimp/";

        var module = options.AddModule("AssimpNet.Unmanaged");
        module.IncludeDirs.Add(includeDir);
        module.Headers.Add(assimpInclude + "cimport.h");
        module.Headers.Add(assimpInclude + "scene.h");
        module.SharedLibraryName = "assimp";

        options.OutputDir = outDir;

        //options.GenerateFreeStandingFunctionsClassName = _ => "SDL";
        options.GenerationOutputMode = GenerationOutputMode.FilePerUnit;
    }

    public void SetupPasses(Driver driver) {
        driver.Context.TranslationUnitPasses.RemovePrefix("ai");

        driver.SetupTypeMaps();
    }

    public void Preprocess(Driver driver, ASTContext ctx) {
        foreach (var e in ctx.TranslationUnits
                     .SelectMany(x => x.Declarations, (_, d) => d as Enumeration)
                     .Where(x => x != null)) {
            var commonPrefix = LongestCommonPrefix(e.Items.Select(x => x.Name).ToArray());
            if (commonPrefix == "") {
                continue;
            }

            foreach (var val in e.Items) {
                val.Name = val.Name.Replace(commonPrefix, "");
            }
        }

        // TODO
        var mesh = ctx.TranslationUnits.SelectMany(x => x.Classes).FirstOrDefault(x => x.Name == "aiMesh");
        mesh.Fields.RemoveRange(0, mesh.Fields.Count - 1);
        var amesh = ctx.TranslationUnits.SelectMany(x => x.Classes).FirstOrDefault(x => x.Name == "aiAnimMesh");
        amesh.Fields.RemoveRange(0, amesh.Fields.Count - 1);

        string LongestCommonPrefix(string[] strings) {
            if (strings.Length == 0) {
                return "";
            }

            for (var i = 0; i < strings[0].Length; i++) {
                if (strings.Any(x => x.Length <= i || x[i] != strings[0][i])) {
                    return strings[0][..i];
                }
            }

            return strings[0];
        }
    }

    public void Postprocess(Driver driver, ASTContext ctx) {

    }
}

public class MyTypeName<T> : TypeMap {
    public override Type CSharpSignatureType(TypePrinterContext ctx) {
        return new CILType(typeof(T));
    }

    public override void CSharpMarshalToNative(CSharpMarshalContext ctx) {
        ctx.Return.Write(ctx.Parameter.Type.IsPointer() ? $"new IntPtr(&{ctx.Parameter.Name})" : ctx.Parameter.Name);
    }

    public override void CSharpMarshalToManaged(CSharpMarshalContext ctx) {
        ctx.Return.Write($"((__Internal*)__Instance)->{ctx.ArgName.Replace("__", "")}");
    }
}

[TypeMap("aiMatrix4x4")]
public class MatrixMap : MyTypeName<Matrix4x4>;

[TypeMap("aiVector2D")]
public class Vector2Map : MyTypeName<Vector2>;

[TypeMap("aiVector3D"), TypeMap("aiColor3D")]
public class Vector3Map : MyTypeName<Vector3>;

[TypeMap("aiColor4D"), TypeMap("aiVector4D")]
public class Vector4Map : MyTypeName<Vector4>;

[TypeMap("aiQuaternion")]
public class QuaternionMap : MyTypeName<Quaternion>;
