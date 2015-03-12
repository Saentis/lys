/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Language
{
    public class BoolType : TypeBase
    {
        public BoolType()
        {
        }

        public override string Identifier
        {
            get { return "bool"; }
        }

        public override IType OperationEqual(IType other)
        {
            if (other is BoolType)
                return this;
            else
                return base.OperationEqual(other);
        }

        public override IType OperationNotEqual(IType other)
        {
            if (other is BoolType)
                return this;
            else
                return base.OperationNotEqual(other);
        }

        public override IType OperationLogicalAnd(IType other)
        {
            if (other is BoolType)
                return this;
            else
                return base.OperationLogicalAnd(other);
        }

        public override IType OperationLogicalNot()
        {
            return this;
        }

        public override IType OperationLogicalOr(IType other)
        {
            if (other is BoolType)
                return this;
            else
                return base.OperationLogicalOr(other);
        }

    }
}