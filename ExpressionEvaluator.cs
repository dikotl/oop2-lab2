using System.Diagnostics;

namespace Calculator;

/// <summary>
/// MathFunc stores amount of arguments and the function itself.
/// </summary>
internal record MathFunc(int ArgsCount, Func<double[], double> F);

internal class ExpressionEvaluator
{
    private Dictionary<string, double> _variables = new()
    {
        ["Pi"] = double.Pi,
        ["E"] = double.E,
    };

    private Dictionary<string, MathFunc> _functions = new()
    {
        ["sqrt"] = new MathFunc(1, a => Math.Sqrt(a[0])),
        ["pow"] = new MathFunc(2, a => Math.Pow(a[0], a[1])),
        ["ln"] = new MathFunc(1, a => Math.Log(a[0])),
        ["log"] = new MathFunc(2, a => Math.Log(a[0], a[1])),
        ["abs"] = new MathFunc(1, a => Math.Abs(a[0])),
        ["sin"] = new MathFunc(1, a => Math.Sin(a[0])),
        ["cos"] = new MathFunc(1, a => Math.Cos(a[0])),
        ["tan"] = new MathFunc(1, a => Math.Tan(a[0])),
        ["cot"] = new MathFunc(1, a => 1 / Math.Tan(a[0])),
        ["sec"] = new MathFunc(1, a => 1 / Math.Cos(a[0])),
        ["csc"] = new MathFunc(1, a => 1 / Math.Sin(a[0])),
        ["sinh"] = new MathFunc(1, a => Math.Sinh(a[0])),
        ["cosh"] = new MathFunc(1, a => Math.Cosh(a[0])),
        ["tanh"] = new MathFunc(1, a => Math.Tanh(a[0])),
        ["coth"] = new MathFunc(1, a => (Math.Exp(a[0]) + Math.Exp(-a[0])) / (Math.Exp(a[0]) - Math.Exp(-a[0]))),
        ["sech"] = new MathFunc(1, a => 2 / (Math.Exp(a[0]) + Math.Exp(-a[0]))),
        ["csch"] = new MathFunc(1, a => 2 / (Math.Exp(a[0]) - Math.Exp(-a[0]))),
    };

    public double Eval(string input)
    {
        var stack = new Stack<double>();
        var commands = new List<ICommand>(capacity: 64);
        var expression = new ExpressionParser(input).ParseExpression();

        FlattenAST(expression, commands);

        foreach (ICommand command in commands)
        {
            command.Execute(stack, _variables);
        }

        if (stack.Count != 1)
        {
            throw new UnreachableException("AST flattening implementation bug");
        }

        return stack.Pop();
    }

    private void FlattenAST(Expression expression, List<ICommand> commands)
    {
        if (expression is Expression.Number number)
        {
            commands.Add(new PushCommand(number.Value));
        }
        else if (expression is Expression.DefineVariable def)
        {
            // TODO: evaluate the value and put into `this._variables`.
            FlattenAST(def.Value, commands);
            commands.Add(new DefineCommand(def.Name));
        }
        else if (expression is Expression.Variable variable)
        {
            if (!_variables.TryGetValue(variable.Name, out double value))
            {
                throw new ExecutionException($"Variable '{variable.Name}' is not defined");
            }
            commands.Add(new PushCommand(value));
        }
        else if (expression is Expression.Function function)
        {
            if (!_functions.TryGetValue(function.Name, out MathFunc? f))
            {
                throw new ExecutionException($"Unknown function '{function.Name}'");
            }
            if (function.Args.Length != f.ArgsCount)
            {
                throw new ExecutionException($"Expected {f.ArgsCount} arguments for '{function.Name}'");
            }
            for (int i = function.Args.Length - 1; i >= 0; --i)
            {
                FlattenAST(function.Args[i], commands);
            }
            commands.Add(new FunctionCommand(f));
        }
        else if (expression is Expression.BinaryOperation binary)
        {
            FlattenAST(binary.Y, commands);
            FlattenAST(binary.X, commands);
            commands.Add(binary.Kind switch
            {
                BinaryOp.Add => new AddCommand(),
                BinaryOp.Subtract => new SubtractCommand(),
                BinaryOp.Multiply => new MultiplyCommand(),
                BinaryOp.Divide => new DivideCommand(),
                BinaryOp.Remainder => new RemainderCommand(),
                BinaryOp.Power => new PowCommand(),
                _ => throw new UnreachableException($"Unhandled binary operation: {binary.Kind}"),
            });
        }
        else if (expression is Expression.UnaryOperation unary)
        {
            FlattenAST(unary.X, commands);
            commands.Add(new NegateCommand());
        }
        else
        {
            throw new UnreachableException($"Unhandled AST node: {expression}");
        }
    }
}
