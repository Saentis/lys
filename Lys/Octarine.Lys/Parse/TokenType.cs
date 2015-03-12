/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys.Parse
{
    public enum TokenType
    {
        // Math operators
        OperatorPlus,
        OperatorMinus,
        OperatorTimes,
        OperatorDivide,
        OperatorModulo,

        // Bitwise operators
        OperatorLeftShift,
        OperatorRightShift,
        OperatorBitwiseNot,
        OperatorBitwiseAnd,
        OperatorBitwiseOr,
        OperatorBitwiseXor,

        // Logical operators
        OperatorLogicalNot,
        OperatorLogicalAnd,
        OperatorLogicalOr,

        // Comparison operators
        OperatorEqual,
        OperatorNotEqual,
        OperatorLessThan,
        OperatorLessThanEqual,
        OperatorMoreThan,
        OperatorMoreThanEqual,

        // Assignment operators
        OperatorAssign,
        OperatorAssignPlus,
        OperatorAssignMinus,
        OperatorAssignTimes,
        OperatorAssignDivide,
        OperatorAssignModulo,
        OperatorAssignLeftShift,
        OperatorAssignRightShift,
        OperatorAssignBitwiseAnd,
        OperatorAssignBitwiseOr,
        OperatorAssignBitwiseXor,
        OperatorIncrement,
        OperatorDecrement,

        // Common symbols
        BracketLeft,
        BracketRight,
        SquareBracketLeft,
        SquareBracketRight,
        CurlyBracketLeft,
        CurlyBracketRight,
        Comma,
        Period,
        Colon,
        DoubleColon,
        EndOfInstruction,
        EndOfDocument,
        
        Name,
        ReservedName,
        DecInt,
        HexInt,
        BinInt,
        Float,
        String,
    }
}