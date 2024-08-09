using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp;

namespace FaustDSP
{
    public class DspCompiler
    {
        int version = 1;

        public IFaustDSP CompileDSP(string dspPath)
        {
            return CompileDSP(dspPath, AssemblyLoadContext.Default);
        }
        
        public IFaustDSP CompileDSP(string dspPath, AssemblyLoadContext loadContext)
        {
            StringBuilder compilerOutput = new StringBuilder();
            StringBuilder compilerError = new StringBuilder();

            using (Process process = new Process())
            {
                process.StartInfo.FileName = @"C:\Program Files\faust\bin\faust.exe";
                process.StartInfo.Arguments = @"-lang csharp -a CSharpFaustClass.cs -double " + dspPath;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;

                process.OutputDataReceived += (sender, args) => compilerOutput.AppendLine(args.Data);
                process.ErrorDataReceived += (sender, args) => compilerError.AppendLine(args.Data);

                if (process.Start())
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new FaustCompileException("Faust compile failed: " + compilerError.ToString());
                    }

                    List<MetadataReference> refs = new List<MetadataReference>();

                    Assembly executingAssembly = this.GetType().Assembly;

                    refs.Add(MetadataReference.CreateFromFile(executingAssembly.Location));

                    refs.Add(MetadataReference.CreateFromFile(typeof(Object).Assembly.Location));

                    foreach (AssemblyName assemblyName in executingAssembly.GetReferencedAssemblies())
                    {
                        refs.Add(MetadataReference.CreateFromFile(Assembly.Load(assemblyName).Location));
                    }

                    CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release,
                        assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);

                    CSharpCompilation csharpCompilation = CSharpCompilation.Create("DynamicPlugin" + version, new[] { CSharpSyntaxTree.ParseText(compilerOutput.ToString()) }, refs, options);

                    version++;

                    using (var memoryStream = new MemoryStream())
                    {
                        EmitResult result = csharpCompilation.Emit(memoryStream);

                        if (result.Success)
                        {
                            memoryStream.Seek(0, SeekOrigin.Begin);

                            Assembly assembly = loadContext.LoadFromStream(memoryStream);

                            Type dspType = assembly.GetType("mydsp");

                            if (dspType == null)
                            {
                                string typeStr = "Couldn't find type in assembly with types: ";

                                foreach (Type type in assembly.GetTypes())
                                {
                                    typeStr += type.FullName + ", ";
                                }

                                typeStr += "\nDiagnostics: ";

                                foreach (Diagnostic diagnostic in result.Diagnostics)
                                {
                                    typeStr += diagnostic.ToString() + "\n";
                                }

                                throw new FaustCompileException(typeStr);
                            }

                            IFaustDSP dspClass = Activator.CreateInstance(dspType) as IFaustDSP;

                            return dspClass;
                        }
                        else
                        {
                            string errStr = null;

                            foreach (Diagnostic diag in result.Diagnostics)
                            {
                                errStr += diag.ToString();
                            }

                            throw new FaustCompileException("CSharpCompilation failed: " + errStr);
                        }
                    }
                }
            }

            return null;
        }

        public class FaustCompileException : Exception
        {
            public FaustCompileException()
            {
            }

            public FaustCompileException(string message)
                : base(message)
            {
            }

            public FaustCompileException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}
