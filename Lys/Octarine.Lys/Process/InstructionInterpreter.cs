/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Octarine.Lys.Language;
using Octarine.Lys.Parse;

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Class for interpreting instructions (such as inside functions) as operations.
    /// This class DOES NOT interprete functions itself (nor classes, namespaces, ...)
    /// </summary>
    public class InstructionInterpreter : IInterpreter
    {
        /// <summary>
        /// Initializes a new interpreter.
        /// </summary>
        /// <param name="function">The function description in the context of which the instructions are interpreted.</param>
        /// <param name="tokenIterator">The tokenizer wrapper where to get the tokens from.</param>
        public InstructionInterpreter(FunctionContext function, TokenIterator tokenIterator)
        {
            if (object.ReferenceEquals(null, tokenIterator))
                throw new ArgumentNullException("tokenIterator");
            _iterator = tokenIterator;
            _function = function;
        }

        private TokenIterator _iterator;
        private FunctionContext _function;

        /// <summary>
        /// Interpretes the next instruction.
        /// </summary>
        public IOperationCollection InterpreteNext()
        {
            // Create a global scope
            Scope scope = new Scope();
            foreach (var v in _function.Signature.Arguments)
                scope.RegisterVariable(v.Name, v.Type);

            // Handle the first operation in the precedence chain
            return pushScopeBlock(scope, false);
        }

        private Func<long, IOperationCollection> o(OperationType type)
        {
            return (position) => new SingleOperation(new SimpleOperation(type, position));
        }

        private IOperationCollection pushGenericBinaryLeftToRightOperation(Scope scope, Func<Scope, IOperationCollection> nextLevel, Dictionary<TokenType, Func<long, IOperationCollection>> operationConverters)
        {
            // left-to-right
            IOperationCollection operations = new OperationCollection();

            // Get allowed operations
            TokenType[] allOperationTokens = operationConverters.Keys.ToArray();

            // Push first value
            operations.Append(nextLevel(scope));
            while (_iterator.Is(allOperationTokens))
            {
                var operation = _iterator.Current.Type;
                var position = _iterator.Position;
                _iterator.Next();

                // Push next value ...
                operations.Append(nextLevel(scope));

                // ... and the operation
                operations.Append(operationConverters[operation](position));
            }

            return operations;
        }

        private IOperationCollection pushGenericBinaryRightToLeftOperation(Scope scope, Func<Scope, IOperationCollection> nextLevel, Dictionary<TokenType, Func<long, IOperationCollection>> operationConverters)
        {
            // right-to-left
            IOperationCollection operations = new OperationCollection();

            // Get allowed operations
            TokenType[] allOperationTokens = operationConverters.Keys.ToArray();

            // Store first value
            var value = nextLevel(scope);

            // Check if there is an operation
            while (_iterator.Is(allOperationTokens))
            {
                // Push operation
                operations.Prepend(operationConverters[_iterator.Current.Type](_iterator.Position));
                _iterator.Next();

                // Push value
                operations.Prepend(value);

                // Read next value
                value = nextLevel(scope);
            }

            // Push last value
            operations.Prepend(value);

            return operations;
        }

        private IOperationCollection pushGenericUnaryRightToLeftOperation(Scope scope, Func<Scope, IOperationCollection> nextLevel, Dictionary<TokenType, Func<long, IOperationCollection>> operationConverters)
        {
            // right-to-left
            IOperationCollection operations = new OperationCollection();

            // Get allowed operations
            TokenType[] allOperationTokens = operationConverters.Keys.ToArray();

            // Push first value
            while (_iterator.Is(allOperationTokens))
            {
                // Push the operation
                operations.Prepend(operationConverters[_iterator.Current.Type](_iterator.Position));
                _iterator.Next();
            }

            // Push value itself
            operations.Prepend(nextLevel(scope));

            return operations;
        }

        private IOperationCollection pushGenericUnaryLeftToRightOperation(Scope scope, Func<Scope, IOperationCollection> nextLevel, Dictionary<TokenType, Func<long, IOperationCollection>> operationConverters)
        {
            // left-to-right
            IOperationCollection operations = new OperationCollection();

            // Get allowed operations
            TokenType[] allOperationTokens = operationConverters.Keys.ToArray();

            // Push value itself
            operations.Append(nextLevel(scope));

            while (_iterator.Is(allOperationTokens))
            {
                // Push the operation
                operations.Append(operationConverters[_iterator.Current.Type](_iterator.Position));
                _iterator.Next();
            }

            return operations;
        }


        private IOperationCollection pushScopeBlock(Scope scope, bool forceScoping)
        {
            if (_iterator.Is(TokenType.CurlyBracketLeft))
            {
                IOperationCollection operations = new OperationCollection();
                operations.Append(new SimpleOperation(OperationType.BeginScope, _iterator.Position));

                _iterator.Next();

                while (!_iterator.Is(TokenType.CurlyBracketRight))
                {
                    operations.Append(pushScopeBlock(scope, false));
                }

                operations.Append(new SimpleOperation(OperationType.EndScope, _iterator.Position));

                _iterator.Next();
                return operations;
            }
            else
            {
                IOperationCollection operations = new OperationCollection();
                if (forceScoping) operations.Append(new SimpleOperation(OperationType.BeginScope, -1));
                operations.Append(pushLoop(scope));
                if (forceScoping) operations.Append(new SimpleOperation(OperationType.EndScope, -1));
                return operations;
            }
        }

        private IOperationCollection pushLoop(Scope scope)
        {
            if (_iterator.Is(TokenType.Name))
            {
                switch (_iterator.GetValue<string>())
                {
                    case "if": return pushSingleInstruction_if(scope);
                    case "for": return pushSingleInstruction_for(scope);
                    case "while": return pushSingleInstruction_while(scope);
                    case "do": return pushSingleInstruction_do_repeat(scope, true);
                    case "repeat": return pushSingleInstruction_do_repeat(scope, false);
                }
            }

            return pushTerminatedInstruction(scope);
        }

        private IOperationCollection pushTerminatedInstruction(Scope scope)
        {
            // Handle special keyword instructions
            if (_iterator.Is(TokenType.Name))
            {
                switch (_iterator.GetValue<string>())
                {
                    case "return": return pushTerminatedInstruction_return(scope);
                    case "import": return pushTerminatedInstruction_import(scope);
                    case "async": return pushTerminatedInstruction_async(scope);
                    case "sync": return pushTerminatedInstruction_sync(scope);
                }
            }

            // Otherwise we expect a variable assignment
            IOperationCollection operations = pushVariableAssignment(scope);
            if (operations.Count > 0 && operations.Last.Type != OperationType.CreateVariable)
                operations.Append(new SimpleOperation(OperationType.Pop, _iterator.Position));

            if (!_iterator.Is(TokenType.EndOfInstruction))
                throw new SyntaxException(_iterator.Position, "Expected ';'");
            _iterator.Next();

            return operations;
        }

        private IOperationCollection pushTerminatedInstruction_return(Scope scope)
        {
            long positionReturn = _iterator.Position;
            _iterator.Next();

            // Read what shall be returned
            IOperationCollection operations;
            if (_iterator.Is(TokenType.EndOfInstruction))
            {
                operations = new OperationCollection();
                operations.Append(new SimpleOperation(OperationType.Return, positionReturn));
            }
            else
            {
                operations = pushAssign(scope);
                operations.Append(new SimpleOperation(OperationType.ReturnValue, positionReturn));
            }

            if (!_iterator.Is(TokenType.EndOfInstruction))
                throw new SyntaxException(_iterator.Position, "Expected ';'");
            _iterator.Next();

            return operations;
        }

        private IOperationCollection pushTerminatedInstruction_import(Scope scope)
        {
            long positionImport = _iterator.Position;
            _iterator.Next();

            // Read what shall be imported
            string[] importedNamespace = readNamespacePath(false).ToArray();

            if (!_iterator.Is(TokenType.EndOfInstruction))
                throw new SyntaxException(_iterator.Position, "Expected ';'");
            _iterator.Next();

            IOperationCollection operations = new OperationCollection();
            operations.Append(new ParametrizedOperation<string[]>(OperationType.Import, positionImport, importedNamespace));
            return operations;
        }

        private IOperationCollection pushTerminatedInstruction_async(Scope scope)
        {
            long asyncPosition = _iterator.Position;

            // The syntax is 'async(name) instr;'
            _iterator.Next();
            // We expect a '('
            if (!_iterator.Is(TokenType.BracketLeft))
                throw new SyntaxException(_iterator.Position, "Expected '(' after 'async'.");
            _iterator.Next();
            // Read name
            IOperationCollection operations_name = pushAssign(scope);
            // We expect a ')'
            if (!_iterator.Is(TokenType.BracketRight))
                throw new SyntaxException(_iterator.Position, "Expected ')' after 'async('.");
            _iterator.Next();

            // Handle 'wait' instruction
            if (_iterator.Is(TokenType.Name) && _iterator.GetValue<string>() == "wait")
            {
                _iterator.Next();

                // Read expression which evaluates to the wait time
                IOperationCollection operations_waitTime = pushAssign(scope);

                if (!_iterator.Is(TokenType.EndOfInstruction))
                    throw new SyntaxException(_iterator.Position, "Expected ';'");
                _iterator.Next();

                IOperationCollection operations = new OperationCollection();
                operations.Append(operations_name);
                operations.Append(operations_waitTime);
                operations.Append(new SimpleOperation(OperationType.AsyncWait, asyncPosition));
                return operations;
            }
            else
            {
                IOperationCollection operations = new OperationCollection();
                operations.Append(operations_name);
                operations.Append(new SimpleOperation(OperationType.Async, asyncPosition));
                operations.Append(pushScopeBlock(scope, true));
                return operations;
            }
        }

        private IOperationCollection pushTerminatedInstruction_sync(Scope scope)
        {
            long syncPosition = _iterator.Position;

            // The syntax is 'sync(name) mode;'
            _iterator.Next();
            // We expect a '('
            if (!_iterator.Is(TokenType.BracketLeft))
                throw new SyntaxException(_iterator.Position, "Expected '(' after 'sync'.");
            _iterator.Next();
            // Read name
            IOperationCollection operations_name = pushAssign(scope);
            // We expect a ')'
            if (!_iterator.Is(TokenType.BracketRight))
                throw new SyntaxException(_iterator.Position, "Expected ')' after 'sync('.");
            _iterator.Next();

            // Read mode
            OperationType opType;
            switch (_iterator.Is(TokenType.Name) ? _iterator.GetValue<string>() : string.Empty)
            {
                case "end":
                    opType = OperationType.SyncEnd;
                    break;
                case "abort":
                    opType = OperationType.SyncAbort;
                    break;
                default:
                    throw new SyntaxException(_iterator.Position, "Expected 'end' or 'abort' after 'sync()'.");
            }
            _iterator.Next();
            if (!_iterator.Is(TokenType.EndOfInstruction))
                throw new SyntaxException(_iterator.Position, "Expected ';'");
            _iterator.Next();

            operations_name.Append(new SimpleOperation(opType, syncPosition));
            return operations_name;
        }

        private IOperationCollection pushVariableAssignment(Scope scope)
        {
            // We need something of the form "type variableName"
            if (_iterator.Is(TokenType.Name))
            {
                // At this point it is not entirely clear whether we are dealing
                // with a variable assignment. Therefore we have to remember everything
                // we read from now on so that we can restore it.
                _iterator.CreateRevertPoint();

                // Read the variable type
                long theTypePosition = _iterator.Position;
                string theTypePath;
                string theTypeContextPath;
                IType theTypeInstance;
                try
                {
                    theTypePath = string.Join("::", readNamespacePath(false).ToArray());
                    theTypeContextPath = Helper.PrependNamespace(theTypePath, _function.Signature.Namespace); // the current namespace might be implicit
                    if (_function.TypeTable.Has(theTypePath))
                        theTypeInstance = _function.TypeTable.Lookup(theTypePath);
                    else if (_function.TypeTable.Has(theTypeContextPath))
                        theTypeInstance = _function.TypeTable.Lookup(theTypeContextPath);
                    else
                        throw new SyntaxException(theTypePosition, "Unrecognized type: " + theTypePath);
                }
                catch (SyntaxException)
                {
                    // Something failed, so we are not dealing with a variable assignment.
                    // Proceed with the next layer.
                    _iterator.Revert();
                    return pushAssign(scope);
                }

                // We read the name of the (base) type, now it might be
                // that case that it is followed by arbitrarily many "[]"
                // which indicate an array declaration

                // Read "[]" tokens which identify the type as array type
                int listDimension = 0;
                while (_iterator.Is(TokenType.SquareBracketLeft))
                {
                    listDimension++;
                    _iterator.Next();
                    if (!_iterator.Is(TokenType.SquareBracketRight))
                    {
                        listDimension = -1;
                        break;
                    }
                    _iterator.Next();
                }

                // If reading "[]" did not fail and the next token is a name,
                // we finally know that it *is* a variable assignment
                if (listDimension >= 0 && _iterator.Is(TokenType.Name))
                {
                    // Phew. Now remember to destroy the revert point.
                    _iterator.Commit();

                    // Apply the array dimension to the type
                    for (int i = 0; i < listDimension; i++)
                        theTypeInstance = new ArrayType(theTypeInstance);

                    string theVariableName = _iterator.GetValue<string>();
                    _iterator.Next();
                    switch (_iterator.Current.Type)
                    {
                        case TokenType.EndOfInstruction:
                            var ops1 = new OperationCollection();
                            ops1.Append(new SimpleOperation(OperationType.LoadUndefined, theTypePosition));
                            ops1.Append(new TwoParametrizedOperation<IType, string>(OperationType.CreateVariable, theTypePosition, theTypeInstance, theVariableName));
                            return ops1;
                        case TokenType.OperatorAssign:
                            _iterator.Next();
                            var ops2 = new OperationCollection();
                            ops2.Append(pushAssign(scope));
                            ops2.Append(new TwoParametrizedOperation<IType, string>(OperationType.CreateVariable, theTypePosition, theTypeInstance, theVariableName));
                            return ops2;
                        default:
                            throw new SyntaxException(_iterator.Position, "Unexpected token: " + _iterator.Current.Type);
                    }
                }
                else
                {
                    // Otherwise restore everything from the revert point
                    // and proceed with the next layer
                    _iterator.Revert();
                    return pushAssign(scope);
                }
            }
            else
            {
                return pushAssign(scope);
            }
        }

        private IOperationCollection pushSingleInstruction_if(Scope scope)
        {
            long ifPosition = _iterator.Position;

            // We assume that the current token is the "if" keyword
            // Proceed to next token
            _iterator.Next();

            // 'if' must be followed by '('
            if (!_iterator.Is(TokenType.BracketLeft))
                throw new SyntaxException(_iterator.Position, "Expected '(' after 'if'.");
            _iterator.Next();

            // Read the condition
            IOperationCollection operations_condition = pushAssign(scope);

            // Condition must be followed by ')'
            if (!_iterator.Is(TokenType.BracketRight))
                throw new SyntaxException(_iterator.Position, "Expected ')' after 'if('.");
            _iterator.Next();

            // ')' must be followed by an instruction
            IOperationCollection operations_if = pushScopeBlock(scope, true);

            // Look for 'else'
            IOperationCollection operations_else;
            if (_iterator.Is(TokenType.Name) && _iterator.GetValue<string>() == "else")
            {
                _iterator.Next();
                operations_else = pushScopeBlock(scope, true);
            }
            else
            {
                operations_else = new OperationCollection();
                operations_else.Append(new SimpleOperation(OperationType.NoOperation, -1));
            }

            // Put everything together
            operations_condition.Append(new SimpleOperation(OperationType.If, ifPosition));
            operations_condition.Append(operations_if);
            operations_condition.Append(operations_else);
            return operations_condition;
        }

        private IOperationCollection pushSingleInstruction_for(Scope scope)
        {
            IOperationCollection operations_assignment;
            IOperationCollection operations_condition;
            IOperationCollection operations_init;
            long forPosition = _iterator.Position;

            // We assume that the current token is the "for" keyword
            // Proceed to next token
            _iterator.Next();

            // 'for' must be followed by '('
            if (!_iterator.Is(TokenType.BracketLeft))
                throw new SyntaxException(_iterator.Position, "Expected '(' after 'for'.");
            _iterator.Next();

            {
                // Read the initialization part
                // In contrast to the other parts, this also allows creation of new variables,
                // so we use pushTerminatedInstruction() here. Note that the latter also
                // reads the terminating ';' so that we don't have to do this by our own.
                if (_iterator.Is(TokenType.EndOfInstruction))
                {
                    operations_init = new OperationCollection();
                    _iterator.Next();
                }
                else
                {
                    operations_init = pushTerminatedInstruction(scope);
                }
            }
            {
                // Read the condition part
                if (_iterator.Is(TokenType.EndOfInstruction))
                    operations_condition = new OperationCollection();
                else
                    operations_condition = pushVariableAssignment(scope);

                // Condition must be followed by ';'
                if (!_iterator.Is(TokenType.EndOfInstruction))
                    throw new SyntaxException(_iterator.Position, "Expected ';' in 'for('.");
                _iterator.Next();
            }
            {
                // Read the condition part
                if (_iterator.Is(TokenType.BracketRight))
                    operations_assignment = new OperationCollection();
                else
                    operations_assignment = pushVariableAssignment(scope);

                // 'for' must be ended by ')'
                if (!_iterator.Is(TokenType.BracketRight))
                    throw new SyntaxException(_iterator.Position, "Expected ')' after 'for('.");
                _iterator.Next();
            }

            // ')' must be followed by an instruction
            IOperationCollection operations_body = pushScopeBlock(scope, true);

            // Put everything together
            IOperationCollection operations = new OperationCollection();
            operations.Append(new SimpleOperation(OperationType.For, forPosition));
            operations.Append(new SimpleOperation(OperationType.BeginScope, -1));
            operations.Append(operations_init);
            operations.Append(new SimpleOperation(OperationType.EndScope, -1));
            operations.Append(new SimpleOperation(OperationType.BeginOpBlock, -1));
            operations.Append(operations_condition);
            operations.Append(new SimpleOperation(OperationType.EndOpBlock, -1));
            operations.Append(new SimpleOperation(OperationType.BeginOpBlock, -1));
            operations.Append(operations_assignment);
            operations.Append(new SimpleOperation(OperationType.EndOpBlock, -1));
            operations.Append(operations_body);
            return operations;
        }

        private IOperationCollection pushSingleInstruction_while(Scope scope)
        {
            IOperationCollection operations_condition;
            long whilePosition = _iterator.Position;

            // We assume that the current token is the "while" keyword
            // Proceed to next token
            _iterator.Next();

            // 'while' must be followed by '('
            if (!_iterator.Is(TokenType.BracketLeft))
                throw new SyntaxException(_iterator.Position, "Expected '(' after 'while'.");
            _iterator.Next();

            // Read the condition part
            if (_iterator.Is(TokenType.EndOfInstruction))
                operations_condition = new OperationCollection();
            else
                operations_condition = pushVariableAssignment(scope);

            // Condition must be followed by ')'
            if (!_iterator.Is(TokenType.BracketRight))
                throw new SyntaxException(_iterator.Position, "Expected ')' after 'while('.");
            _iterator.Next();

            // ')' must be followed by an instruction
            IOperationCollection operations_body = pushScopeBlock(scope, true);

            // Put everything together
            IOperationCollection operations = new OperationCollection();
            operations.Append(new SimpleOperation(OperationType.While, whilePosition));
            operations.Append(new SimpleOperation(OperationType.BeginOpBlock, -1));
            operations.Append(operations_condition);
            operations.Append(new SimpleOperation(OperationType.EndOpBlock, -1));
            operations.Append(operations_body);
            return operations;
        }

        private IOperationCollection pushSingleInstruction_do_repeat(Scope scope, bool trueForDo_falseForRepeat)
        {
            IOperationCollection operations_condition;
            long whilePosition = _iterator.Position;

            // We assume that the current token is the "do" keyword
            // Proceed to next token
            _iterator.Next();

            // 'do' must be followed by an instruction
            IOperationCollection operations_body = pushScopeBlock(scope, true);

            string matchingKeyword = trueForDo_falseForRepeat ? "while" : "until";

            // We expect the 'while' resp. 'until' keyword ...
            if (!_iterator.Is(TokenType.Name) || _iterator.GetValue<string>() != matchingKeyword)
                throw new SyntaxException(_iterator.Position, "Expected '" + matchingKeyword + "' statement.");
            _iterator.Next();

            // ... which must be followed by '('
            if (!_iterator.Is(TokenType.BracketLeft))
                throw new SyntaxException(_iterator.Position, "Expected '(' after '" + matchingKeyword + "'.");
            _iterator.Next();

            // Read the condition part
            long conditionPosition = _iterator.Position;
            if (_iterator.Is(TokenType.EndOfInstruction))
                operations_condition = new OperationCollection();
            else
                operations_condition = pushVariableAssignment(scope);

            // repeat..until is the same as do..while, but the condition is negated
            if (!trueForDo_falseForRepeat)
                operations_condition.Append(new SimpleOperation(OperationType.LogicalNot, conditionPosition));

            // Condition must be followed by ');'
            if (!_iterator.Is(TokenType.BracketRight))
                throw new SyntaxException(_iterator.Position, "Expected ')' after '" + matchingKeyword + "('.");
            _iterator.Next();
            if (!_iterator.Is(TokenType.EndOfInstruction))
                throw new SyntaxException(_iterator.Position, "Expected ';' after '" + matchingKeyword + "()'.");
            _iterator.Next();

            // Put everything together
            IOperationCollection operations = new OperationCollection();
            operations.Append(new SimpleOperation(OperationType.DoWhile, whilePosition));
            operations.Append(operations_body);
            operations.Append(new SimpleOperation(OperationType.BeginOpBlock, -1));
            operations.Append(operations_condition);
            operations.Append(new SimpleOperation(OperationType.EndOpBlock, -1));
            return operations;
        }

        private IOperationCollection pushCommaSeparated(Scope scope, out int count)
        {
            return pushCommaSeparated(scope, pushAssign, out count);
        }

        private IOperationCollection pushCommaSeparated(Scope scope, Func<Scope, IOperationCollection> customCallback, out int count)
        {
            // Note: this method assumes that there is AT LEAST ONE argument
            // to be parsed. You have to make sure BEFORE calling this method
            // that there is such a value

            IOperationCollection operations = new OperationCollection();

            count = 0;
            do
            {
                operations.Append(customCallback(scope));
                count++;

                if (!_iterator.Is(TokenType.Comma))
                    return operations;
                _iterator.Next();
            }
            while (true);
        }

        private IOperationCollection pushAssign(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorAssign, this.o(OperationType.Assign));
            operationConverters.Add(TokenType.OperatorAssignBitwiseAnd, o(OperationType.AssignBitwiseAnd));
            operationConverters.Add(TokenType.OperatorAssignBitwiseOr, o(OperationType.AssignBitwiseOr));
            operationConverters.Add(TokenType.OperatorAssignBitwiseXor, o(OperationType.AssignBitwiseXor));
            operationConverters.Add(TokenType.OperatorAssignLeftShift, o(OperationType.AssignLeftShift));
            operationConverters.Add(TokenType.OperatorAssignRightShift, o(OperationType.AssignRightShift));
            operationConverters.Add(TokenType.OperatorAssignDivide, o(OperationType.AssignDivide));
            operationConverters.Add(TokenType.OperatorAssignMinus, o(OperationType.AssignMinus));
            operationConverters.Add(TokenType.OperatorAssignModulo, o(OperationType.AssignModulo));
            operationConverters.Add(TokenType.OperatorAssignPlus, o(OperationType.AssignPlus));
            operationConverters.Add(TokenType.OperatorAssignTimes, o(OperationType.AssignTimes));

            return pushGenericBinaryRightToLeftOperation(scope, pushLogicalOr, operationConverters);
        }

        private IOperationCollection pushLogicalOr(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorLogicalOr, o(OperationType.LogicalOr));

            return pushGenericBinaryLeftToRightOperation(scope, pushLogicalAnd, operationConverters);
        }

        private IOperationCollection pushLogicalAnd(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorLogicalAnd, o(OperationType.LogicalAnd));

            return pushGenericBinaryLeftToRightOperation(scope, pushBitwiseOr, operationConverters);
        }

        private IOperationCollection pushBitwiseOr(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorBitwiseOr, o(OperationType.BitwiseOr));

            return pushGenericBinaryLeftToRightOperation(scope, pushBitwiseXor, operationConverters);
        }

        private IOperationCollection pushBitwiseXor(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorBitwiseXor, o(OperationType.BitwiseXor));

            return pushGenericBinaryLeftToRightOperation(scope, pushBitwiseAnd, operationConverters);
        }

        private IOperationCollection pushBitwiseAnd(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorBitwiseAnd, o(OperationType.BitwiseAnd));

            return pushGenericBinaryLeftToRightOperation(scope, pushEqual, operationConverters);
        }

        private IOperationCollection pushEqual(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorEqual, o(OperationType.Equal));
            operationConverters.Add(TokenType.OperatorNotEqual, o(OperationType.NotEqual));

            return pushGenericBinaryLeftToRightOperation(scope, pushCompare, operationConverters);
        }

        private IOperationCollection pushCompare(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorLessThan, o(OperationType.LessThan));
            operationConverters.Add(TokenType.OperatorLessThanEqual, o(OperationType.LessThanEqual));
            operationConverters.Add(TokenType.OperatorMoreThan, o(OperationType.MoreThan));
            operationConverters.Add(TokenType.OperatorMoreThanEqual, o(OperationType.MoreThanEqual));

            return pushGenericBinaryLeftToRightOperation(scope, pushShift, operationConverters);
        }

        private IOperationCollection pushShift(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorLeftShift, o(OperationType.LeftShift));
            operationConverters.Add(TokenType.OperatorRightShift, o(OperationType.RightShift));

            return pushGenericBinaryLeftToRightOperation(scope, pushSum, operationConverters);
        }

        private IOperationCollection pushSum(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorPlus, o(OperationType.Plus));
            operationConverters.Add(TokenType.OperatorMinus, o(OperationType.Minus));

            return pushGenericBinaryLeftToRightOperation(scope, pushProduct, operationConverters);
        }

        private IOperationCollection pushProduct(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorTimes, o(OperationType.Times));
            operationConverters.Add(TokenType.OperatorDivide, o(OperationType.Divide));
            operationConverters.Add(TokenType.OperatorModulo, o(OperationType.Modulo));

            return pushGenericBinaryLeftToRightOperation(scope, pushUnary, operationConverters);
        }

        private IOperationCollection pushUnary(Scope scope)
        {
            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorLogicalNot, o(OperationType.LogicalNot));
            operationConverters.Add(TokenType.OperatorBitwiseNot, o(OperationType.BitwiseNot));
            operationConverters.Add(TokenType.OperatorMinus, o(OperationType.UnaryMinus));
            operationConverters.Add(TokenType.OperatorPlus, o(OperationType.UnaryPlus));
            operationConverters.Add(TokenType.OperatorIncrement, o(OperationType.PrefixIncrement));
            operationConverters.Add(TokenType.OperatorDecrement, o(OperationType.PrefixDecrement));

            return pushGenericUnaryRightToLeftOperation(scope, pushSuffixIncDec, operationConverters);
        }

        private IOperationCollection pushSuffixIncDec(Scope scope)
        {
            Func<long, bool, IOperationCollection> functionCallHandler = (bracketPosition, isImplicit) =>
            {
                IOperationCollection operations = new OperationCollection();
                int argCount = 0;

                _iterator.Next();
                if (!_iterator.Is(TokenType.BracketRight))
                {
                    // Push the loading instructions of the arguments after the 
                    // ones of the function itself
                    operations.Append(pushCommaSeparated(scope, out argCount));
                }

                if (isImplicit) argCount += 1;

                // Push instruction to call the function
                operations.Append(new ParametrizedOperation<int>(OperationType.Call, bracketPosition, argCount));

                // There should be a right bracket now
                if (!_iterator.Is(TokenType.BracketRight))
                    throw new SyntaxException(_iterator.Position, "Expected ')'");

                // Do not call _iterator.Next() here! This is done in the genericOperation method
                return operations;
            };

            var operationConverters = new Dictionary<TokenType, Func<long, IOperationCollection>>();
            operationConverters.Add(TokenType.OperatorIncrement, o(OperationType.SuffixIncrement));
            operationConverters.Add(TokenType.OperatorDecrement, o(OperationType.SuffixDecrement));
            operationConverters.Add(TokenType.Period, (periodPosition) =>
            {
                _iterator.Next();
                long posFirstNamespace = _iterator.Current.Position;
                List<string> propertyName = new List<string>();

                if (!_iterator.Is(TokenType.Name))
                    throw new SyntaxException(_iterator.Position, "Expected name after '.' operator.");
                propertyName.Add(_iterator.GetValue<string>());
                _iterator.CreateRevertPoint();
                _iterator.Next();

                while (_iterator.Is(TokenType.DoubleColon))
                {
                    _iterator.Next();
                    if (!_iterator.Is(TokenType.Name))
                        throw new SyntaxException(_iterator.Position, "Expected name after '::'.");
                    propertyName.Add(_iterator.GetValue<string>());
                    _iterator.Commit();
                    _iterator.CreateRevertPoint();
                    _iterator.Next();
                }

                // Check if the 'property access' is actually an implicit function call.
                if (_iterator.Is(TokenType.BracketLeft))
                {
                    // Discard the restore point
                    _iterator.Commit();

                    // Handle it as a function
                    var operations = new OperationCollection();
                    operations.Append(new ParametrizedOperation<string[]>(OperationType.LoadFunction, posFirstNamespace, propertyName.ToArray()));
                    operations.Append(new SimpleOperation(OperationType.Swap, -1)); // first argument and function are on the stack in the wrong order
                    operations.Append(functionCallHandler(_iterator.Current.Position, true));
                    return operations;
                }
                else
                {
                    // Note that genericOperation() already calls _iterator.Next(), that's why
                    // the iterator must point to the _current_ instruction, NOT to the next one.
                    // We used revert points to store the state of the iterator.
                    _iterator.Revert();

                    // Since it is really a property now, we do not allow namespacing anymore
                    if (propertyName.Count != 1)
                        throw new SyntaxException(posFirstNamespace, "Namespaces are not allowed in property names");

                    return new SingleOperation(new ParametrizedOperation<string>(OperationType.PropertyAccess, periodPosition, propertyName.First()));
                }
            });
            operationConverters.Add(TokenType.BracketLeft, (x) => functionCallHandler(x, false));
            operationConverters.Add(TokenType.SquareBracketLeft, (sqBrPosition) => // indexing
            {
                _iterator.Next();
                IOperationCollection operations = pushAssign(scope);
                operations.Append(new SimpleOperation(OperationType.Index, sqBrPosition));

                // There should be a right bracket now
                if (!_iterator.Is(TokenType.SquareBracketRight))
                    throw new SyntaxException(_iterator.Position, "Expected ']'");

                // Do not call _iterator.Next() here! This is done in the genericOperation method
                return operations;
            });

            return pushGenericUnaryLeftToRightOperation(scope, pushObject, operationConverters);
        }

        private IOperationCollection pushObject(Scope scope)
        {
            IOperationCollection operations = new OperationCollection();

            switch (_iterator.Current.Type)
            {
                case TokenType.EndOfDocument:
                    throw new SyntaxException(_iterator.Position, "Unexpected end of document");
                case TokenType.EndOfInstruction:
                    throw new SyntaxException(_iterator.Position, "Unexpected instruction delimiter");
                default:
                    throw new SyntaxException(_iterator.Position, "Unexpected token: " + _iterator.Current.Type);
                case TokenType.DecInt:
                case TokenType.HexInt:
                case TokenType.BinInt:
                    operations.Append(new ParametrizedOperation<RawInt>(OperationType.LoadConstant, _iterator.Position, _iterator.GetValue<RawInt>()));
                    _iterator.Next();
                    break;
                case TokenType.Float:
                    operations.Append(new ParametrizedOperation<RawFloat>(OperationType.LoadConstant, _iterator.Position, _iterator.GetValue<RawFloat>()));
                    _iterator.Next();
                    break;
                case TokenType.String:
                    operations.Append(new ParametrizedOperation<string>(OperationType.LoadConstant, _iterator.Position, _iterator.GetValue<string>()));
                    _iterator.Next();
                    break;
                case TokenType.Name:
                case TokenType.ReservedName:
                    long posFirstNamespace = _iterator.Position;
                    bool isReserved = _iterator.Is(TokenType.ReservedName);
                    // Read keyword, possibly with namespace prefix
                    var keyword = readNamespacePath(true);

                    // Check if we are dealing with a function
                    if (_iterator.Is(TokenType.BracketLeft))
                    {
                        operations.Append(new ParametrizedOperation<string[]>(OperationType.LoadFunction, posFirstNamespace, keyword.ToArray()));
                        break;
                    }

                    // In all other cases namespaces make no sense and are, hence, forbidden
                    // Moreover, reserved names are forbidden
                    if (keyword.Count != 1)
                        throw new SyntaxException(posFirstNamespace, "Unexpected namespace");
                    if (isReserved)
                        throw new SyntaxException(posFirstNamespace, "Illegal variable name: '$' characters not allowed");

                    // Otherwise we are dealing with a special value or with a variable
                    switch (keyword[0])
                    {
                        case "true":
                            operations.Append(new ParametrizedOperation<bool>(OperationType.LoadConstant, posFirstNamespace, true));
                            break;
                        case "false":
                            operations.Append(new ParametrizedOperation<bool>(OperationType.LoadConstant, posFirstNamespace, false));
                            break;
                        default:
                            operations.Append(new ParametrizedOperation<string>(OperationType.LoadSymbol, posFirstNamespace, keyword[0]));
                            break;
                    }
                    break;
                case TokenType.SquareBracketLeft:
                    long sqBrPosition = _iterator.Position;
                    _iterator.Next();
                    int listLength;
                    if (_iterator.Is(TokenType.SquareBracketRight))
                        listLength = 0;
                    else
                        operations.Append(pushCommaSeparated(scope, out listLength));
                    operations.Append(new ParametrizedOperation<int>(OperationType.MakeList, sqBrPosition, listLength));
                    if (!_iterator.Is(TokenType.SquareBracketRight))
                        throw new SyntaxException(_iterator.Position, "Expected ']'");
                    _iterator.Next();
                    break;
                case TokenType.OperatorLessThan:
                    long vecPosition = _iterator.Position;
                    _iterator.Next();
                    int vectorDim;
                    operations.Append(pushCommaSeparated(scope, pushObject, out vectorDim));
                    operations.Append(new ParametrizedOperation<int>(OperationType.MakeVector, vecPosition, vectorDim));
                    if (!_iterator.Is(TokenType.OperatorMoreThan))
                        throw new SyntaxException(_iterator.Position, "Expected '>'. Expressions in vector definitions have to be bracketed.");
                    _iterator.Next();
                    break;
                case TokenType.BracketLeft:
                    _iterator.Next();
                    operations.Append(pushAssign(scope));
                    if (!_iterator.Is(TokenType.BracketRight))
                        throw new SyntaxException(_iterator.Position, "Expected ')'");
                    _iterator.Next();
                    break;
            }

            return operations;
        }

        /// <summary>
        /// Reads a namespace path (tokens separated by '::') from the tokenizer.
        /// </summary>
        /// <returns>a list containing the several parts of the path.</returns>
        private List<string> readNamespacePath(bool allowReserved)
        {
            List<string> ns = new List<string>();
            var allowedTokens = allowReserved ? new TokenType[] { TokenType.Name, TokenType.ReservedName } : new TokenType[] { TokenType.Name };
            bool previousWasReserved = _iterator.Is(TokenType.ReservedName);
            long previousPosition = _iterator.Position;

            // Read namespace name
            if (!_iterator.Is(allowedTokens))
                throw new SyntaxException(_iterator.Position, "Expected namespace name");
            ns.Add(_iterator.GetValue<string>());
            _iterator.Next();

            // And potential parent namespaces
            while (_iterator.Is(TokenType.DoubleColon))
            {
                _iterator.Next();

                // When we read a token, we do not know a priori whether it is a namespace
                // or the final object. We assume it to be the latter until we read a further token.
                // That way, we can only verify the previous token in the next iteration.
                if (previousWasReserved)
                    throw new SyntaxException(previousPosition, "Illegal namespace name: '$' characters not allowed");
                previousPosition = _iterator.Position;
                previousWasReserved = _iterator.Is(TokenType.ReservedName);

                if (!_iterator.Is(allowedTokens))
                    throw new SyntaxException(_iterator.Position, "Expected namespace name");
                ns.Add(_iterator.GetValue<string>());
                _iterator.Next();
            }

            return ns;
        }

    }
}