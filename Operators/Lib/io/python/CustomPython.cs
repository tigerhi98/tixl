using System;
using System.IO;
using System.Collections.Generic;
using System.Numerics;
using Python.Runtime;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.DataTypes;
using T3.Core.Logging;

namespace Lib.io.python
{
    [Guid("3b5c1c07-1a9b-473f-b409-3aaf32bd79c2")]
    internal sealed class CustomPython : Instance<CustomPython>
    {
        // (The rest of the class is correct and unchanged)

        // ~~~~ Inputs ~~~~ //
        [Input(Guid = "2d92c717-3535-4293-8eff-6931c39062b1")]
        public readonly InputSlot<string> PythonDllPath = new();

        [Input(Guid = "d3ae0de9-f0d5-4890-aa57-aaf3a6897e93")]
        public readonly InputSlot<string> PythonCode = new();

        [Input(Guid = "dd86a59c-f2c9-4db2-baf7-37a22c37d2c8")]
        public readonly InputSlot<bool> RunCode = new();

        [Input(Guid = "395d886a-73c3-4f9e-a89a-0e98031a7c36")]
        public readonly InputSlot<string> PythonFilePath = new();

        [Input(Guid = "5f8dd033-3ca4-4ef0-8228-b04674137408")]
        public readonly InputSlot<Vector2> Center = new();
        [Input(Guid = "74f25c3e-b1ea-438b-b40e-0a2f29686ae7")]
        public readonly InputSlot<float> A = new();
        [Input(Guid = "bcff0fd4-6631-403c-90a1-c817617e33c2")]
        public readonly InputSlot<float> B = new();
        [Input(Guid = "8fe07d8e-99bc-4afe-93af-693d9331dda3")]
        public readonly InputSlot<float> C = new();
        [Input(Guid = "3fd44f13-814a-4365-8f3d-41604b7e111a")]
        public readonly InputSlot<float> D = new();
        [Input(Guid = "b3c63468-0753-4493-9a75-64d215a835b9")]
        public readonly InputSlot<Int2> Resolution = new();
        [Input(Guid = "6d59e50c-eaa2-46ed-babe-19b3a96f0848")]
        public readonly InputSlot<Texture2D> FxTexture = new();
        [Input(Guid = "ecfe0f79-0de5-4584-827c-11c86faae67b")]
        public readonly InputSlot<List<float>> ListInput = new();
        [Input(Guid = "70927633-676e-45a5-a665-e97e8988f50d")]
        public readonly InputSlot<bool> IgnoreTemplate = new();

        // ~~~~ Outputs ~~~~ //
        [Output(Guid = "eee51f68-e081-46de-96bb-34c15afa21cc")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Output(Guid = "dbd8127c-5220-4835-9185-4389737177d2")]
        public readonly Slot<string> ConsoleOutput = new();

        [Output(Guid = "e29ada3e-62c9-41cf-adda-0b94c944e32c")]
        public readonly Slot<List<float>> ListOutput = new();

        public CustomPython()
        {
            TextureOutput.UpdateAction = Update;
            ConsoleOutput.UpdateAction = Update;
            ListOutput.UpdateAction = Update;
        }

        private static string _lastInitializedDllPath = string.Empty;

        private void Update(EvaluationContext context)
        {
            string dllPath = PythonDllPath.GetValue(context);
            if (!PythonEngine.IsInitialized)
            {
                if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
                {
                    ConsoleOutput.Value = "Python Engine is not initialized.\nPlease provide a valid path to your python3x.dll file.";
                    ListOutput.Value = new List<float>();
                    return;
                }

                try
                {
                    Runtime.PythonDLL = dllPath;
                    PythonEngine.Initialize();
                    PythonEngine.BeginAllowThreads();
                    _lastInitializedDllPath = dllPath;
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to initialize PythonEngine with DLL '{dllPath}'. Error: {ex.Message}");
                    ConsoleOutput.Value = $"ERROR: Failed to initialize Python with the specified DLL.\nSee logs for details.\nMessage: {ex.Message}";
                    _lastInitializedDllPath = "initialization_failed";
                    return;
                }
            }

            if (!RunCode.GetValue(context))
            {
                ConsoleOutput.Value = "Execution is paused. Toggle 'RunCode' to enable.";
                return;
            }

            using (Py.GIL())
            {
                var consoleOutputWriter = new StringWriter();
                try
                {
                    using (var sys = Py.Import("sys"))
                    {
                        var writerDelegate = new StringWriterDelegate(consoleOutputWriter);
                        sys.SetAttr("stdout", writerDelegate.ToPython());
                        sys.SetAttr("stderr", writerDelegate.ToPython());

                        string pythonFilePath = PythonFilePath.GetValue(context);
                        string pythonCodeString = PythonCode.GetValue(context);

                        if (!string.IsNullOrEmpty(pythonFilePath) && File.Exists(pythonFilePath))
                        {
                            string dir = Path.GetDirectoryName(pythonFilePath);
                            string moduleName = Path.GetFileNameWithoutExtension(pythonFilePath);
                            using (var sysPath = sys.GetAttr("path"))
                            {
                                if (!string.IsNullOrEmpty(dir)) sysPath.InvokeMethod("insert", 0.ToPython(), dir.ToPython());
                            }
                            using (var module = Py.Import(moduleName))
                            {
                                var kwargs = new PyDict();
                                kwargs["Center"] = Center.GetValue(context).ToPython();
                                kwargs["A"] = A.GetValue(context).ToPython();
                                kwargs["B"] = B.GetValue(context).ToPython();
                                kwargs["C"] = C.GetValue(context).ToPython();
                                kwargs["D"] = D.GetValue(context).ToPython();
                                kwargs["Resolution"] = Resolution.GetValue(context).ToPython();
                                kwargs["ListInput"] = ListInput.GetValue(context).ToPython();

                                PyObject result = null;
                                if (module.HasAttr("run_script")) result = module.InvokeMethod("run_script", new PyObject[] { }, kwargs);
                                else if (module.HasAttr("main")) result = module.InvokeMethod("main", new PyObject[] { }, kwargs);

                                if (result != null)
                                {
                                    ListOutput.Value = ConvertPyObjectToList(result);
                                }
                                else if (module.HasAttr("ListOutput"))
                                {
                                    var pyList = module.GetAttr("ListOutput");
                                    ListOutput.Value = ConvertPyObjectToList(pyList);
                                }
                                else
                                {
                                    ListOutput.Value = new List<float>();
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(pythonCodeString))
                        {
                            using (var scope = Py.CreateScope())
                            {
                                scope.Set("Center", Center.GetValue(context).ToPython());
                                scope.Set("A", A.GetValue(context).ToPython());
                                scope.Set("B", B.GetValue(context).ToPython());
                                scope.Set("C", C.GetValue(context).ToPython());
                                scope.Set("D", D.GetValue(context).ToPython());
                                scope.Set("Resolution", Resolution.GetValue(context).ToPython());
                                scope.Set("ListInput", ListInput.GetValue(context).ToPython());

                                scope.Exec(pythonCodeString);

                                if (scope.Contains("ListOutput"))
                                {
                                    var pyList = scope.Get("ListOutput");
                                    ListOutput.Value = ConvertPyObjectToList(pyList);
                                }
                                else
                                {
                                    ListOutput.Value = new List<float>();
                                }
                            }
                        }
                        else
                        {
                            consoleOutputWriter.WriteLine("Ready. Provide Python code or a file path to execute.");
                        }
                    }
                    TextureOutput.Value = FxTexture.GetValue(context);
                }
                catch (Exception ex)
                {
                    consoleOutputWriter.WriteLine(ex is PythonException ? "\n--- PYTHON ERROR ---" : "\n--- .NET ERROR ---");
                    consoleOutputWriter.WriteLine(ex.Message);
                    consoleOutputWriter.WriteLine(ex.StackTrace);
                }
                finally
                {
                    ConsoleOutput.Value = consoleOutputWriter.ToString();
                }
            }
        }

        // *** FINAL FIX IS HERE ***
        // This is the definitive way to handle Python's iteration protocol from C#.
        private List<float> ConvertPyObjectToList(PyObject pyObject)
        {
            var newList = new List<float>();
            if (pyObject == null || !pyObject.IsIterable())
                return newList;

            // Get a reference to Python's built-in StopIteration exception type
            using (var builtins = Py.Import("builtins"))
            using (var stopIterationType = builtins.GetAttr("StopIteration"))
            using (var iterator = pyObject.GetIterator())
            {
                while (true)
                {
                    try
                    {
                        using (var item = iterator.InvokeMethod("__next__"))
                        {
                            newList.Add(item.As<float>());
                        }
                    }
                    catch (PythonException e)
                    {
                        // Check if the caught exception is an instance of the StopIteration type
                        if (e.Type.Equals(stopIterationType))
                        {
                            // This is the correct and expected way to end the loop.
                            break;
                        }
                        // Any other Python error during iteration should be reported.
                        throw;
                    }
                }
            }
            return newList;
        }

        private class StringWriterDelegate : TextWriter
        {
            private readonly StringWriter _writer;
            public StringWriterDelegate(StringWriter writer) { _writer = writer; }
            public override void Write(string value) { _writer.Write(value); }
            public void write(string value) { Write(value); }
            public void flush() { }
            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        }
    }
}