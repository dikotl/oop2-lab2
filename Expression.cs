namespace Calculator;

internal abstract record Expression
{
    internal sealed record Number(double Value) : Expression;
    internal sealed record DefineVariable(string Name, Expression Value) : Expression;
    internal sealed record Variable(string Name) : Expression;
    internal sealed record Function(string Name, Expression[] Args) : Expression;
    internal sealed record BinaryOperation(Expression X, Expression Y, BinaryOp Kind) : Expression;
    internal sealed record UnaryOperation(Expression X, UnaryOp Kind) : Expression;
}

internal enum BinaryOp
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Remainder,
    Power,
}

internal enum UnaryOp
{
    Negate,
}
