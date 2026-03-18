namespace Calculator;

internal class ExecutionException(string Message) : Exception(Message);

internal interface ICommand
{
    void Execute(Stack<double> stack, Dictionary<string, double> variables);
}

internal struct AddCommand : ICommand
{
    public void Execute(Stack<double> stack, Dictionary<string, double> variables)
    {
        var a = stack.Pop();
        var b = stack.Pop();
        stack.Push(a + b);
    }
}

internal struct SubtractCommand : ICommand
{
    public void Execute(Stack<double> stack, Dictionary<string, double> variables)
    {
        var a = stack.Pop();
        var b = stack.Pop();
        stack.Push(a - b);
    }
}

internal struct MultiplyCommand : ICommand
{
    public void Execute(Stack<double> stack, Dictionary<string, double> variables)
    {
        var a = stack.Pop();
        var b = stack.Pop();
        stack.Push(a * b);
    }
}

internal struct DivideCommand : ICommand
{
    public void Execute(Stack<double> stack, Dictionary<string, double> variables)
    {
        var a = stack.Pop();
        var b = stack.Pop();
        try
        {
            stack.Push(checked(a / b));
        }
        catch (DivideByZeroException)
        {
            throw new ExecutionException("Division by zero");
        }
    }
}

internal struct RemainderCommand : ICommand
{
    public void Execute(Stack<double> stack, Dictionary<string, double> variables)
    {
        var a = stack.Pop();
        var b = stack.Pop();
        try
        {
            stack.Push(checked(a % b));
        }
        catch (DivideByZeroException)
        {
            throw new ExecutionException("Division by zero");
        }
    }
}

internal struct PowCommand : ICommand
{
    public void Execute(Stack<double> stack, Dictionary<string, double> variables)
    {
        var a = stack.Pop();
        var b = stack.Pop();
        stack.Push(Math.Pow(a, b));
    }
}

internal struct PushCommand(double Value) : ICommand
{
    public void Execute(Stack<double> stack, Dictionary<string, double> variables)
    {
        stack.Push(Value);
    }
}

internal struct DefineCommand(string Name) : ICommand
{
    public void Execute(Stack<double> stack, Dictionary<string, double> variables)
    {
        variables[Name] = stack.Peek();
    }
}

internal struct FunctionCommand(MathFunc f) : ICommand
{
    public void Execute(Stack<double> stack, Dictionary<string, double> variables)
    {
        var args = new double[f.ArgsCount];

        for (int i = 0; i < f.ArgsCount; i++)
        {
            args[i] = stack.Pop();
        }

        stack.Push(f.F(args));
    }
}

internal struct NegateCommand : ICommand
{
    public void Execute(Stack<double> stack, Dictionary<string, double> variables)
    {
        var a = stack.Pop();
        stack.Push(-a);
    }
}
