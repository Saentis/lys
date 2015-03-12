/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Language
{
    public class FloatType : TypeBase
    {
        public FloatType(int bits)
        {
            if (bits <= 0 || bits % 8 != 0) throw new ArgumentOutOfRangeException("bits");
            _bits = bits;
        }

        private int _bits;

        public override string Identifier
        {
            get { return "float" + _bits; }
        }

        public override IType OperationPlus(IType other)
        {
            if (other is IntType)
                return this;
            else if (other is FloatType)
            {
                if (this._bits < ((FloatType)other)._bits)
                    return other;
                else
                    return this;
            }
            else
                return base.OperationPlus(other);
        }

        public override IType OperationMinus(IType other)
        {
            if (other is IntType)
                return this;
            else if (other is FloatType)
            {
                if (this._bits < ((FloatType)other)._bits)
                    return other;
                else
                    return this;
            }
            else
                return base.OperationMinus(other);
        }

        public override IType OperationTimes(IType other)
        {
            if (other is IntType)
                return this;
            else if (other is VecType)
                return other;
            else if (other is FloatType)
            {
                if (this._bits < ((FloatType)other)._bits)
                    return other;
                else
                    return this;
            }
            else
                return base.OperationTimes(other);
        }

        public override IType OperationDivide(IType other)
        {
            if (other is IntType)
                return this;
            else if (other is FloatType)
            {
                if (this._bits < ((FloatType)other)._bits)
                    return other;
                else
                    return this;
            }
            else
                return base.OperationDivide(other);
        }

        public override IType OperationEqual(IType other)
        {
            if (other is IntType || other is FloatType)
                return new BoolType();
            else
                return base.OperationEqual(other);
        }

        public override IType OperationNotEqual(IType other)
        {
            if (other is IntType || other is FloatType)
                return new BoolType();
            else
                return base.OperationNotEqual(other);
        }

        public override IType OperationLessThan(IType other)
        {
            if (other is IntType || other is FloatType)
                return new BoolType();
            else
                return base.OperationLessThan(other);
        }

        public override IType OperationLessThanEqual(IType other)
        {
            if (other is IntType || other is FloatType)
                return new BoolType();
            else
                return base.OperationLessThanEqual(other);
        }

        public override IType OperationMoreThan(IType other)
        {
            if (other is IntType || other is FloatType)
                return new BoolType();
            else
                return base.OperationMoreThan(other);
        }

        public override IType OperationMoreThanEqual(IType other)
        {
            if (other is IntType || other is FloatType)
                return new BoolType();
            else
                return base.OperationMoreThanEqual(other);
        }

        public override IType OperationModulo(IType other)
        {
            if (other is IntType || other is FloatType)
                return this;
            else
                return base.OperationModulo(other);
        }

        public override IType OperationUnaryMinus()
        {
            return this;
        }

        public override IType OperationUnaryPlus()
        {
            return this;
        }
    }
}