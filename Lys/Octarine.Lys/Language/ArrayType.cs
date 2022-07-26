/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Language
{
    public class ArrayType : TypeBase
    {
        public ArrayType(IType? baseType)
        {
            _baseType = baseType ?? throw new ArgumentNullException("baseType");
        }

        private IType _baseType;

        public override string Identifier
        {
            get { return _baseType.Identifier + "[]"; }
        }

        public override bool CanCastTo(IType? other)
        {
            if (other is ArrayType)
                return _baseType.CanCastTo(((ArrayType)other)._baseType);
            else
                return base.CanCastTo(other);
        }

        public override IType OperationIndex(IType other)
        {
            if (other is IntType)
                return _baseType;
            else
                return base.OperationIndex(other);
        }
    }
}