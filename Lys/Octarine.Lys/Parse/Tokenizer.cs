/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System.Collections.Generic;
using System.Text;

namespace Octarine.Lys.Parse
{
    /// <summary>
    /// Interface for reading script language tokens from a source.
    /// </summary>
    public class Tokenizer : ITokenizer
    {
        /// <summary>
        /// Initializes a new tokenizer.
        /// </summary>
        /// <param name="source">The data source which should be parsed.</param>
        public Tokenizer(ICharReader source)
        {
            _source = source;
            _stack = new Stack<Token>();
        }

        private ICharReader _source;
        private Stack<Token> _stack;

        public const int MIN_FLOAT_BITLENGTH = 8; // Floats only exist in 8-bit steps
        public const int MAX_FLOAT_BITLENGTH = 256;
        public const int MIN_INT_BITLENGTH = 1; // Integers exist in 1-bit steps
        public const int MAX_INT_BITLENGTH = 1024;

        private enum CommentMode { None, Line, Block }

        /// <summary>
        /// Pushes back a token.
        /// </summary>
        public void PushBack(Token token)
        {
            _stack.Push(token);
        }

        /// <summary>
        /// Reads the next token from the source.
        /// </summary>
        public Token Read()
        {
            if (_stack.Count == 0)
                return ReadNew();
            else
                return _stack.Pop();
        }

        private Token ReadNew()
        {
            CommentMode comment = CommentMode.None;
            int readInt;
            Token? tmpToken;
            while ((readInt = _source.Read()) >= 0)
            {
                // Handle comment mode
                if (comment == CommentMode.Line)
                {
                    if (readInt == '\n' || readInt == '\r')
                        comment = CommentMode.None;
                    continue;
                }
                else if (comment == CommentMode.Block)
                {
                    if (readInt == '*')
                    {
                        if ((readInt = _source.Read()) == '/')
                            comment = CommentMode.None;
                        else if (readInt >= 0)
                            _source.PushBack(readInt);
                    }
                    continue;
                }

                // Skip whitespaces
                if (char.IsWhiteSpace((char)readInt)) continue;

                long position = _source.Position;
                switch (readInt)
                {
                    case '+':
                        switch (readInt = _source.Read())
                        {
                            case '+': return new Token(TokenType.OperatorIncrement, position);
                            case '=': return new Token(TokenType.OperatorAssignPlus, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorPlus, position);
                        }
                    case '-':
                        switch (readInt = _source.Read())
                        {
                            case '-': return new Token(TokenType.OperatorDecrement, position);
                            case '=': return new Token(TokenType.OperatorAssignMinus, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorMinus, position);
                        }
                    case '*':
                        switch (readInt = _source.Read())
                        {
                            case '=': return new Token(TokenType.OperatorAssignTimes, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorTimes, position);
                        }
                    case '/':
                        switch (readInt = _source.Read())
                        {
                            case '=': return new Token(TokenType.OperatorAssignDivide, position);
                            case '/':
                                comment = CommentMode.Line;
                                break;
                            case '*':
                                comment = CommentMode.Block;
                                break;
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorDivide, position);
                        }
                        break;
                    case '%':
                        switch (readInt = _source.Read())
                        {
                            case '=': return new Token(TokenType.OperatorAssignModulo, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorModulo, position);
                        }
                    case '(': return new Token(TokenType.BracketLeft, position);
                    case ')': return new Token(TokenType.BracketRight, position);
                    case '[': return new Token(TokenType.SquareBracketLeft, position);
                    case ']': return new Token(TokenType.SquareBracketRight, position);
                    case '{': return new Token(TokenType.CurlyBracketLeft, position);
                    case '}': return new Token(TokenType.CurlyBracketRight, position);
                    case ';': return new Token(TokenType.EndOfInstruction, position);
                    case ',': return new Token(TokenType.Comma, position);
                    case '.':
                        // It might be a floating point number (in fact, iff it is follows by a digit)
                        int nextInt = _source.Read();
                        if (nextInt >= 0)
                        {
                            _source.PushBack(nextInt);
                            if (char.IsDigit((char)nextInt))
                                // Since the next character is a digit, ProcessNumber_Float() cannot return null
                                return ProcessNumber_Float(position, new StringBuilder(), readInt) ?? throw new SyntaxException(_source.Position, "Failed to parse floating point number with leading dot.");
                        }
                        return new Token(TokenType.Period, position);
                    case ':':
                        switch (readInt = _source.Read())
                        {
                            case ':': return new Token(TokenType.DoubleColon, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.Colon, position);
                        }
                    case '<':
                        switch (readInt = _source.Read())
                        {
                            case '<':
                                if ((readInt = _source.Read()) == '=')
                                    return new Token(TokenType.OperatorAssignLeftShift, position);
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorLeftShift, position);
                            case '=': return new Token(TokenType.OperatorLessThanEqual, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorLessThan, position);
                        }
                    case '>':
                        switch (readInt = _source.Read())
                        {
                            case '>':
                                if ((readInt = _source.Read()) == '=')
                                    return new Token(TokenType.OperatorAssignRightShift, position);
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorRightShift, position);
                            case '=': return new Token(TokenType.OperatorMoreThanEqual, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorMoreThan, position);
                        }
                    case '!':
                        switch (readInt = _source.Read())
                        {
                            case '=': return new Token(TokenType.OperatorNotEqual, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorLogicalNot, position);
                        }
                    case '~': return new Token(TokenType.OperatorBitwiseNot, position);
                    case '&':
                        switch (readInt = _source.Read())
                        {
                            case '&': return new Token(TokenType.OperatorLogicalAnd, position);
                            case '=': return new Token(TokenType.OperatorAssignBitwiseAnd, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorBitwiseAnd, position);
                        }
                    case '|':
                        switch (readInt = _source.Read())
                        {
                            case '|': return new Token(TokenType.OperatorLogicalOr, position);
                            case '=': return new Token(TokenType.OperatorAssignBitwiseOr, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorBitwiseOr, position);
                        }
                    case '^':
                        switch (readInt = _source.Read())
                        {
                            case '=': return new Token(TokenType.OperatorAssignBitwiseXor, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorBitwiseXor, position);
                        }
                    case '=':
                        switch (readInt = _source.Read())
                        {
                            case '=': return new Token(TokenType.OperatorEqual, position);
                            default:
                                if (readInt >= 0) _source.PushBack(readInt);
                                return new Token(TokenType.OperatorAssign, position);
                        }
                    case '\'':
                        switch (readInt = _source.Read())
                        {
                            case '\'':
                                throw new SyntaxException(_source.Position, "Empty character");
                            case '\\':
                                switch (readInt = _source.Read())
                                {
                                    case '0': readInt = 0; break;
                                    case '\'': break;
                                    case '\\': break;
                                    case 'n': readInt = '\n'; break;
                                    case 'r': readInt = '\r'; break;
                                    case 't': readInt = '\t'; break;
                                    default:
                                        throw new SyntaxException(_source.Position, "Unrecognized escape sequence");
                                }
                                break;
                        }
                        if (_source.Read() != '\'')
                            throw new SyntaxException(_source.Position, "Too many characters");
                        return new ValuedToken<string>(TokenType.DecInt, position, readInt.ToString());
                    default:
                        if ((tmpToken = ProcessNumber(readInt)) != null) return tmpToken;
                        if ((tmpToken = ProcessName(readInt)) != null) return tmpToken;
                        if ((tmpToken = ProcessString(readInt)) != null) return tmpToken;
                        throw new SyntaxException(_source.Position, "Unrecognized character: " + (char)readInt);
                }
            }

            // We reached the end
            return new Token(TokenType.EndOfDocument, _source.Position + 1);
        }

        private Token? ProcessNumber(int readInt)
        {
            long position = _source.Position;
            StringBuilder number = new StringBuilder();
            bool isFirstRun = true; // trouble with do-while is that we cannot push back the first `readInt`
            do
            {
                if (readInt >= '0' && readInt <= '9')
                {
                    number.Append((char)readInt);
                }
                else if (readInt == '.' || (number.Length > 0 && (readInt == 'e' || readInt == 'E')))
                {
                    // We are (probably) dealing with a float, unless '.' stands for the property access operator.
                    // In this case, ProcessNumber_Float() returns null.
                    var floatToken = ProcessNumber_Float(position, number, readInt);
                    if (!object.ReferenceEquals(null, floatToken))
                    {
                        return floatToken;
                    }
                    else
                    {
                        // '.' is not part of the token, so push it back
                        _source.PushBack(readInt);
                        break;
                    }
                }
                else if ((readInt == 'x' || readInt == 'X') && number.Length == 1 && number.ToString() == "0")
                {
                    return ProcessNumber_Hex(position);
                }
                else if ((readInt == 'b' || readInt == 'B') && number.Length == 1 && number.ToString() == "0")
                {
                    if (number.Length == 1 && number.ToString() == "0")
                        return ProcessNumber_Bin(position);
                }
                else
                {
                    // We read a character which does not belong to the number
                    // Put that back and return the token
                    if (!isFirstRun) _source.PushBack(readInt);
                    break;
                }
                isFirstRun = false;
            }
            while ((readInt = _source.Read()) >= 0);

            if (number.Length == 0)
            {
                return null;
            }

            // Read optional 'unsigned' indication
            bool unsignedPart = false;
            switch (readInt = _source.Read())
            {
                case 'u':
                case 'U':
                    unsignedPart = true;
                    break;
                default:
                    // We read a character which does not belong to the number; put it back
                    if (readInt >= 0) _source.PushBack(readInt);
                    break;
            }

            // Read optional bit indication
            int bitPart = 0;
            if ((readInt = _source.Read()) == '@')
            {
                long positionOfAtSymbol = _source.Position;
                while ((readInt = _source.Read()) >= 0 && char.IsDigit((char)readInt))
                {
                    bitPart = bitPart * 10 + (readInt - '0');
                }
                if (_source.Position == positionOfAtSymbol + 1)
                    throw new SyntaxException(positionOfAtSymbol, "Expected bit length");

                // Add an artificial upper bound
                if (bitPart < MIN_INT_BITLENGTH || bitPart > MAX_INT_BITLENGTH)
                    throw new SyntaxException(positionOfAtSymbol, "Bit length " + bitPart + " is not supported for integers.");
            }
            // We read a character which does not belong to the number; put it back
            if (readInt >= 0) _source.PushBack(readInt);

            RawInt rawInt;
            rawInt.Integer = number.ToString();
            rawInt.Base = RawIntBase.Decimal;
            rawInt.Bits = bitPart;
            rawInt.Unsigned = unsignedPart;
            return new ValuedToken<RawInt>(TokenType.DecInt, position, rawInt);
        }

        /// <returns>null if the number is not a float, a token representing the float otherwise.</returns>
        private Token? ProcessNumber_Float(long position, StringBuilder integerPart, int readInt)
        {
            StringBuilder fractionalPart = new StringBuilder();
            StringBuilder exponentialPart = new StringBuilder();
            int bitPart = 0;

            bool isReadingFraction;
            if (readInt == '.')
                isReadingFraction = true;
            else if (readInt == 'e' || readInt == 'E')
                isReadingFraction = false;
            else
                throw new System.ArgumentException("Expected '.' or 'E' as previously read character.", "readInt");

            // Dealing with '.' is troublesome, as it can be used in floating
            // point numbers AND as property access operator.
            // We assume '.' stands for the latter if it is followed by (possibly spaces followed by) a letter,
            // and for the former in any other case.
            if (readInt == '.')
            {
                // Read white spaces ... we do not care about restoring them (pushing them back)
                // since they are ignored anyway in the course of the tokenizing process.
                while ((readInt = _source.Read()) >= 0 && char.IsWhiteSpace((char)readInt)) ;

                // Check if there is a character which can be at the beginning of a `Name` token, see this.ProcessName()
                if (char.IsLetter((char)readInt) || readInt == '_' || readInt == '$')
                    // The '.' character was an operator
                    return null;
                else if (readInt >= 0)
                    _source.PushBack(readInt);
            }

            while ((readInt = _source.Read()) >= 0)
            {
                if (readInt >= '0' && readInt <= '9')
                {
                    if (isReadingFraction)
                        fractionalPart.Append((char)readInt);
                    else
                        exponentialPart.Append((char)readInt);
                }
                else if ((readInt == 'e' || readInt == 'E') && isReadingFraction)
                {
                    isReadingFraction = false;
                }
                else if (readInt == '-' && !isReadingFraction && exponentialPart.Length == 0)
                {
                    exponentialPart.Append('-');
                }
                else
                {
                    // We read a character which does not belong to the number; put it back
                    _source.PushBack(readInt);
                    break;
                }
            }

            // Read optional bit indication
            if ((readInt = _source.Read()) == '@')
            {
                long positionOfAtSymbol = _source.Position;
                while ((readInt = _source.Read()) >= 0 && char.IsDigit((char)readInt))
                {
                    bitPart = bitPart * 10 + (readInt - '0');
                }
                if (_source.Position == positionOfAtSymbol + 1)
                    throw new SyntaxException(positionOfAtSymbol, "Expected bit length");

                // Add an artificial upper bound
                if (bitPart < MIN_FLOAT_BITLENGTH || bitPart > MAX_FLOAT_BITLENGTH || bitPart % 8 != 0)
                    throw new SyntaxException(positionOfAtSymbol, "Bit length " + bitPart + " is not supported for floats.");
            }
            // We read a character which does not belong to the number; put it back
            if (readInt >= 0) _source.PushBack(readInt);

            // Check if number is valid
            if (fractionalPart.Length == 0 && integerPart.Length == 0)
                throw new SyntaxException(position, "Invalid float: integer or fractional part is required");
            if (!isReadingFraction && exponentialPart.Length == 0)
                throw new SyntaxException(position, "Invalid float: expected exponent after 'E'");

            RawFloat rawFloat;
            rawFloat.IntegerPart = integerPart.ToString();
            rawFloat.FractionalPart = fractionalPart.ToString();
            rawFloat.ExponentialPart = exponentialPart.ToString();
            rawFloat.Bits = bitPart;
            return new ValuedToken<RawFloat>(TokenType.Float, position, rawFloat);
        }

        private Token ProcessNumber_Hex(long position)
        {
            StringBuilder number = new StringBuilder();
            int readInt;
            while ((readInt = _source.Read()) >= 0)
            {
                if ((readInt >= '0' && readInt <= '9') || (readInt >= 'A' && readInt <= 'F'))
                {
                    number.Append((char)readInt);
                }
                else if (readInt >= 'a' && readInt <= 'f')
                {
                    number.Append((char)(readInt - 'a' + 'A'));
                }
                else
                {
                    // We read a character which does not belong to the number
                    // Put that back and return the token
                    _source.PushBack(readInt);
                    break;
                }
            }
            if (number.Length == 0)
                throw new SyntaxException(position, "Invalid hex number: expected digits after '0x'.");

            // Read optional bit indication
            int bitPart = 0;
            if ((readInt = _source.Read()) == '@')
            {
                long positionOfAtSymbol = _source.Position;
                while ((readInt = _source.Read()) >= 0 && char.IsDigit((char)readInt))
                {
                    bitPart = bitPart * 10 + (readInt - '0');
                }
                if (_source.Position == positionOfAtSymbol + 1)
                    throw new SyntaxException(positionOfAtSymbol, "Expected bit length");

                // Add an artificial upper bound
                if (bitPart < MIN_INT_BITLENGTH || bitPart > MAX_INT_BITLENGTH)
                    throw new SyntaxException(positionOfAtSymbol, "Bit length " + bitPart + " is not supported for integers.");
            }
            // We read a character which does not belong to the number; put it back
            if (readInt >= 0) _source.PushBack(readInt);

            RawInt rawInt;
            rawInt.Integer = number.ToString();
            rawInt.Base = RawIntBase.Hexadecimal;
            rawInt.Bits = bitPart;
            rawInt.Unsigned = true;
            return new ValuedToken<RawInt>(TokenType.HexInt, position, rawInt);
        }

        private Token ProcessNumber_Bin(long position)
        {
            StringBuilder number = new StringBuilder();
            int readInt;
            while ((readInt = _source.Read()) >= 0)
            {
                if (readInt == '0' || readInt == '1')
                {
                    number.Append((char)readInt);
                }
                else
                {
                    // We read a character which does not belong to the number
                    // Put that back and return the token
                    _source.PushBack(readInt);
                    break;
                }
            }
            if (number.Length == 0)
                throw new SyntaxException(position, "Invalid hex number: expected digits after '0b'.");

            // Read optional bit indication
            int bitPart = 0;
            if ((readInt = _source.Read()) == '@')
            {
                long positionOfAtSymbol = _source.Position;
                while ((readInt = _source.Read()) >= 0 && char.IsDigit((char)readInt))
                {
                    bitPart = bitPart * 10 + (readInt - '0');
                }
                if (_source.Position == positionOfAtSymbol + 1)
                    throw new SyntaxException(positionOfAtSymbol, "Expected bit length");

                // Add an artificial upper bound
                if (bitPart < MIN_INT_BITLENGTH || bitPart > MAX_INT_BITLENGTH)
                    throw new SyntaxException(positionOfAtSymbol, "Bit length " + bitPart + " is not supported for integers.");
            }
            // We read a character which does not belong to the number; put it back
            if (readInt >= 0) _source.PushBack(readInt);

            RawInt rawInt;
            rawInt.Integer = number.ToString();
            rawInt.Base = RawIntBase.Binary;
            rawInt.Bits = bitPart;
            rawInt.Unsigned = true;
            return new ValuedToken<RawInt>(TokenType.BinInt, position, rawInt);
        }

        private Token? ProcessName(int readInt)
        {
            // Names must start with a letter, an underscore or a dollar sign
            // NOTE: if these are modified, remember to change them in this.ProcessNumber_Float() as well!
            if (!char.IsLetter((char)readInt) && readInt != '_' && readInt != '$')
                return null;

            long position = _source.Position;
            bool startsWithDollar = readInt == '$';
            StringBuilder keyword = new StringBuilder();
            keyword.Append((char)readInt);

            while ((readInt = _source.Read()) >= 0)
            {
                // Further dollar signs are not allowed, but digits are
                if (char.IsLetterOrDigit((char)readInt) || readInt == '_')
                {
                    keyword.Append((char)readInt);
                }
                else
                {
                    // We read a character which does not belong to the keyword
                    // Put that back and return the token
                    _source.PushBack(readInt);
                    break;
                }
            }
            return new ValuedToken<string>(startsWithDollar ? TokenType.ReservedName : TokenType.Name, position, keyword.ToString());
        }

        private Token? ProcessString(int readInt, bool inEscapeMode = false)
        {
            // Must start with double quote
            if ((char)readInt != '"') return null;
            long position = _source.Position;

            bool isEscaping = false;
            StringBuilder sb = new StringBuilder();
            while ((readInt = _source.Read()) >= 0)
            {
                if (inEscapeMode)
                {
                    if (readInt == '"')
                        return new ValuedToken<string>(TokenType.String, position, sb.ToString());
                    else
                        sb.Append((char)readInt);
                }
                else if (isEscaping)
                {
                    isEscaping = false;
                    switch (readInt)
                    {
                        case 'n':
                            sb.Append("\n");
                            break;
                        case 'r':
                            sb.Append("\r");
                            break;
                        case 't':
                            sb.Append("\t");
                            break;
                        case '\\':
                        case '"':
                            sb.Append((char)readInt);
                            break;
                        default:
                            throw new SyntaxException(_source.Position, "Unrecognized escape sequence");
                    }
                }
                else
                {
                    switch (readInt)
                    {
                        case '\\':
                            isEscaping = true;
                            break;
                        default:
                            sb.Append((char)readInt);
                            break;
                        case '"':
                            return new ValuedToken<string>(TokenType.String, position, sb.ToString());
                    }
                }
            }
            throw new SyntaxException(_source.Position, "Unexpected end of document");
        }

    }
}