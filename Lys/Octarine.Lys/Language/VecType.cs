/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Language
{
    public class VecType : TypeBase
    {
        public VecType(int dimension)
        {
            if (dimension <= 1) throw new ArgumentOutOfRangeException("dimension");
            _dimension = dimension;
        }

        private int _dimension;

        public override string Identifier
        {
            get
            {
                return "vec" + _dimension;
            }
        }

        public override bool CanCastTo(IType other)
        {
            if (other is GenericVecType)
                return true;
            else
                return base.CanCastTo(other);
        }

        public override IType OperationPlus(IType other)
        {
            if (other is VecType && ((VecType)other)._dimension == this._dimension)
                return this;
            else
                return base.OperationPlus(other);
        }

        public override IType OperationMinus(IType other)
        {
            if (other is VecType && ((VecType)other)._dimension == this._dimension)
                return this;
            else
                return base.OperationMinus(other);
        }

        public override IType OperationTimes(IType other)
        {
            if (other is FloatType || other is IntType)
                return this;
            else if (other is VecType && ((VecType)other)._dimension == this._dimension)
                // Scalar product
                return new FloatType(32);
            else
                return base.OperationTimes(other);
        }

        public override IType OperationDivide(IType other)
        {
            if (other is FloatType || other is IntType)
                return this;
            else
                return base.OperationDivide(other);
        }

        public override IType OperationIndex(IType other)
        {
            return new FloatType(32);
        }

        public override IType OperationUnaryMinus()
        {
            return this;
        }

        public override IType OperationUnaryPlus()
        {
            return this;
        }

        public override IType OperationEqual(IType other)
        {
            if (other is VecType && ((VecType)other)._dimension == this._dimension)
                return new BoolType();
            else
                return base.OperationEqual(other);
        }

        public override IType OperationNotEqual(IType other)
        {
            if (other is VecType && ((VecType)other)._dimension == this._dimension)
                return new BoolType();
            else
                return base.OperationNotEqual(other);
        }
        
    }
}