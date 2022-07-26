/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Language
{
    public class IntType : TypeBase
    {
        public IntType(int bits, bool unsigned)
        {
            if (bits <= 0) throw new ArgumentOutOfRangeException("bits");
            _unsigned = unsigned;
            _bits = bits;
        }

        private bool _unsigned;
        private int _bits;

        public int Bits
        {
            get { return _bits; }
        }

        public bool Unsigned
        {
            get
            {
                return _unsigned;
            }
        }

        public long Mask
        {
            get
            {
                return (1L << _bits) - 1;
            }
        }

        public override string Identifier
        {
            get { return (_unsigned ? "uint" : "int") + _bits; }
        }

        public override bool CanCastTo(IType? other)
        {
            if (other is FloatType)
                return true;
            else
                return base.CanCastTo(other);
        }

        public override IType OperationPlus(IType other)
        {
            if (other is IntType)
                return this;
            else if (other is FloatType)
                return other;
            else
                return base.OperationPlus(other);
        }

        public override IType OperationMinus(IType other)
        {
            if (other is IntType)
                return this;
            else if (other is FloatType)
                return other;
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
                return other;
            else
                return base.OperationTimes(other);
        }

        public override IType OperationDivide(IType other)
        {
            if (other is IntType)
                return this;
            else if (other is FloatType)
                return other;
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
            if (other is IntType)
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

        public override IType OperationBitwiseAnd(IType other)
        {
            if (other is IntType && ((IntType)other)._bits <= this._bits)
                return this;
            else
                return base.OperationBitwiseAnd(other);
        }

        public override IType OperationBitwiseNot()
        {
            return this;
        }

        public override IType OperationBitwiseOr(IType other)
        {
            if (other is IntType && ((IntType)other)._bits <= this._bits)
                return this;
            else
                return base.OperationBitwiseOr(other);
        }

        public override IType OperationBitwiseXor(IType other)
        {
            if (other is IntType && ((IntType)other)._bits <= this._bits)
                return this;
            else
                return base.OperationBitwiseXor(other);
        }

        public override IType OperationLeftShift(IType other)
        {
            if (other is IntType)
                return this;
            else
                return base.OperationLeftShift(other);
        }

        public override IType OperationRightShift(IType other)
        {
            if (other is IntType)
                return this;
            else
                return base.OperationRightShift(other);
        }
        
    }
}