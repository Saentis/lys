/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System.Collections.Generic;

namespace Octarine.Lys.Parse
{
    /// <summary>
    /// Class for iterating over tokens in an ITokenizer instance.
    /// </summary>
    public class TokenIterator
    {
        /// <summary>
        /// Initializes a new token iterator.
        /// </summary>
        /// <param name="tokenizer">The tokenizer.</param>
        public TokenIterator(ITokenizer tokenizer)
        {
            if (object.ReferenceEquals(null, tokenizer))
                throw new System.ArgumentNullException("tokenizer");
            this.Tokenizer = tokenizer;
        }

        private Token? _pointer;
        private Stack<Token> _history = new Stack<Token>();

        /// <summary>
        /// Gets the associated tokenizer.
        /// </summary>
        public ITokenizer Tokenizer { get; private set; }

        /// <summary>
        /// Gets the token which the iterator is currently pointing at.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Raises an InvalidOperationException if the iterator is not pointing at a token.
        /// </exception>
        public Token Current
        {
            get
            {
                return _pointer ?? throw new System.InvalidOperationException("Not pointing at a token, use Next().");
            }
        }

        /// <summary>
        /// Gets the position in the source code of the token which the iterator is currently pointing at.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Raises an InvalidOperationException if the iterator is not pointing at a token.
        /// </exception>
        public long Position
        {
            get
            {
                return this.Current.Position;
            }
        }

        /// <summary>
        /// Gets the value of the current token.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Raises an InvalidOperationException if the iterator is not
        /// pointing at a token, or the token does not contain a value.
        /// </exception>
        /// <exception cref="System.InvalidCastException">
        /// Raises an InvalidCastException if the value is not of type T.
        /// </exception>
        public T GetValue<T>() where T : notnull
        {
            if (this.Current is ValuedToken<T> vt)
                return vt.Value;
            else
                throw new System.InvalidCastException();
        }

        /// <summary>
        /// Indicates whether the current token is of any of the specified types.
        /// </summary>
        /// <param name="tokenTypes">The token types to match.</param>
        /// <returns>true if this.Current.Type is contained in tokenTypes.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Raises an InvalidOperationException if the iterator is not pointing at a token.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Raises an InvalidOperationException if tokenType is null.
        /// </exception>
        public bool Is(params TokenType[] tokenTypes)
        {
            if (tokenTypes == null)
                throw new System.ArgumentNullException("tokenTypes");
            return System.Array.IndexOf(tokenTypes, this.Current.Type) >= 0;
        }

        /// <summary>
        /// Moves the iterator to the next token.
        /// </summary>
        /// <returns>false if the last token (EndOfDocument token) has been reached, true otherwise.</returns>
        public bool Next()
        {
            _pointer = this.Tokenizer.Read();

            // If there is a restore point, store what we got
            if (_history.Count > 0)
                _history.Push(_pointer);

            return _pointer.Type != TokenType.EndOfDocument;
        }

        /// <summary>
        /// Creates a revert point for this iterator.
        /// </summary>
        /// <remarks>
        /// The iterator can be reverted to the current state if the Revert() method is called.
        /// Note that CreateRevertPoint() cannot be used again until Revert() or Commit() is called.
        /// </remarks>
        /// <seealso cref="this.Restore"/>
        /// <seealso cref="this.Commit"/>
        public void CreateRevertPoint()
        {
            if (_history.Count > 0)
                throw new System.InvalidOperationException("Revert point already exists, use Revert() or Commit() before calling this method again.");
            _history.Push(this.Current);
        }

        /// <summary>
        /// Reverts back to the revert point set up by CreateRevertPoint().
        /// </summary>
        public void Revert()
        {
            if (_history.Count == 0)
                throw new System.InvalidOperationException("No revert point, use CreateRevertPoint() to create one.");

            // Push everything from history back to the tokenizer
            while (_history.Count > 0)
                this.Tokenizer.PushBack(_history.Pop());
            // The last element in 'history' was the back-then current token,
            // which we pushed back in the previous line. Read it again so that it becomes
            // the 'current' element.
            Next();
        }

        /// <summary>
        /// Destroys the revert point set up by CreateRevertPoint().
        /// </summary>
        public void Commit()
        {
            if (_history.Count == 0)
                throw new System.InvalidOperationException("No revert point, use CreateRevertPoint() to create one.");
            _history.Clear();
        }

    }
}