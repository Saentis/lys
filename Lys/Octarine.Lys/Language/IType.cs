/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/


namespace Octarine.Lys.Language
{
    /// <summary>
    /// Interface for a data type.
    /// </summary>
    public interface IType
    {
        /// <summary>
        /// Gets the type identifier in the source code.
        /// </summary>
        string Identifier { get; }

        bool CanCastTo(IType? other);

        IType OperationPlus(IType other);
        IType OperationMinus(IType other);
        IType OperationTimes(IType other);
        IType OperationDivide(IType other);
        IType OperationModulo(IType other);
        IType OperationLogicalNot();
        IType OperationLogicalAnd(IType other);
        IType OperationLogicalOr(IType other);
        IType OperationBitwiseNot();
        IType OperationBitwiseAnd(IType other);
        IType OperationBitwiseOr(IType other);
        IType OperationBitwiseXor(IType other);
        IType OperationUnaryPlus();
        IType OperationUnaryMinus();
        IType OperationLeftShift(IType other);
        IType OperationRightShift(IType other);
        IType OperationEqual(IType other);
        IType OperationNotEqual(IType other);
        IType OperationLessThan(IType other);
        IType OperationLessThanEqual(IType other);
        IType OperationMoreThan(IType other);
        IType OperationMoreThanEqual(IType other);
        IType OperationProperty(string name);
        IType OperationIndex(IType other);
    }
}