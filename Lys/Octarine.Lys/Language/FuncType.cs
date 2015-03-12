/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Language
{
    public class FuncType : TypeBase
    {
        public FuncType(string[] functionPath)
        {
            _functionPath = functionPath;
        }

        private string[] _functionPath;

        public override string Identifier
        {
            get { return "[function]"; }
        }

        public string[] FunctionPath
        {
            get { return _functionPath; }
        }

        public override bool CanCastTo(IType other)
        {
            // Can never be cast
            return false;
        }

    }
}