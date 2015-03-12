/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Language
{
    public class StringType : TypeBase
    {
        public StringType()
        {
        }

        public override string Identifier
        {
            get { return "string"; }
        }

        public override IType OperationEqual(IType other)
        {
            if (other is StringType)
                return new BoolType();
            else
                return base.OperationEqual(other);
        }

        public override IType OperationNotEqual(IType other)
        {
            if (other is StringType)
                return new BoolType();
            else
                return base.OperationNotEqual(other);
        }

        public override IType OperationIndex(IType other)
        {
            if (other is IntType)
                return new IntType(32, true);
            else
                return base.OperationIndex(other);
        }

        public override IType OperationPlus(IType other)
        {
            if (other is StringType)
                return this;
            else
                return base.OperationPlus(other);
        }

    }
}