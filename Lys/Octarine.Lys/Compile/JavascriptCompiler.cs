/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
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
    /// Represents a compiler which outputs the given operations into Javascript code.
    /// </summary>
    public class JavascriptCompiler : CompilerBase
    {
        /// <summary>
        /// Initializes a new Javascript compiler.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="namespaces">The namespaces containing the operations to be compiled.</param>
        /// <param name="typeTable">The type look-up table.</param>
        public JavascriptCompiler(string appName, Namespace[] namespaces, ITypeTable builtinTypes)
            : base(appName, namespaces, builtinTypes)
        {
        }

        private int _tempVariableCounter = 0;

        /// <summary>
        /// Compiles the namespaces to the output.
        /// </summary>
        /// <param name="output">The writer where to write the output to.</param>
        public override void Compile(TextWriter output)
        {
            if (object.ReferenceEquals(null, output))
                throw new ArgumentNullException("output");

            output.WriteLine("{");
            foreach (var ns in this.RootNamespace.Children)
            {
                this.CompileSingleNamespace(output, ns, "\t");
            }
            output.WriteLine("}");
        }

        /// <summary>
        /// Compiles a namespace and all of its descendant namespaces and functions.
        /// </summary>
        /// <param name="output">The writer where to write the output to.</param>
        /// <param name="ns">The namespace to be compiled.</param>
        /// <param name="levelPrefix">The prefix string which stands for the code indentation.</param>
        private void CompileSingleNamespace(TextWriter output, StructuredNamespace ns, string levelPrefix)
        {
            // Don't compile empty namespaces
            if (ns.IsEmpty()) return;

            output.WriteLine(levelPrefix + ns.Name + ": {");
            foreach (var sub in ns.Children)
            {
                this.CompileSingleNamespace(output, sub, levelPrefix + '\t');
            }
            foreach (var func in ns.UserFunctions)
            {
                this.CompileFunction(output, func, levelPrefix + '\t');
            }
            output.WriteLine(levelPrefix + "},");
        }

        /// <summary>
        /// Compiles a single user function.
        /// </summary>
        /// <param name="output">The writer where to write the output to.</param>
        /// <param name="func">The function to be compiled.</param>
        /// <param name="levelPrefix">The prefix string which stands for the code indentation.</param>
        private void CompileFunction(TextWriter output, UserFunction func, string levelPrefix)
        {
            output.Write(levelPrefix + "\"" + func.Signature.Name + "#" + func.Signature.Index + "\": function(");
            output.Write(string.Join(", ", func.Signature.Arguments.Select(x => x.Name).ToArray()));
            output.WriteLine(") {");

            // Compile function body
            var iter = func.Body.GetIterator();
            Scope scope = new Scope();
            foreach (var arg in func.Signature.Arguments)
                scope.RegisterVariable(arg.Name, arg.Type);
            InstructionScopeResult result = this.CompileInstructionScope(output, func, scope, iter, levelPrefix + '\t');

            // Check return type
            VerifyReturnType(result.ReturnType, func.Signature.ReturnType, func.SourcePosition);

            output.WriteLine(levelPrefix + "},");
        }

        /// <summary>
        /// Compiles an instruction scope.
        /// </summary>
        /// <param name="output">The writer where to write the output to.</param>
        /// <param name="context">The function which contains the code.</param>
        /// <param name="scope">The current scope which is being compiled.</param>
        /// <param name="iter">The operations iterator.</param>
        /// <param name="levelPrefix">The prefix string which stands for the code indentation.</param>
        /// <param name="isForInitialization">Hack: if processing the 'for'-initialization code, no ';' is output at the end.</param>
        /// <returns>the type of the return value of the instruction scope.</returns>
        private InstructionScopeResult CompileInstructionScope(TextWriter output, UserFunction context, Scope scope, IOperationCollectionIterator iter, string levelPrefix, bool isForInitialization = false)
        {
            InstructionScopeResult result;
            result.ReturnType = null;
            result.UsedVariables = new HashSet<Variable>();
            result.CreatedVariables = new HashSet<Variable>();

            Stack<StackElement> stack = new Stack<StackElement>();
            StackElement stackElement;
            while (iter.Next())
            {
                switch (iter.Current.Type)
                {
                    #region OpCode structure

                    case OperationType.NoOperation:
                        break;
                    case OperationType.Pop:
                        stackElement = stack.Pop();
                        output.Write(levelPrefix + stackElement.CodeGet);
                        output.WriteLine(";");
                        break;
                    case OperationType.Swap:
                        {
                            var o1 = stack.Pop();
                            var o2 = stack.Pop();
                            stack.Push(o1);
                            stack.Push(o2);
                        }
                        break;
                    case OperationType.BeginOpBlock:
                        // Operation blocks should not occur arbitrarily in code,
                        // but only after some instructions (for, while, ...) and handled
                        // there apropriately.
                        throw new CompileException(iter.Current.SourcePosition, "Unexpected operation block");
                    case OperationType.EndOpBlock:
                        if (stack.Count != 1)
                            throw new CompileException(iter.Current.SourcePosition, "Instruction stack should contain exactly one element at end of operation block");
                        stackElement = stack.Pop();
                        output.Write(stackElement.CodeGet);
                        result.ReturnType = stackElement.Type;
                        return result;

                    #endregion

                    #region Operators

                    case OperationType.Plus: handleGenericBinaryOperation(stack, "+", (x, y) => x.OperationPlus(y), iter.Current.SourcePosition); break;
                    case OperationType.Minus: handleGenericBinaryOperation(stack, "-", (x, y) => x.OperationMinus(y), iter.Current.SourcePosition); break;
                    case OperationType.Times: handleGenericBinaryOperation(stack, "*", (x, y) => x.OperationTimes(y), iter.Current.SourcePosition); break;
                    case OperationType.Divide:
                        {
                            var stackOtherElement = stack.Pop();
                            stackElement = stack.Pop();
                            IType resultType;
                            try
                            {
                                resultType = stackElement.Type.OperationDivide(stackOtherElement.Type);
                            }
                            catch (NotSupportedException)
                            {
                                throw new CompileException(iter.Current.SourcePosition, "Operator / not defined on " + stackElement.Type.Identifier + " and " + stackOtherElement.Type.Identifier);
                            }
                            // Javascript always does float division
                            string code = resultType.Identifier.StartsWith("float") ? "({0}/{1})" : "Math.floor(({0}/{1}))";
                            stack.Push(new StackElement(string.Format(code, stackElement.CodeGet, stackOtherElement.CodeGet), resultType));
                        }
                        break;
                    case OperationType.Modulo: handleGenericBinaryOperation(stack, "%", (x, y) => x.OperationModulo(y), iter.Current.SourcePosition); break;
                    case OperationType.LogicalNot: handleGenericUnaryOperation(stack, "!", (x) => x.OperationLogicalNot(), iter.Current.SourcePosition); break;
                    case OperationType.LogicalAnd: handleGenericBinaryOperation(stack, "&&", (x, y) => x.OperationLogicalAnd(y), iter.Current.SourcePosition); break;
                    case OperationType.LogicalOr: handleGenericBinaryOperation(stack, "||", (x, y) => x.OperationLogicalOr(y), iter.Current.SourcePosition); break;
                    case OperationType.UnaryPlus: handleGenericUnaryOperation(stack, "+", (x) => x.OperationUnaryPlus(), iter.Current.SourcePosition); break;
                    case OperationType.UnaryMinus: handleGenericUnaryOperation(stack, "-", (x) => x.OperationUnaryMinus(), iter.Current.SourcePosition); break;
                    case OperationType.BitwiseNot: handleGenericUnaryOperation(stack, "~", (x) => x.OperationBitwiseNot(), iter.Current.SourcePosition); break;
                    case OperationType.BitwiseAnd: handleGenericBinaryOperation(stack, "&", (x, y) => x.OperationBitwiseAnd(y), iter.Current.SourcePosition); break;
                    case OperationType.BitwiseOr: handleGenericBinaryOperation(stack, "|", (x, y) => x.OperationBitwiseOr(y), iter.Current.SourcePosition); break;
                    case OperationType.BitwiseXor: handleGenericBinaryOperation(stack, "^", (x, y) => x.OperationBitwiseXor(y), iter.Current.SourcePosition); break;
                    case OperationType.LeftShift: handleGenericBinaryOperation(stack, "<<", (x, y) => x.OperationLeftShift(y), iter.Current.SourcePosition); break;
                    case OperationType.RightShift: handleGenericBinaryOperation(stack, ">>", (x, y) => x.OperationRightShift(y), iter.Current.SourcePosition); break;
                    case OperationType.Equal: handleGenericBinaryOperation(stack, "==", (x, y) => x.OperationEqual(y), iter.Current.SourcePosition); break;
                    case OperationType.NotEqual: handleGenericBinaryOperation(stack, "!=", (x, y) => x.OperationNotEqual(y), iter.Current.SourcePosition); break;
                    case OperationType.LessThan: handleGenericBinaryOperation(stack, "<", (x, y) => x.OperationLessThan(y), iter.Current.SourcePosition); break;
                    case OperationType.LessThanEqual: handleGenericBinaryOperation(stack, "<=", (x, y) => x.OperationLessThanEqual(y), iter.Current.SourcePosition); break;
                    case OperationType.MoreThan: handleGenericBinaryOperation(stack, ">", (x, y) => x.OperationMoreThan(y), iter.Current.SourcePosition); break;
                    case OperationType.MoreThanEqual: handleGenericBinaryOperation(stack, ">=", (x, y) => x.OperationMoreThanEqual(y), iter.Current.SourcePosition); break;
                    case OperationType.Assign: handleGenericAssignOperation(stack, "", (x, y) => y, iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.AssignBitwiseAnd: handleGenericAssignOperation(stack, "&", (x, y) => x.OperationBitwiseAnd(y), iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.AssignBitwiseOr: handleGenericAssignOperation(stack, "|", (x, y) => x.OperationBitwiseOr(y), iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.AssignBitwiseXor: handleGenericAssignOperation(stack, "^", (x, y) => x.OperationBitwiseXor(y), iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.AssignLeftShift: handleGenericAssignOperation(stack, "<<", (x, y) => x.OperationLeftShift(y), iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.AssignRightShift: handleGenericAssignOperation(stack, ">>", (x, y) => x.OperationRightShift(y), iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.AssignPlus: handleGenericAssignOperation(stack, "+", (x, y) => x.OperationPlus(y), iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.AssignMinus: handleGenericAssignOperation(stack, "-", (x, y) => x.OperationMinus(y), iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.AssignTimes: handleGenericAssignOperation(stack, "*", (x, y) => x.OperationTimes(y), iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.AssignDivide: handleGenericAssignOperation(stack, "/", (x, y) => x.OperationDivide(y), iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.AssignModulo: handleGenericAssignOperation(stack, "%", (x, y) => x.OperationModulo(y), iter.Current.SourcePosition, output, levelPrefix); break;
                    case OperationType.SuffixIncrement: handleGenericIncDecOperation(stack, "", "++", iter.Current.SourcePosition); break;
                    case OperationType.SuffixDecrement: handleGenericIncDecOperation(stack, "", "--", iter.Current.SourcePosition); break;
                    case OperationType.PrefixIncrement: handleGenericIncDecOperation(stack, "++", "", iter.Current.SourcePosition); break;
                    case OperationType.PrefixDecrement: handleGenericIncDecOperation(stack, "--", "", iter.Current.SourcePosition); break;
                    case OperationType.PropertyAccess:
                        {
                            string propertyName = iter.Current.GetMetaData().First() as string;
                            if (propertyName == null) throw new InvalidDataException("PropertyAccess expects a string argument.");

                            // Get the object the property of which is accessed
                            stackElement = stack.Pop();
                            IType propertyType;
                            try
                            {
                                propertyType = stackElement.Type.OperationProperty(propertyName);
                            }
                            catch (NotSupportedException)
                            {
                                throw new CompileException(iter.Current.SourcePosition, "Property " + propertyName + " is not defined on " + stackElement.Type.Identifier);
                            }

                            if (stackElement.Type is CustomType)
                                stack.Push(new StackElement(stackElement.CodeGet + "." + propertyName, propertyType, true, false));
                            else
                                stack.Push(new StackElement(stackElement.CodeGet + ".prop_" + propertyName, propertyType, false, true));
                        }
                        break;
                    case OperationType.Index:
                        {
                            var stackElementIndex = stack.Pop();
                            var stackElementObject = stack.Pop();

                            IType resultType;
                            try
                            {
                                resultType = stackElementObject.Type.OperationIndex(stackElementIndex.Type);
                            }
                            catch (NotSupportedException)
                            {
                                throw new CompileException(iter.Current.SourcePosition, "Cannot index " + stackElementObject.Type.Identifier + " using " + stackElementIndex.Type.Identifier);
                            }

                            if (stackElementObject.Type is StringType)
                                stack.Push(new StackElement(stackElementObject.RawCode + ".charCodeAt(" + stackElementIndex.CodeGet + ")", resultType, false /* read-only */, false));
                            else
                                stack.Push(new StackElement(stackElementObject.RawCode + "[" + stackElementIndex.CodeGet + "]", resultType, true, false));
                        }
                        break;
                    case OperationType.Call:
                        {
                            // Read arguments passed to the function
                            int argCount = (int)iter.Current.GetMetaData().First();
                            StringBuilder argString = new StringBuilder("(");
                            StackElement[] args = new StackElement[argCount];
                            IType[] argTypes = new IType[argCount];
                            for (int i = 0; i < argCount; i++)
                            {
                                argTypes[argCount - 1 - i] = (args[argCount - 1 - i] = stack.Pop()).Type;
                            }
                            for (int i = 0; i < argCount; i++)
                            {
                                if (i > 0) argString.Append(",");

                                // Whenever we assign a struct value ('custom type') to a new variable,
                                // we have to create a copy of the value (in Javascript)
                                if (argTypes[i] is CustomType)
                                    argString.Append("$vm.copy(" + args[i].CodeGet + ")");
                                else
                                    argString.Append(args[i].CodeGet);
                            }
                            argString.Append(")");

                            // Read function
                            stackElement = stack.Pop();
                            if (!(stackElement.Type is FuncType))
                                throw new CompileException(iter.Current.SourcePosition, "Only functions can be called");
                            string[] funcPath = ((FuncType)stackElement.Type).FunctionPath;

                            // Resolve function
                            var func = ResolveFunction(funcPath, context.Signature.Namespace, scope, argTypes);
                            if (func.Index < 0)
                                throw new CompileException(iter.Current.SourcePosition, "Could not resolve function " + string.Join("::", funcPath));

                            // Output
                            StringBuilder funcOutput = new StringBuilder();
                            if (func.IsBuiltin && func.Namespace.Length == 0 && func.Name == "$")
                            {
                                funcOutput.Append("$vm.led");
                            }
                            else
                            {
                                if (func.IsBuiltin)
                                    funcOutput.Append("$vm.builtin(");
                                else
                                    funcOutput.Append("$vm.user(");
                                for (int i = 0; i < func.Namespace.Length; i++)
                                {
                                    funcOutput.Append("\"");
                                    funcOutput.Append(func.Namespace[i].Replace("\"", "\\\""));
                                    funcOutput.Append("\",");
                                }
                                funcOutput.Append("\"");
                                funcOutput.Append(func.Name.Replace("\"", "\\\""));
                                funcOutput.Append("#");
                                funcOutput.Append(func.Index);
                                funcOutput.Append("\")");
                            }
                            funcOutput.Append(argString);
                            stack.Push(new StackElement(funcOutput.ToString(), func.ReturnType));
                        }
                        break;

                    #endregion

                    #region Loops

                    case OperationType.BeginScope:
                        output.WriteLine(levelPrefix + "{");
                        var r = CompileInstructionScope(output, context, new Scope(scope), iter, levelPrefix + '\t');
                        result.UsedVariables.UnionWith(r.UsedVariables);
                        result.CreatedVariables.UnionWith(r.CreatedVariables);
                        output.WriteLine(levelPrefix + "}");
                        if (!result.HasReturnType)
                            result.ReturnType = r.ReturnType;
                        break;
                    case OperationType.EndScope:
                        if (stack.Count != 0)
                            throw new CompileException(iter.Current.SourcePosition, "Instruction stack should be empty at end of scope");
                        return result;
                    case OperationType.If:
                        stackElement = stack.Pop(); // stack contains `if` condition
                        if (stackElement.Type.Identifier != "bool")
                            throw new CompileException(iter.Current.SourcePosition, "If-statement expects a boolean value as its condition");
                        output.Write(levelPrefix + "if (");
                        output.Write(stackElement.CodeGet);
                        output.WriteLine(") {");

                        // Compile `if` body
                        iter.Next();
                        InstructionScopeResult retType1 = CompileInstructionScope(output, context, new Scope(scope), iter, levelPrefix + '\t');
                        InstructionScopeResult retType2 = default(InstructionScopeResult);
                        result.UsedVariables.UnionWith(retType1.UsedVariables);
                        result.CreatedVariables.UnionWith(retType1.CreatedVariables);
                        output.WriteLine(levelPrefix + "}");

                        // Check if there is an `else` statement
                        iter.Next();
                        switch (iter.Current.Type)
                        {
                            case OperationType.NoOperation: // there is not
                                break;
                            case OperationType.BeginScope: // there is
                                output.WriteLine(levelPrefix + "else {");
                                retType2 = CompileInstructionScope(output, context, new Scope(scope), iter, levelPrefix + '\t');
                                result.UsedVariables.UnionWith(retType2.UsedVariables);
                                result.CreatedVariables.UnionWith(retType2.CreatedVariables);
                                output.WriteLine(levelPrefix + "}");
                                break;
                            default:
                                throw new CompileException(iter.Current.SourcePosition, "Unexpected operation after if-statement: " + iter.Current.Type);
                        }

                        // If both statements return something, the whole block does so as well.
                        // Note that we checked type compatibility when compiling the RETURN operation itself,
                        // see `case OperationType.Return`.
                        if (retType1.HasReturnType && retType2.HasReturnType)
                        {
                            if (!result.HasReturnType)
                                result.ReturnType = context.Signature.ReturnType;
                        }
                        break;
                    case OperationType.For:
                        var scopeBody = new Scope(scope);
                        output.Write(levelPrefix + "for (");

                        // Compile `for`-initialization, discard its return type (we do not expect any, anyway)
                        if (!iter.Next() || iter.Current.Type != OperationType.BeginScope)
                            throw new CompileException(iter.Current.SourcePosition, "Expected scope after for-statement");
                        var r1 = this.CompileInstructionScope(output, context, scopeBody, iter, string.Empty, isForInitialization: true);
                        result.CreatedVariables.UnionWith(r1.CreatedVariables);
                        result.UsedVariables.UnionWith(r1.UsedVariables);
                        output.Write("; ");

                        // Compile `for`-condition
                        if (!iter.Next() || iter.Current.Type != OperationType.BeginOpBlock)
                            throw new CompileException(iter.Current.SourcePosition, "Expected operation block after for-statement");
                        long forCondPos = iter.Current.SourcePosition;
                        var forCondType = this.CompileInstructionScope(output, context, scopeBody, iter, string.Empty);
                        if (!forCondType.HasReturnType || forCondType.ReturnType.Identifier != "bool")
                            throw new CompileException(forCondPos, "Expected boolean value for for-condition");
                        output.Write("; ");

                        // Compile `for`-postaction, discard its return type (we do not expect any, anyway)
                        if (!iter.Next() || iter.Current.Type != OperationType.BeginOpBlock)
                            throw new CompileException(iter.Current.SourcePosition, "Expected operation block after for-statement");
                        var r2 = this.CompileInstructionScope(output, context, scopeBody, iter, string.Empty);
                        result.CreatedVariables.UnionWith(r2.CreatedVariables);
                        result.UsedVariables.UnionWith(r2.UsedVariables);
                        output.WriteLine(") {");

                        // Compile `for` body
                        if (!iter.Next() || iter.Current.Type != OperationType.BeginScope)
                            throw new CompileException(iter.Current.SourcePosition, "Expected scope after for-statement");
                        InstructionScopeResult forReturnType = CompileInstructionScope(output, context, scopeBody, iter, levelPrefix + '\t');
                        result.UsedVariables.UnionWith(forReturnType.UsedVariables);
                        result.CreatedVariables.UnionWith(forReturnType.CreatedVariables);
                        output.WriteLine(levelPrefix + "}");

                        // Handle case where there is a `return` statement in the `for` body
                        if (forReturnType.HasReturnType)
                        {
                            if (!result.HasReturnType)
                                result.ReturnType = forReturnType.ReturnType;
                        }
                        break;
                    case OperationType.While:
                        output.Write(levelPrefix + "while (");

                        // Compile `while`-condition
                        if (!iter.Next() || iter.Current.Type != OperationType.BeginOpBlock)
                            throw new CompileException(iter.Current.SourcePosition, "Expected operation block after while-statement");
                        long whileCondPos = iter.Current.SourcePosition;
                        var whileCondType = this.CompileInstructionScope(output, context, scope, iter, levelPrefix + '\t');
                        if (!whileCondType.HasReturnType || whileCondType.ReturnType.Identifier != "bool")
                            throw new CompileException(whileCondPos, "Expected boolean value for while-condition");
                        output.WriteLine(") {");

                        // Compile `while` body
                        if (!iter.Next() || iter.Current.Type != OperationType.BeginScope)
                            throw new CompileException(iter.Current.SourcePosition, "Expected scope after while-statement");
                        InstructionScopeResult whileReturnType = CompileInstructionScope(output, context, new Scope(scope), iter, levelPrefix + '\t');
                        result.UsedVariables.UnionWith(whileReturnType.UsedVariables);
                        result.CreatedVariables.UnionWith(whileReturnType.CreatedVariables);
                        output.WriteLine(levelPrefix + "}");

                        // Handle case where there is a `return` statement in the `while` body
                        if (whileReturnType.HasReturnType)
                        {
                            if (!result.HasReturnType)
                                result.ReturnType = whileReturnType.ReturnType;
                        }
                        break;
                    case OperationType.DoWhile:
                        output.WriteLine(levelPrefix + "do {");

                        // Compile `while` body
                        if (!iter.Next() || iter.Current.Type != OperationType.BeginScope)
                            throw new CompileException(iter.Current.SourcePosition, "Expected scope after do-statement");
                        var dowhileReturnType = CompileInstructionScope(output, context, new Scope(scope), iter, levelPrefix + '\t');
                        output.WriteLine(levelPrefix + "}");
                        output.Write(levelPrefix + "while (");

                        // Compile `while`-condition
                        if (!iter.Next() || iter.Current.Type != OperationType.BeginOpBlock)
                            throw new CompileException(iter.Current.SourcePosition, "Expected operation block after do-statement");
                        long dowhileCondPos = iter.Current.SourcePosition;
                        var dowhileCondType = this.CompileInstructionScope(output, context, scope, iter, string.Empty);
                        result.UsedVariables.UnionWith(dowhileCondType.UsedVariables);
                        result.CreatedVariables.UnionWith(dowhileCondType.CreatedVariables);
                        if (!dowhileCondType.HasReturnType || dowhileCondType.ReturnType.Identifier != "bool")
                            throw new CompileException(dowhileCondPos, "Expected boolean value for do-condition");
                        output.WriteLine(");");

                        // Handle case where there is a `return` statement in the `while` body
                        if (dowhileReturnType.HasReturnType)
                        {
                            if (!result.HasReturnType)
                                result.ReturnType = dowhileReturnType.ReturnType;
                        }
                        break;

                    #endregion

                    #region Object creation

                    case OperationType.LoadSymbol:
                        {
                            string variableName = iter.Current.GetMetaData().First() as string;
                            if (variableName == null) throw new InvalidDataException("LoadSymbol expects a string argument.");
                            if (!scope.HasVariable(variableName))
                                throw new CompileException(iter.Current.SourcePosition, "Cannot resolve variable: " + variableName);
                            IType variableType = scope.GetVariableType(variableName);
                            stack.Push(new StackElement(variableName, variableType, true, false));

                            // Remember that we used this variable
                            result.UsedVariables.Add(new Variable(variableName, variableType));
                        }
                        break;
                    case OperationType.LoadConstant:
                        object cst = iter.Current.GetMetaData().First();
                        if (cst is string)
                        {
                            stack.Push(new StackElement("\"" + ((string)cst).Replace("\"", "\\\"") + "\"", this.BuiltinTypes.Lookup("string")));
                        }
                        else if (cst is bool)
                        {
                            stack.Push(new StackElement((bool)cst ? "true" : "false", this.BuiltinTypes.Lookup("bool")));
                        }
                        else if (cst is RawInt)
                        {
                            string typename = (((RawInt)cst).Unsigned ? "u" : "") + (((RawInt)cst).Bits > 0 ? "int" + ((RawInt)cst).Bits : "int");
                            stack.Push(new StackElement(((RawInt)cst).String, this.BuiltinTypes.Lookup(typename)));
                        }
                        else if (cst is RawFloat)
                        {
                            string typename = ((RawFloat)cst).Bits > 0 ? "float" + ((RawFloat)cst).Bits : "float";
                            stack.Push(new StackElement(((RawFloat)cst).String, this.BuiltinTypes.Lookup(typename)));
                        }
                        else
                        {
                            throw new InvalidDataException("LoadConstant has an unsupported argument type: " + cst.GetType().Name);
                        }
                        break;
                    case OperationType.LoadUndefined:
                        // This operation is only used in combination with CreateVariable.
                        // It is processed in `case OperationType.CreateVariable`.
                        break;
                    case OperationType.LoadFunction:
                        {
                            string[] funcPath = (string[])iter.Current.GetMetaData().First();
                            stack.Push(new StackElement(null, new FuncType(funcPath)));
                        }
                        break;
                    case OperationType.MakeList:
                        {
                            // Get the array length
                            int length = (int)iter.Current.GetMetaData().First();

                            // Read the array elements in reverse order (!) from the stack
                            StackElement[] array = new StackElement[length];
                            for (int i = 0; i < length; i++)
                                array[length - 1 - i] = stack.Pop();

                            // Compile the elements and remember the common data type
                            IType commonType = null;
                            StringBuilder sbEntries = new StringBuilder("[");
                            for (int i = 0; i < length; i++)
                            {
                                var e = array[i];
                                if (i == 0)
                                    commonType = e.Type;
                                else if (commonType.CanCastTo(e.Type))
                                    commonType = e.Type;
                                else if (!e.Type.CanCastTo(commonType))
                                    throw new CompileException(iter.Current.SourcePosition, "Incompatible types in array");

                                if (i > 0) sbEntries.Append(",");
                                sbEntries.Append(e.CodeGet);
                            }
                            sbEntries.Append("]");
                            stack.Push(new StackElement(sbEntries.ToString(), new ArrayType(commonType)));
                        }
                        break;
                    case OperationType.MakeVector:
                        {
                            // Get the vector dimension
                            int dimension = (int)iter.Current.GetMetaData().First();

                            // Read the vector elements in reverse order (!) from the stack
                            StackElement[] vec = new StackElement[dimension];
                            for (int i = 0; i < dimension; i++)
                                vec[dimension - 1 - i] = stack.Pop();

                            // Compile the elements; they have to be of type float
                            IType commonType = this.BuiltinTypes.Lookup("float");
                            StringBuilder sbEntries = new StringBuilder("[");
                            for (int i = 0; i < dimension; i++)
                            {
                                var e = vec[i];
                                if (!e.Type.CanCastTo(commonType))
                                    throw new CompileException(iter.Current.SourcePosition, "Vector element must be floating point numbers");

                                if (i > 0) sbEntries.Append(",");
                                sbEntries.Append(e.CodeGet);
                            }
                            sbEntries.Append("]");
                            stack.Push(new StackElement(sbEntries.ToString(), new VecType(dimension)));
                        }
                        break;

                    #endregion

                    #region Program interaction

                    case OperationType.CreateVariable:
                        {
                            // Check if the variable is also initialized
                            bool hasValue = true;
                            if (iter.Back())
                            {
                                if (iter.Current.Type == OperationType.LoadUndefined)
                                    hasValue = false;
                                iter.Next();
                            }

                            IType variableType = iter.Current.GetMetaData().First() as IType;
                            string variableName = iter.Current.GetMetaData().Skip(1).First() as string;
                            if (object.ReferenceEquals(null, variableType) || object.ReferenceEquals(null, variableName))
                                throw new InvalidDataException("CreateVariable expects an <IType,string> argument.");

                            // Register variable
                            if (scope.HasVariable(variableName))
                                throw new CompileException(iter.Current.SourcePosition, "A variable with this identifier already exists: " + variableName);
                            scope.RegisterVariable(variableName, variableType);

                            // Output statement
                            output.Write(levelPrefix + "var " + variableName);
                            if (hasValue)
                            {
                                // Check type compatibility
                                stackElement = stack.Pop();
                                if (!stackElement.Type.CanCastTo(variableType))
                                    throw new CompileException(iter.Current.SourcePosition, "Cannot convert " + stackElement.Type.Identifier + " to " + variableType.Identifier);
                                // Whenever we assign a struct value ('custom type') to a new variable,
                                // we have to create a copy of the value (in Javascript)
                                if (stackElement.Type is CustomType)
                                    output.Write("=$vm.copy(" + stackElement.CodeGet + ")");
                                else
                                    output.Write("=" + stackElement.CodeGet);
                            }
                            else if (variableType is CustomType)
                            {
                                // In Javascript, we have to initialize objects so that its properties are known
                                Func<CustomType, string> initCode = null;
                                initCode = (t) =>
                                {
                                    var f1 = t.Fields.Where(x => x.Value is CustomType).Select(x => x.Key + ":" + initCode((CustomType)x.Value));
                                    var f2 = t.Fields.Where(x => !(x.Value is CustomType)).Select(x => x.Key + ":null");
                                    return "{" + string.Join(",", f1.Concat(f2).ToArray()) + "}";
                                };
                                output.Write("=" + initCode((CustomType)variableType));
                            }
                            if (!isForInitialization)
                                output.WriteLine(";");

                            // Remember that we used this variable
                            result.UsedVariables.Add(new Variable(variableName, variableType));
                            result.CreatedVariables.Add(new Variable(variableName, variableType));
                        }
                        break;
                    case OperationType.Return:
                        output.Write(levelPrefix + "return;");

                        // Check compatibility
                        if (!object.ReferenceEquals(null, context.Signature.ReturnType))
                            throw new CompileException(iter.Current.SourcePosition, "The function requires a value to be returned");
                        break;
                    case OperationType.ReturnValue:
                        if (stack.Count != 1)
                            throw new CompileException(iter.Current.SourcePosition, "Instruction stack should contain exactly one element at RETURN operation");
                        stackElement = stack.Pop();
                        output.Write(levelPrefix + "return ");
                        output.WriteLine(stackElement.CodeGet);
                        output.Write(";");

                        // Check compatibility
                        if (object.ReferenceEquals(null, context.Signature.ReturnType))
                            throw new CompileException(iter.Current.SourcePosition, "The function does not allow values to be returned");
                        if (!stackElement.Type.CanCastTo(context.Signature.ReturnType))
                            throw new CompileException(iter.Current.SourcePosition, "Cannot convert " + stackElement.Type.Identifier + " to " + context.Signature.ReturnType.Identifier);

                        if (!result.HasReturnType)
                            result.ReturnType = stackElement.Type;
                        break;
                    case OperationType.Import:
                        scope.ImportNamespace((string[])iter.Current.GetMetaData().First());
                        break;
                    case OperationType.Async:
                        stackElement = stack.Pop(); // contains the queue identifier
                        if (!stackElement.Type.CanCastTo(this.BuiltinTypes.Lookup("int")))
                            throw new CompileException(iter.Current.SourcePosition, "Async identifier must be an integer");
                        if (!iter.Next() || iter.Current.Type != OperationType.BeginScope)
                            throw new CompileException(iter.Current.SourcePosition, "Expected scope after async-statement");

                        using (MemoryStream tmpMemory = new MemoryStream())
                        using (StreamWriter tmpMemoryWriter = new StreamWriter(tmpMemory, output.Encoding))
                        {
                            InstructionScopeResult tmpResult = CompileInstructionScope(tmpMemoryWriter, context, new Scope(scope), iter, levelPrefix + '\t');
                            tmpMemoryWriter.Flush();

                            // Since the command is executed asynchrounously, the value of the variables
                            // used in the command might have changed in the meantime.
                            // In Javascript we can bypass this problem by wrapping the call inside another function

                            StringBuilder asyncArgsDef = new StringBuilder();
                            StringBuilder asyncArgsPassed = new StringBuilder();
                            foreach (Variable v in tmpResult.UsedVariables.Except(tmpResult.CreatedVariables))
                            {
                                if (asyncArgsDef.Length > 0) asyncArgsDef.Append(", ");
                                if (asyncArgsPassed.Length > 0) asyncArgsPassed.Append(", ");

                                asyncArgsDef.Append(v);
                                // Whenever we assign a struct value ('custom type') to a new variable,
                                // we have to create a copy of the value (in Javascript)
                                if (v.Type is CustomType)
                                    asyncArgsPassed.Append("$vm.copy(" + v + ")");
                                else
                                    asyncArgsPassed.Append(v);
                            }

                            output.Write(levelPrefix + "$vm.async(");
                            output.Write(stackElement.CodeGet);
                            output.Write(", (function(");
                            output.Write(asyncArgsDef.ToString());
                            output.WriteLine(") { return function() { ");
                            output.Write(output.Encoding.GetString(tmpMemory.ToArray()));
                            output.Write(levelPrefix + "}; })(");
                            output.Write(asyncArgsPassed.ToString());
                            output.WriteLine("));");
                        }
                        break;
                    case OperationType.AsyncWait:
                        var asyncTime = stack.Pop(); // waiting time in seconds
                        var asyncId = stack.Pop(); // queue identifier
                        if (!asyncId.Type.CanCastTo(this.BuiltinTypes.Lookup("int")))
                            throw new CompileException(iter.Current.SourcePosition, "Async identifier must be an integer");
                        if (!asyncTime.Type.CanCastTo(this.BuiltinTypes.Lookup("float")))
                            throw new CompileException(iter.Current.SourcePosition, "Async waiting time must be a number (float/int)");
                        output.Write(levelPrefix + "$vm.async(");
                        output.Write(asyncId.CodeGet);
                        output.Write(", ");
                        output.Write(asyncTime.CodeGet);
                        output.WriteLine(");");
                        break;
                    case OperationType.SyncAbort:
                        stackElement = stack.Pop(); // contains the queue identifier
                        if (!(stackElement.Type is IntType))
                            throw new CompileException(iter.Current.SourcePosition, "Async identifier must be an integer");
                        output.Write(levelPrefix + "$vm.sync(");
                        output.Write(stackElement.CodeGet);
                        output.Write(", false);");
                        break;
                    case OperationType.SyncEnd:
                        stackElement = stack.Pop(); // contains the queue identifier
                        if (!(stackElement.Type is IntType))
                            throw new CompileException(iter.Current.SourcePosition, "Async identifier must be an integer");
                        output.Write(levelPrefix + "$vm.sync(");
                        output.Write(stackElement.CodeGet);
                        output.Write(", true);");
                        break;

                    #endregion

                    default:
                        throw new CompileException(iter.Current.SourcePosition, "Operation not implemented: " + iter.Current.Type);
                }
            }

            if (stack.Count != 0)
                throw new CompileException(iter.Current.SourcePosition, "Instruction stack should be empty at end of scope");
            return result;
        }

        private void handleGenericBinaryOperation(Stack<StackElement> stack, string opChar, Func<IType, IType, IType> op, long position)
        {
            var stackOtherElement = stack.Pop();
            var stackElement = stack.Pop();
            IType resultType;
            try
            {
                resultType = op(stackElement.Type, stackOtherElement.Type);
            }
            catch (NotSupportedException)
            {
                throw new CompileException(position, "Operator " + opChar + " not defined on " + stackElement.Type.Identifier + " and " + stackOtherElement.Type.Identifier);
            }

            string newcode;
            if (opChar == "/" && resultType is IntType)
            {
                // Javascript always does float division
                newcode = "Math.floor(({0}/{1}))";
            }
            else if (stackElement.Type is VecType || stackOtherElement.Type is VecType)
            {
                switch (opChar)
                {
                    case "+": newcode = "$vm.Vector.plus({0},{1})"; break;
                    case "-": newcode = "$vm.Vector.minus({0},{1})"; break;
                    case "*":
                        if (stackElement.Type is VecType && stackOtherElement.Type is VecType)
                            newcode = "$vm.Vector.scalarProduct({0},{1})";
                        else
                            newcode = "$vm.Vector.times({0},{1})";
                        break;
                    case "/": newcode = "$vm.Vector.divide({0},{1})"; break;
                    case "==": newcode = "$vm.Vector.equal({0},{1})"; break;
                    case "!=": newcode = "$vm.Vector.notEqual({0},{1})"; break;
                    default: throw new ArgumentException("Unsupported binary operation on vectors: " + opChar);
                }
            }
            else
            {
                newcode = "({0}" + opChar + "{1})";
            }
            stack.Push(new StackElement(string.Format(newcode, stackElement.CodeGet, stackOtherElement.CodeGet), resultType));
        }

        private void handleGenericUnaryOperation(Stack<StackElement> stack, string opChar, Func<IType, IType> op, long position)
        {
            var stackElement = stack.Pop();
            IType resultType;
            try
            {
                resultType = op(stackElement.Type);
            }
            catch (NotSupportedException)
            {
                throw new CompileException(position, "Operator " + opChar + " not defined on " + stackElement.Type.Identifier);
            }

            string newcode;
            if (resultType is VecType)
            {
                switch (opChar)
                {
                    case "+": newcode = "{0}"; break;
                    case "-": newcode = "$vm.Vector.negate({0})"; break;
                    default: throw new ArgumentException("Unsupported unary operation on vectors: " + opChar);
                }
            }
            else
            {
                newcode = "(" + opChar + "{0})";
            }
            stack.Push(new StackElement(string.Format(newcode, stackElement.CodeGet), resultType));
        }

        private void handleGenericAssignOperation(Stack<StackElement> stack, string opChar, Func<IType, IType, IType> op, long position, TextWriter output, string levelPrefix)
        {
            var stackElement = stack.Pop(); // this should be a variable
            var stackOtherElement = stack.Pop();
            IType resultType;
            if (!stackElement.IsVariable && !stackElement.IsProperty)
                throw new CompileException(position, "Cannot assign a value to non-variable/non-property");

            try
            {
                resultType = op(stackElement.Type, stackOtherElement.Type);
            }
            catch (NotSupportedException)
            {
                throw new CompileException(position, "Operator not defined on " + stackElement.Type.Identifier + " and " + stackOtherElement.Type.Identifier);
            }

            // Types have to match
            if (!resultType.CanCastTo(stackElement.Type))
            {
                throw new CompileException(position, "Cannot cast " + resultType.Identifier + " to " + stackElement.Type.Identifier);
            }

            // Handle the case where we access a property with an modification-assignment operator
            // (like +=, *=, &=, etc., but not =)
            if (stackElement.IsProperty && opChar.Length > 0)
            {
                // We have to create an additional temporary variable
                string tmpVarName = "tmp$" + _tempVariableCounter++;
                output.WriteLine(levelPrefix + "var " + tmpVarName + "=" + stackElement.RawCode + ";");

                // NOTE: It should not be necessary to remember this temporary variable
                // in the .UsedVariables field of the current instruction scope,
                // since it is really used only in this scope, and not in sub-scopes.
                // That is due to the fact that if we would need it in a sub-scope,
                // the creation of the temporary variable would also have been made there.

                string newcode = "{0}({0}()" + opChar + "{1})";
                stack.Push(new StackElement(string.Format(newcode, tmpVarName, stackOtherElement.CodeGet), resultType));
            }
            else
            {
                string newcode;
                if (stackElement.IsProperty)
                {
                    if (opChar.Length > 0)
                        newcode = "{0}({1}" + opChar + "{2})";
                    else
                        newcode = "{0}({2})";
                }
                else
                {
                    if (opChar.Length > 0)
                    {
                        if (opChar == "/" && resultType is IntType)
                        {
                            // Javascript always does float division
                            newcode = "({0}=Math.floor({1}/{2}))";
                        }
                        else if (stackElement.Type is VecType || stackOtherElement.Type is VecType)
                        {
                            switch (opChar)
                            {
                                case "+": newcode = "({0}=$vm.Vector.plus({1},{2}))"; break;
                                case "-": newcode = "({0}=$vm.Vector.minus({1},{2}))"; break;
                                case "*": newcode = "({0}=$vm.Vector.times({1},{2}))"; break;
                                case "/": newcode = "({0}=$vm.Vector.divide({1},{2}))"; break;
                                default: throw new ArgumentException("Unsupported binary operation on vectors: " + opChar);
                            }
                        }
                        else
                            newcode = "({0}={1}" + opChar + "{2})";
                    }
                    else
                    {
                        newcode = "({0}={2})";
                    }
                }
                stack.Push(new StackElement(string.Format(newcode, stackElement.RawCode, stackElement.CodeGet, stackOtherElement.CodeGet), resultType));
            }
        }

        private void handleGenericIncDecOperation(Stack<StackElement> stack, string opPrefix, string opSuffix, long position)
        {
            var stackElement = stack.Pop(); // this should be a variable
            if (stackElement.IsProperty)
                throw new CompileException(position, "Cannot increment/decrement properties");
            if (!stackElement.IsVariable)
                throw new CompileException(position, "Cannot increment/decrement a non-variable");
            if (!(stackElement.Type is IntType))
                throw new CompileException(position, "Cannot increment/decrement a non-integer variable");

            stack.Push(new StackElement("(" + opPrefix + stackElement.RawCode + opSuffix + ")", stackElement.Type));
        }

    }
}