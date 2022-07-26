/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Octarine.Lys.Language;
using Octarine.Lys.Process;

namespace Octarine.Lys.Compile
{
    /// <summary>
    /// Represents an abstract compiler implementing the basic code.
    /// </summary>
    public abstract class CompilerBase
    {
        /// <summary>
        /// Initializes a new compiler.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="namespaces">The namespaces containing the operations to be compiled.</param>
        /// <param name="typeTable">The type look-up table.</param>
        public CompilerBase(string appName, Namespace[] namespaces, ITypeTable builtinTypes)
        {
            if (object.ReferenceEquals(null, appName))
                throw new ArgumentNullException("appName");
            if (object.ReferenceEquals(null, namespaces))
                throw new ArgumentNullException("namespaces");
            if (object.ReferenceEquals(null, builtinTypes))
                throw new ArgumentNullException("builtinTypes");
            _appName = appName;
            _rootNamespace = StructuredNamespace.CreateRoot();
            _rootNamespace.Children = StructuredNamespace.StructureNamespaces(namespaces);
            _builtinTypes = builtinTypes;
        }

        private string _appName;
        private StructuredNamespace _rootNamespace;
        private ITypeTable _builtinTypes;

        protected class StructuredNamespace
        {
            private StructuredNamespace()
            {
                this.Name = null;
            }
            public StructuredNamespace(string name)
            {
                this.Name = name;
            }
            public StructuredNamespace(Namespace ns)
            {
                this.Name = ns.Path[ns.Path.Length - 1];
                this.UserFunctions = new List<UserFunction>(ns.Functions);
            }

            public static StructuredNamespace CreateRoot()
            {
                return new StructuredNamespace();
            }

            public string? Name;
            public List<FunctionSignature> BuiltinFunctions = new List<FunctionSignature>();
            public List<UserFunction> UserFunctions = new List<UserFunction>();
            public Dictionary<string, IType> TypeDefinitions = new Dictionary<string, IType>();
            public List<StructuredNamespace> Children = new List<StructuredNamespace>();

            /// <summary>
            /// Checks whether there are any user functions defined in this namespace, or any of its children.
            /// </summary>
            /// <returns>true if there are no user functions.</returns>
            public bool IsEmpty()
            {
                return this.UserFunctions.Count == 0 && this.Children.TrueForAll(x => x.IsEmpty());
            }

            /// <summary>
            /// Creates a hierarchical structure from the given namespaces.
            /// </summary>
            /// <param name="namespaces">The namespaces.</param>
            public static List<StructuredNamespace> StructureNamespaces(Namespace[] namespaces)
            {
                List<StructuredNamespace> list = new List<StructuredNamespace>();
                foreach (var ns in namespaces)
                {
                    var currentNsLevel = list;
                    for (int i = 0; i < ns.Path.Length; i++)
                    {
                        var pathAnalogon = currentNsLevel.Find(x => x.Name == ns.Path[i]);
                        if (pathAnalogon == null)
                        {
                            if (i == ns.Path.Length - 1)
                                pathAnalogon = new StructuredNamespace(ns);
                            else
                                pathAnalogon = new StructuredNamespace(ns.Path[i]);
                            currentNsLevel.Add(pathAnalogon);
                        }
                        else if (i == ns.Path.Length - 1)
                        {
                            pathAnalogon.UserFunctions.AddRange(ns.Functions);
                            foreach (var kv in ns.TypeDefinitions)
                                pathAnalogon.TypeDefinitions[kv.Key] = kv.Value;
                        }
                        currentNsLevel = pathAnalogon.Children;
                    }
                }
                return list;
            }
        }

        /// <summary>
        /// Element on the instruction stack.
        /// </summary>
        protected class StackElement
        {
            public StackElement(string? code, IType? type)
            {
                this.RawCode = code;
                this.Type = type;
                this.IsVariable = false;
                this.IsProperty = false;
            }
            public StackElement(string code, IType? type, bool isVariable, bool isProperty)
            {
                this.RawCode = code;
                this.Type = type;
                this.IsVariable = isVariable;
                this.IsProperty = isProperty;
            }

            /// <summary>
            /// The associated Javascript source code, as passed to the constructor.
            /// </summary>
            public string? RawCode { get; private set; }
            
            /// <summary>
            /// The associated Javascript source code,
            /// modified as so to be used in a context where the value of the code is accessed.
            /// </summary>
            public string CodeGet
            {
                get
                {
                    if (this.RawCode is null)
                        return "";

                    string code = this.IsProperty ? this.RawCode + "()" : this.RawCode;
                    if (this.Type is IntType)
                    {
                        if (((IntType)this.Type).Unsigned)
                            code = string.Format("({0}&{1})", code, ((IntType)this.Type).Mask);
                        else
                            code = string.Format("({0}&{1}-{2})", code, ((IntType)this.Type).Mask, ((IntType)this.Type).Mask + 1);
                    }
                    return code;
                }
            }

            /// <summary>
            /// The type of this stack element.
            /// </summary>
            public IType? Type { get; private set; }

            /// <summary>
            /// Indicates whether this element is a variable.
            /// This is used to determine whether it can be assigned a value.
            /// </summary>
            public bool IsVariable { get; private set; }

            /// <summary>
            /// Indicates whether this element is a property.
            /// </summary>
            public bool IsProperty { get; private set; }
        }

        /// <summary>
        /// Result of a the compilation of an instruction scope.
        /// </summary>
        protected struct InstructionScopeResult
        {
            /// <summary>
            /// The type of the return value of this instruction scope.
            /// May be null if no value is returned.
            /// </summary>
            public IType? ReturnType;

            /// <summary>
            /// The set of all variables used in this scope.
            /// </summary>
            public ISet<Variable> CreatedVariables;

            /// <summary>
            /// The set of all variables used in this scope.
            /// </summary>
            public ISet<Variable> UsedVariables;
        }

        /// <summary>
        /// Gets the name of the application passed to the constructor.
        /// </summary>
        public string AppName
        {
            get { return _appName; }
        }

        /// <summary>
        /// Gets the root namespace.
        /// </summary>
        protected StructuredNamespace RootNamespace
        {
            get { return _rootNamespace; }
        }

        /// <summary>
        /// Gets the type table which contains the built-in types.
        /// </summary>
        protected ITypeTable BuiltinTypes
        {
            get { return _builtinTypes; }
        }

        /// <summary>
        /// Adds a built-in function to this compiler instance.
        /// </summary>
        /// <param name="func">
        /// The function description.
        /// The `IsBuiltin` and `Index` fields are ignored and overwritten.
        /// </param>
        public void AddBuiltinFunction(FunctionSignature func)
        {
            if (object.ReferenceEquals(null, func.Arguments) ||
                string.IsNullOrEmpty(func.Name) ||
                object.ReferenceEquals(null, func.Namespace))
                throw new ArgumentNullException("func");

            // Find (or create) associated namespace
            StructuredNamespace ptr = _rootNamespace;
            foreach (string nsPart in func.Namespace)
            {
                var ns = ptr.Children.Find(x => x.Name == nsPart);
                if (ns == null)
                    ptr.Children.Add(ns = new StructuredNamespace(nsPart));
                ptr = ns;
            }

            // Make sure function has valid properties
            func.IsBuiltin = true;
            func.Index = ptr.BuiltinFunctions.Where(x => x.Name == func.Name).Count();

            // Add the function
            ptr.BuiltinFunctions.Add(func);
        }

        /// <summary>
        /// Resolves the given type.
        /// </summary>
        /// <param name="sourceCodePosition">The current position of the token in the source code. Used for throwing exceptions.</param>
        /// <param name="typePath">The complete path to the type.</param>
        protected IType ResolveType(long sourceCodePosition, params string[] typePath)
        {
            string serialized = string.Join("::", typePath);
            if (_builtinTypes.Has(serialized))
                return _builtinTypes.Lookup(serialized);
            else
                throw new CompileException(sourceCodePosition, "Cannot resolve type: " + serialized);
        }

        /// <summary>
        /// Resolves the given function in the specified context.
        /// </summary>
        /// <param name="funcPath">The (absolute or relative) path to the function.</param>
        /// <param name="namespaceContext">The namespace in which the function is called.</param>
        /// <param name="scope">The scope in which the function is called.</param>
        /// <param name="arguments">The arguments with which the function is called.</param>
        /// <returns>the function if it was found, an empty element with .Index == -1 otherwise.</returns>
        protected FunctionSignature ResolveFunction(string[] funcPath, string[] namespaceContext, Scope scope, IType?[] arguments)
        {
            // List all potential functions
            List<FunctionSignature> potentialFunctions = new List<FunctionSignature>();
            string funcName = funcPath[funcPath.Length - 1];

            // `funcPath` can be relative to any part of `namespaceContext`
            for (int context = namespaceContext.Length; context >= 0; context--)
            {
                StructuredNamespace? ptr = _rootNamespace; // pointer to the current namespace in the search process
                for (int i = 0; ptr != null && i < context + funcPath.Length - 1; i++)
                {
                    string token = i < context ? namespaceContext[i] : funcPath[i - context];
                    ptr = ptr?.Children.Find(x => x.Name == token);
                }
                if (ptr != null)
                {
                    potentialFunctions.AddRange(ptr.BuiltinFunctions.
                        Concat(ptr.UserFunctions.Select(f => f.Signature)).
                        Where(f => f.Name == funcName));
                }
            }

            // `funcPath` can be relative to any imported namespace
            foreach (var ns in scope.GetImportedNamespaces())
            {
                StructuredNamespace? ptr = _rootNamespace; // pointer to the current namespace in the search process
                for (int i = 0; ptr != null && i < ns.Length + funcPath.Length - 1; i++)
                {
                    string token = i < ns.Length ? ns[i] : funcPath[i - ns.Length];
                    ptr = ptr?.Children.Find(x => x.Name == token);
                }
                if (ptr != null)
                {
                    potentialFunctions.AddRange(ptr.BuiltinFunctions.
                        Concat(ptr.UserFunctions.Select(f => f.Signature)).
                        Where(f => f.Name == funcName));
                }
            }

            // Search for the first function which matches the signature
            foreach (var f in potentialFunctions)
            {
                if (f.Arguments.Length != arguments.Length) continue;
                bool signatureMatch = true;
                for (int i = 0; i < f.Arguments.Length; i++)
                {
                    if (!(arguments[i]?.CanCastTo(f.Arguments[i].Type)??false))
                    {
                        signatureMatch = false;
                        break;
                    }
                }
                if (signatureMatch)
                    return f;
            }

            return new FunctionSignature { Index = -1 };
        }

        /// <summary>
        /// Verifies whether the return type matches the function return type.
        /// </summary>
        /// <param name="actualReturnType">The type of the value which is returned.</param>
        /// <param name="definedReturnType">The type of the function specified in the function signature.</param>
        /// <param name="positionInSourceCode">The current position of the token in the source code. Used for throwing exceptions.</param>
        protected void VerifyReturnType(IType? actualReturnType, IType? definedReturnType, long positionInSourceCode)
        {
            if (actualReturnType is null)
            {
                if (definedReturnType is not null)
                    throw new CompileException(positionInSourceCode, "Function does not return a value");
            }
            else if (definedReturnType is null)
                throw new CompileException(positionInSourceCode, "Function must not return a value");
            else if (!actualReturnType.CanCastTo(definedReturnType))
                throw new CompileException(positionInSourceCode, "Function return value does not match definition");
        }

        /// <summary>
        /// Compiles the namespaces to the output.
        /// </summary>
        /// <param name="output">The writer where to write the output to.</param>
        public abstract void Compile(TextWriter output);

    }
}