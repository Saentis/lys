/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Language
{
    public abstract class TypeBase : IType
    {
        public abstract string Identifier { get; }

        public virtual bool CanCastTo(IType other)
        {
            return other.GetType() == this.GetType();
        }

        public virtual IType OperationPlus(IType other)
        {
            if (this is StringType || other is StringType)
                return new StringType();
            throw new NotSupportedException();
        }

        public virtual IType OperationMinus(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationTimes(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationDivide(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationModulo(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationLogicalNot()
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationLogicalAnd(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationLogicalOr(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationBitwiseNot()
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationBitwiseAnd(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationBitwiseOr(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationBitwiseXor(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationUnaryPlus()
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationUnaryMinus()
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationLeftShift(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationRightShift(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationEqual(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationNotEqual(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationLessThan(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationLessThanEqual(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationMoreThan(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationMoreThanEqual(IType other)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationProperty(string name)
        {
            throw new NotSupportedException();
        }

        public virtual IType OperationIndex(IType other)
        {
            throw new NotSupportedException();
        }

    }
}