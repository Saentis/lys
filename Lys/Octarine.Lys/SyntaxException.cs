/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
namespace Octarine.Lys
{
    public class SyntaxException : Exception
    {
        public SyntaxException(long position, string message)
            : base(message)
        {
            this.Position = position;
        }

        public SyntaxException(long position, Exception innerException)
            : base(innerException.Message, innerException)
        {
            this.Position = position;
        }

        public long Position { get; private set; }

        public override string ToString()
        {
            return "Syntax error at " + this.Position + ": " + this.Message;
        }

    }
}