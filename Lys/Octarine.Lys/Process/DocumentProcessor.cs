/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Octarine.Lys.Language;
using Octarine.Lys.Parse;

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Class for reading the document structure (such as namespaces, functions).
    /// </summary>
    public class DocumentProcessor
    {
        /// <summary>
        /// Initializes a new document processor.
        /// </summary>
        /// <param name="tokenizer">The tokenizer where to get the tokens from.</param>
        /// <param name="typeTable">The type look-up table.</param>
        /// <param name="instructionInterpreterFactory">
        /// The factory which creates an interpreter for instructions, such as in function bodies.
        /// </param
        public DocumentProcessor(ITokenizer tokenizer, ITypeTable typeTable, IInterpreterFactory instructionInterpreterFactory)
        {
            if (object.ReferenceEquals(null, tokenizer))
                throw new ArgumentNullException("tokenizer");
            if (object.ReferenceEquals(null, typeTable))
                throw new ArgumentNullException("typeTable");
            if (object.ReferenceEquals(null, instructionInterpreterFactory))
                throw new ArgumentNullException("instructionInterpreterFactory");

            _tokenizer = tokenizer;
            _iterator = new TokenIterator(_tokenizer);
            _typeTable = typeTable;
            _instructionInterpreterFactory = instructionInterpreterFactory;
        }

        private ITokenizer _tokenizer;
        private TokenIterator _iterator;
        private ITypeTable _typeTable;
        private IInterpreterFactory _instructionInterpreterFactory;

        /// <summary>
        /// Reads the namespaces from the tokenizer.
        /// </summary>
        public Namespace[] Read()
        {
            List<Namespace> ns = new List<Namespace>();
            
            while (_iterator.Next())
            {
                ns.AddRange(ReadNamespace(new List<string>()));
            }

            return ns.ToArray();
        }

        /// <summary>
        /// Looks up the type with the given identifier.
        /// </summary>
        /// <param name="identfier">The identfier of the type (without namespace prefix).</param>
        /// <param name="inNamespace">The namespace in which the type shall be looked up. If the namespace itself does not contain the type definition, the parent namespaces are browsed.</param>
        /// <param name="namespaceLengthRestriction">The amount of namespace parts which shall be considered in the lookup process. This is used by the method itself recursively, and should otherwise be set to the amout of namespace parts.</param>
        /// <returns>the type if it was found, or null otherwise.</returns>
        private IType LookupType(string identfier, IEnumerable<string> inNamespace, int namespaceLengthRestriction)
        {
            if (identfier == null)
                throw new ArgumentNullException("identfier");
            if (inNamespace == null)
                throw new ArgumentNullException("inNamespace");

            // Build the full path to the type, assuming it is defined in the given namespace
            StringBuilder typePath = new StringBuilder();
            int i = 0;
            foreach (string nsPart in inNamespace)
            {
                if (++i > namespaceLengthRestriction) break;
                typePath.Append(nsPart);
                typePath.Append("::");
            }
            typePath.Append(identfier);
            string typePathString = typePath.ToString();

            // Check if the type is defined in the given namespace
            if (_typeTable.Has(typePathString))
                return _typeTable.Lookup(typePathString);
            // Otherwise search it in the parent namespace
            else if (namespaceLengthRestriction > 0)
                return LookupType(identfier, inNamespace, namespaceLengthRestriction - 1);
            // We already checked the global namespace, so there is no such type
            else
                return null;
        }

        /// <summary>
        /// Reads a namespace path (tokens separated by '::') from the tokenizer.
        /// </summary>
        /// <returns>a list containing the several parts of the path.</returns>
        private List<string> ReadNamespacePath()
        {
            List<string> ns = new List<string>();

            // Read namespace name
            if (!_iterator.Is(TokenType.Name))
                throw new SyntaxException(_iterator.Position, "Expected namespace name");
            ns.Add(_iterator.GetValue<string>());
            _iterator.Next();

            // And potential parent namespaces
            while (_iterator.Is(TokenType.DoubleColon))
            {
                _iterator.Next();
                if (!_iterator.Is(TokenType.Name))
                    throw new SyntaxException(_iterator.Position, "Expected namespace name");
                ns.Add(_iterator.GetValue<string>());
                _iterator.Next();
            }

            return ns;
        }

        /// <summary>
        /// Reads a namespace and all its sub-namespaces from the tokenizer.
        /// </summary>
        /// <param name="parentNamespaces">The parts of the namespace which this definition is contained in.</param>
        /// <returns>a list of all read &quot;namespace{}&quot; tokens.</returns>
        private List<Namespace> ReadNamespace(List<string> parentNamespaces)
        {
            List<string> nsPath = new List<string>(parentNamespaces);
            string nsPathString;
            List<UserFunction> nsFunctions = new List<UserFunction>();
            Dictionary<string, IType> nsTypes = new Dictionary<string, IType>();
            List<Namespace> ns = new List<Namespace>();
            Dictionary<string, int> functionNameCounters = new Dictionary<string, int>();

            // Read a namespace definition if there is any.
            // Note that this is optional.
            if (_iterator.Is(TokenType.Name) && _iterator.GetValue<string>() == "namespace")
            {
                _iterator.Next();

                // Read the namespace name
                nsPath.AddRange(ReadNamespacePath());

                // Now read the namespace body
                if (!_iterator.Is(TokenType.CurlyBracketLeft))
                    throw new SyntaxException(_iterator.Position, "Expected '{'");
                _iterator.Next();
            }
            nsPathString = string.Join("::", nsPath.ToArray());

            // Read the namespace body
            while (!_iterator.Is(TokenType.CurlyBracketRight, TokenType.EndOfDocument))
            {
                if (!_iterator.Is(TokenType.Name))
                    throw new SyntaxException(_iterator.Position, "Unexpected token: " + _iterator.Current.Type);
                switch (_iterator.GetValue<string>())
                {
                    case "namespace":
                        ns.AddRange(ReadNamespace(nsPath));
                        break;
                    case "typedef":
                        Tuple<string, IType> type = ReadTypedef(nsPath);
                        nsTypes.Add(type.Item1, type.Item2);
                        break;
                    default:
                        nsFunctions.Add(ReadFunction(nsPath, functionNameCounters));
                        break;
                }
            }

            // If we had a namespace definition, there has to be a terminating '}'
            if (ns.Count > 0)
            {
                if (!_iterator.Is(TokenType.CurlyBracketRight))
                    throw new SyntaxException(_iterator.Position, "Expected '}'");
                _iterator.Next();
            }

            // Build main namespace and add it to the list of read namespaces
            Namespace main;
            main.Path = nsPath.ToArray();
            main.Functions = nsFunctions.ToArray();
            main.TypeDefinitions = nsTypes;
            ns.Add(main);

            return ns;
        }

        /// <summary>
        /// Reads a type definition from the tokenizer.
        /// </summary>
        /// <param name="parentNamespaces">The parts of the namespace which this definition is contained in.</param>
        /// <returns>a tuple consisting of the type full path (parts separated by '::') and the type itself.</returns>
        private Tuple<string, IType> ReadTypedef(List<string> parentNamespace)
        {
            _iterator.Next();

            IType type;

            // Read type identifier
            if (!_iterator.Is(TokenType.Name))
                throw new SyntaxException(_iterator.Position, "Expected type identifier");
            string typeIdentifier = _iterator.GetValue<string>();
            _iterator.Next();

            // The should be an '='
            if (!_iterator.Is(TokenType.OperatorAssign))
                throw new SyntaxException(_iterator.Position, "Expected '='");
            _iterator.Next();

            // Two possible syntaxes
            // 1) typedef [name] = [alias];
            // 2) typedef [name] = { [name]: [type], [name]: [type], ... };
            if (_iterator.Is(TokenType.Name))
            {
                string aliasPath = string.Join("::", ReadNamespacePath().ToArray());
                long aliasPosition = _iterator.Position;
                IType alias = LookupType(aliasPath, parentNamespace, parentNamespace.Count);
                if (object.ReferenceEquals(null, alias))
                    throw new SyntaxException(aliasPosition, "Unrecognized type: " + aliasPath);
                type = alias;
            }
            else if (_iterator.Is(TokenType.CurlyBracketLeft))
            {
                Dictionary<string, IType> fields = new Dictionary<string, IType>();
                do
                {
                    _iterator.Next();

                    // We expect   "fieldName: fieldType"
                    // Read field name
                    if (!_iterator.Is(TokenType.Name))
                        throw new SyntaxException(_iterator.Position, "Expected field name");
                    string fieldName = _iterator.GetValue<string>();
                    if (fields.ContainsKey(fieldName))
                        throw new SyntaxException(_iterator.Position, "Duplicate field: " + fieldName);
                    _iterator.Next();

                    // The should be a ':'
                    if (!_iterator.Is(TokenType.Colon))
                        throw new SyntaxException(_iterator.Position, "Expected ':'");
                    _iterator.Next();

                    // Read field type
                    long fieldTypePosition = _iterator.Position;
                    string fieldTypePath = string.Join("::", ReadNamespacePath().ToArray());
                    IType fieldType = LookupType(fieldTypePath, parentNamespace, parentNamespace.Count);
                    if (object.ReferenceEquals(null, fieldType))
                        throw new SyntaxException(fieldTypePosition, "Unrecognized type: " + fieldTypePath);

                    fields.Add(fieldName, fieldType);
                }
                while (_iterator.Is(TokenType.Comma));

                // The should be a '}'
                if (!_iterator.Is(TokenType.CurlyBracketRight))
                    throw new SyntaxException(_iterator.Position, "Expected '}'");
                _iterator.Next();

                // Create custom type
                type = new CustomType(typeIdentifier, fields);
            }
            else
            {
                throw new SyntaxException(_iterator.Position, "Expected '{' or a type.");
            }

            // The should be a ';'
            if (!_iterator.Is(TokenType.EndOfInstruction))
                throw new SyntaxException(_iterator.Position, "Expected ';'");
            _iterator.Next();

            // Store internal reference to this type
            StringBuilder typePath = new StringBuilder();
            foreach (string ns in parentNamespace)
            {
                typePath.Append(ns);
                typePath.Append("::");
            }
            typePath.Append(typeIdentifier);
            _typeTable.Define(typePath.ToString(), type);

            // Return tuple
            return new Tuple<string, IType>(typeIdentifier, type);
        }

        /// <summary>
        /// Reads a function from the tokenizer.
        /// </summary>
        /// <param name="parentNamespaces">The parts of the namespace which this definition is contained in.</param>
        /// <returns>the function which has been read.</returns>
        private UserFunction ReadFunction(List<string> parentNamespace, Dictionary<string, int> functionNameCounters)
        {
            // Read return type
            if (!_iterator.Is(TokenType.Name))
                throw new SyntaxException(_iterator.Position, "Expected function return type");
            string returnTypeIdentifier = _iterator.GetValue<string>();

            // Look up return type
            long returnTypePosition = _iterator.Position;
            IType returnType;
            if (_iterator.Is(TokenType.Name) && _iterator.GetValue<string>() == "void")
            {
                returnType = null;
            }
            else
            {
                returnType = LookupType(returnTypeIdentifier, parentNamespace, parentNamespace.Count);
                if (object.ReferenceEquals(null, returnType))
                    throw new SyntaxException(returnTypePosition, "Unrecognized type: " + returnTypeIdentifier);
            }

            _iterator.Next();

            // Read function name
            if (!_iterator.Is(TokenType.Name))
                throw new SyntaxException(_iterator.Position, "Expected function name");
            string funcName = _iterator.GetValue<string>();
            _iterator.Next();

            // There should be a bracket now
            if (!_iterator.Is(TokenType.BracketLeft))
                throw new SyntaxException(_iterator.Position, "Expected '('");
            _iterator.Next();

            // Read arguments (as dictionary 'name=>type')
            Dictionary<string, IType> funcArgs = new Dictionary<string, IType>();
            if (!_iterator.Is(TokenType.BracketRight))
            {
                while (true)
                {
                    // Read argument type
                    if (!_iterator.Is(TokenType.Name))
                        throw new SyntaxException(_iterator.Position, "Expected argument type");
                    string argTypeIdentifier = _iterator.GetValue<string>();

                    // Look up return type
                    long argTypePosition = _iterator.Position;
                    IType argType = LookupType(argTypeIdentifier, parentNamespace, parentNamespace.Count);
                    if (object.ReferenceEquals(null, argType))
                        throw new SyntaxException(argTypePosition, "Unrecognized type: " + argTypeIdentifier);

                    _iterator.Next();

                    // Read argument name
                    if (!_iterator.Is(TokenType.Name))
                        throw new SyntaxException(_iterator.Position, "Expected argument name");
                    string argName = _iterator.GetValue<string>();

                    // Add to dictionary
                    if (funcArgs.ContainsKey(argName))
                        throw new SyntaxException(_iterator.Position, "Duplicate argument name '" + argName + "'.");
                    funcArgs.Add(argName, argType);

                    _iterator.Next();

                    // Check if this is the last argument
                    // and if not, read the separating comma.
                    if (_iterator.Is(TokenType.BracketRight))
                        break;
                    if (!_iterator.Is(TokenType.Comma))
                        throw new SyntaxException(_iterator.Position, "Expected ',' or ')'");
                    _iterator.Next();
                }

            }

            // The iterator must be pointing at a bracket ')' at this point
            _iterator.Next();

            // There should be a curly bracket now
            if (!_iterator.Is(TokenType.CurlyBracketLeft))
                throw new SyntaxException(_iterator.Position, "Expected '{'");
            _iterator.Next();

            // Read function body
            FunctionContext functionHeader;
            functionHeader.Signature.Arguments = funcArgs.Select(x => new Variable { Name = x.Key, Type = x.Value }).ToArray();
            functionHeader.Signature.Name = funcName;
            functionHeader.Signature.Namespace = parentNamespace.ToArray();
            functionHeader.Signature.ReturnType = returnType;
            if (!functionNameCounters.ContainsKey(funcName))
            {
                functionHeader.Signature.Index = 0;
                functionNameCounters[funcName] = 1;
            }
            else
            {
                functionHeader.Signature.Index = functionNameCounters[funcName]++;
            }
            functionHeader.Signature.IsBuiltin = false;
            functionHeader.TypeTable = _typeTable;
            var ii = _instructionInterpreterFactory.Create(functionHeader, _iterator);
            var funcBody = new OperationCollection();
            while (!_iterator.Is(TokenType.EndOfDocument, TokenType.CurlyBracketRight))
            {
                funcBody.Append(ii.InterpreteNext());
            }

            // There should be a curly bracket now
            if (!_iterator.Is(TokenType.CurlyBracketRight))
                throw new SyntaxException(_iterator.Position, "Expected '}'");
            _iterator.Next();

            UserFunction f;
            f.Signature = functionHeader.Signature;
            f.Body = funcBody;
            f.SourcePosition = returnTypePosition;
            return f;
        }

    }
}