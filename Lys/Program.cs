/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using Octarine.Lys;
using Octarine.Lys.Compile;
using Octarine.Lys.Language;
using Octarine.Lys.Parse;
using Octarine.Lys.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lys
{
    class Program
    {
        static int Main(string[] args)
        {
#if DEBUG
            FileInput("matrix1.lys", Encoding.Default, true);
            Console.WriteLine();
            Console.Write("Press any key to exit ... ");
            Console.ReadKey(true);
            return 0;
#else
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            Console.InputEncoding = encoding;
            Console.OutputEncoding = encoding;
            return StreamInput(Console.In, encoding, false);
#endif
        }

        static void FileInput(string filename, Encoding encoding, bool errorsWithStackTrace)
        {
            using (Stream s = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(s, encoding))
            {
                StreamInput(sr, encoding, errorsWithStackTrace);
            }
        }

        static int StreamInput(TextReader input, Encoding encoding, bool errorsWithStackTrace)
        {
            var builtinTypes = LoadBuiltinTypes();
            var charReader = new TextReaderCharReader(input);
            var tokenizer = new Tokenizer(charReader);
            var interpreter = new DocumentProcessor(tokenizer, builtinTypes, new InstructionInterpreterFactory());

            try
            {
                var namespaces = interpreter.Read();
                var compiler = new JavascriptCompiler("example", namespaces, builtinTypes);
                foreach (var f in LoadBuiltinFunctions())
                    compiler.AddBuiltinFunction(f);
                using (MemoryStream mem = new MemoryStream())
                using (StreamWriter memW = new StreamWriter(mem, encoding))
                {
                    compiler.Compile(memW);
                    memW.Flush();
                    Console.Out.Write(encoding.GetString(mem.ToArray()));
                }
                return 0;
            }
            catch (SyntaxException ex)
            {
                Console.Error.WriteLine("Syntax error at " + ex.Position);
                Console.Error.WriteLine(ex.Message);
                if (errorsWithStackTrace) Console.Error.WriteLine(ex.StackTrace);
                return 0x101;
            }
            catch (CompileException ex)
            {
                Console.Error.WriteLine("Compile error at " + ex.Position);
                Console.Error.WriteLine(ex.Message);
                if (errorsWithStackTrace) Console.Error.WriteLine(ex.StackTrace);
                return 0x102;
            }
        }

        static void InteractiveConsoleInput()
        {
            Console.WriteLine("Type 'exit' to exit.");
            Console.WriteLine("Type any code and press ENTER to parse it.");
            Console.WriteLine();

            var charReader = new VarStringCharReader();
            var tokenizer = new Tokenizer(charReader);
            var interpreter = new DocumentProcessor(tokenizer, LoadBuiltinTypes(), new InstructionInterpreterFactory());

            while (true)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;

                string line = Console.ReadLine();
                if (line.ToLower() == "exit") return;
                charReader.SetString(line);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;

                try
                {
                    var namespaces = interpreter.Read();
                    OutputNamespaces(namespaces);
                    Console.WriteLine();
                }
                catch (SyntaxException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("^".PadLeft((int)ex.Position + 1, '-'));
                    Console.WriteLine(ex);
                }
            }
        }
        
        static void OutputNamespaces(Namespace[] namespaces)
        {
            foreach (var ns in namespaces)
            {
                Console.WriteLine("[NS] " + string.Join("::", ns.Path));
                foreach (var fx in ns.Functions)
                {
                    Console.Write("[FX] " + fx.Signature.Name + "(");
                    for (int j = 0; j < fx.Signature.Arguments.Length; j++)
                    {
                        if (j > 0) Console.Write(", ");
                        Console.Write(fx.Signature.Arguments[j].Type.Identifier + " " + fx.Signature.Arguments[j].Name);
                    }
                    Console.WriteLine(")");
                    fx.Body.ForEach(x => Console.WriteLine(x));
                }
                Console.WriteLine();
            }
        }

        static ITypeTable LoadBuiltinTypes()
        {
            var types = new TypeTable();
            for (int i = 1; i <= 32; i++)
            {
                types.Define("int" + i, new IntType(i, false));
                types.Define("uint" + i, new IntType(i, true));
            }
            for (int i = 16; i <= 32; i += 8)
            {
                types.Define("float" + i, new FloatType(i));
            }
            for (int i = 2; i <= 16; i++)
            {
                types.Define("vec" + i, new VecType(i));
            }

            types.Define("bool", new BoolType());
            types.Define("string", new StringType());
            types.Define("led", new LedType());

            types.Define("bit", types.Lookup("uint1"));
            types.Define("byte", types.Lookup("uint8"));
            types.Define("int", types.Lookup("int32"));
            types.Define("float", types.Lookup("float32"));
            return types;
        }

        static IEnumerable<FunctionSignature> LoadBuiltinFunctions()
        {
            foreach (string mathFunc in new string[] { "sin", "cos", "tan", "asin", "acos", "atan", "exp", "log", "sqrt", "ceil", "floor", "abs" })
            {
                yield return new FunctionSignature
                {
                    Arguments = new Variable[] { new Variable("arg0", new FloatType(32)) },
                    Name = mathFunc,
                    Namespace = new string[] { "sys", "math" },
                    ReturnType = new FloatType(32)
                };
            }
            foreach (string mathFunc in new string[] { "min", "max", "pow", "atan2" })
            {
                yield return new FunctionSignature
                {
                    Arguments = new Variable[] { new Variable("arg0", new FloatType(32)), new Variable("arg1", new FloatType(32)) },
                    Name = mathFunc,
                    Namespace = new string[] { "sys", "math" },
                    ReturnType = new FloatType(32)
                };
            }
            foreach (string mathFunc in new string[] { "random" })
            {
                yield return new FunctionSignature
                {
                    Arguments = new Variable[0],
                    Name = mathFunc,
                    Namespace = new string[] { "sys", "math" },
                    ReturnType = new FloatType(32)
                };
            }
            yield return new FunctionSignature
            {
                Arguments = new Variable[] { new Variable("val", new FloatType(32)) },
                Name = "int",
                Namespace = new string[0],
                ReturnType = new IntType(32, false)
            };
            yield return new FunctionSignature
            {
                Arguments = new Variable[] { new Variable("val", new StringType()) },
                Name = "int",
                Namespace = new string[0],
                ReturnType = new IntType(32, false)
            };
            yield return new FunctionSignature
            {
                Arguments = new Variable[] { new Variable("val", new FloatType(32)) },
                Name = "string",
                Namespace = new string[0],
                ReturnType = new StringType()
            };
            yield return new FunctionSignature
            {
                Arguments = new Variable[] { new Variable("val", new BoolType()) },
                Name = "string",
                Namespace = new string[0],
                ReturnType = new StringType()
            };
            yield return new FunctionSignature
            {
                Arguments = new Variable[] { new Variable("val", new GenericVecType()) },
                Name = "string",
                Namespace = new string[0],
                ReturnType = new StringType()
            };
            yield return new FunctionSignature
            {
                Arguments = new Variable[]
                {
                    new Variable("obj", new LedType()),
                    new Variable("r", new FloatType(32)),
                    new Variable("g", new FloatType(32)),
                    new Variable("b", new FloatType(32)),
                },
                Name = "rgb",
                Namespace = new string[0],
                ReturnType = null
            };
            yield return new FunctionSignature
            {
                Arguments = new Variable[] { new Variable("msg", new StringType()) },
                Name = "info",
                Namespace = new string[] { "sys", "log" },
                ReturnType = null
            };
            yield return new FunctionSignature
            {
                Arguments = new Variable[] { new Variable("msg", new StringType()) },
                Name = "error",
                Namespace = new string[] { "sys", "log" },
                ReturnType = null
            };

            // Although this function is handled specially, its definition must be made!
            yield return new FunctionSignature
            {
                Arguments = new Variable[] { new Variable("id", new IntType(32, false)) },
                Name = "$",
                Namespace = new string[0],
                ReturnType = new LedType()
            };
        }

    }
}