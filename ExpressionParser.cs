using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Calculator;

internal class ParseException(string Message, Expects Expected) : Exception(Message)
{
    public Expects Expected { get; init; } = Expected;
}

[Flags]
internal enum Expects
{
    Digit = 1 << 0,
    DigitAfterDot = 1 << 1,
    Dot = 1 << 2,
    Comma = 1 << 3,
    Name = 1 << 4,
    Unary = 1 << 5,
    Binary = 1 << 6,
    LeParen = 1 << 7,
    RiParen = 1 << 8,
    Store = 1 << 9,
}

internal struct ExpressionParser
{
    internal const Expects EXPECTED_OPERAND = Expects.Name | Expects.Digit | Expects.Unary | Expects.LeParen;

    private const int RECURSION_DEPTH_LIMIT = 64;

    private Tokenizer _tokenizer;
    private TokenKind _kind;
    private string _name;
    private int _depth;
    private Expects? _expectedFromTokenizer;

    public Expects Expected
    {
        get;
        private set
        {
            Debug.WriteLine($"Expected {value}");
            field = value;
        }
    }

    public ExpressionParser(string input)
    {
        _depth = 0;
        _tokenizer = new(input);
        _name = "";

        AdvanceToken(expected: EXPECTED_OPERAND);
    }

    private readonly Dictionary<TokenKind, BinaryOp> _operators = new()
    {
        [TokenKind.Plus] = BinaryOp.Add,
        [TokenKind.Minus] = BinaryOp.Subtract,
        [TokenKind.Asterisk] = BinaryOp.Multiply,
        [TokenKind.Slash] = BinaryOp.Divide,
        [TokenKind.Percent] = BinaryOp.Remainder,
        [TokenKind.Caret] = BinaryOp.Power,
    };

    private readonly Dictionary<BinaryOp, int> _operatorPrecedence = new()
    {
        [BinaryOp.Add] = 3,
        [BinaryOp.Subtract] = 3,
        [BinaryOp.Multiply] = 4,
        [BinaryOp.Divide] = 4,
        [BinaryOp.Remainder] = 4,
        [BinaryOp.Power] = 5,
    };

    public (Expression, Expects) ParseExpression()
    {
        Expression ast = ParsePossiblyAssignment();

        switch (_kind)
        {
        case TokenKind.EOF:
            return (ast, Expected);

        case TokenKind.RiParen:
            throw new ParseException("Unexpected closing parenthesis", Expected);

        default:
            throw new ParseException("Expected end of input", 0);
        }
    }

    private Expression ParsePossiblyAssignment()
    {
        if (_kind == TokenKind.Name)
        {
            string name = _name;
            NextToken(expected: Expects.Store | Expects.Binary | Expects.LeParen);
            if (_kind == TokenKind.Store)
            {
                NextToken(expected: EXPECTED_OPERAND);
                return new Expression.DefineVariable(name, ParseBinary());
            }
            if (_kind == TokenKind.LeParen)
            {
                return ParseFunction(name);
            }
            return ParseBinary(x: new Expression.Variable(name));
        }
        return ParseBinary();
    }

    private Expression ParseBinary(int precedence = 1, Expression? x = null)
    {
        x ??= ParsePrefix();

        while (_operators.TryGetValue(_kind, out BinaryOp op) && _operatorPrecedence[op] >= precedence)
        {
            NextToken(expected: EXPECTED_OPERAND & ~Expects.Unary);
            x = new Expression.BinaryOperation(x, ParseBinary(_operatorPrecedence[op] + 1), op);
        }

        return x;
    }

    private Expression ParsePrefix()
    {
        switch (_kind)
        {
        case TokenKind.Minus:
            NextToken(expected: EXPECTED_OPERAND | Expects.Binary);
            return new Expression.UnaryOperation(ParsePrefix(), UnaryOp.Negate);

        case TokenKind.Plus:
            NextToken(expected: EXPECTED_OPERAND | Expects.Binary);
            return ParsePrefix();

        default:
            return ParseOperand();
        }
    }

    private Expression ParseOperand()
    {
        switch (_kind)
        {
        case TokenKind.Name:
            string name = _name;
            NextToken(expected: Expects.Binary | Expects.LeParen); // Includes '('.
            if (_kind == TokenKind.LeParen) return ParseFunction(name);
            return new Expression.Variable(name);

        case TokenKind.Number:
            _name = _name.Replace(',', '.');
            double.TryParse(_name, NumberStyles.Any, CultureInfo.InvariantCulture, out double value);
            NextToken(expected: Expects.Binary);
            return new Expression.Number(value);

        case TokenKind.LeParen:
            NextToken(expected: EXPECTED_OPERAND);

            ++_depth;
            if (_depth > RECURSION_DEPTH_LIMIT) throw new ParseException("Expression is too nested", 0);

            var expression = ParseBinary();

            if (_kind != TokenKind.RiParen) throw new ParseException("Expected closing parenthesis", Expects.Binary | Expects.RiParen);
            NextToken(expected: Expects.Binary);
            --_depth;

            return expression;

        default:
            throw new ParseException("Expected operand", Expected);
        }
    }

    private Expression ParseFunction(string name)
    {
        if (_kind != TokenKind.LeParen) throw new UnreachableException("No check was before");
        NextToken(expected: EXPECTED_OPERAND);
        List<Expression> args = [];
        while (true)
        {
            args.Add(ParseBinary());
            Expected |= Expects.Comma;

            if (_kind == TokenKind.RiParen) break;
            else if (_kind == TokenKind.Comma) NextToken(expected: EXPECTED_OPERAND);
            else throw new ParseException("Expected comma or end of argument list", Expects.Comma | Expects.RiParen);
        }
        NextToken(expected: Expects.Binary);
        return new Expression.Function(name, args.ToArray());
    }

    private void NextToken(Expects? expected)
    {
        if (_kind != TokenKind.EOF) AdvanceToken(expected);
    }

    private void AdvanceToken(Expects? expected)
    {
        if (expected is not null)
        {
            Expected = (Expects)expected | (_expectedFromTokenizer ?? 0);
        }
        else if (_expectedFromTokenizer is not null)
        {
            Expected = (Expects)_expectedFromTokenizer;
        }
        (_kind, _name, _expectedFromTokenizer) = _tokenizer.NextToken();
    }

    private enum TokenKind
    {
        EOF,
        Number,
        Name,
        Plus,
        Minus,
        Asterisk,
        Slash,
        Percent,
        Caret,
        LeParen,
        RiParen,
        Comma,
        Store,
    }

    private struct Tokenizer(string input)
    {
        private char[] _input = input.ToCharArray();
        private int _index = 0;

        private readonly Dictionary<char, TokenKind> _operators = new()
        {
            ['+'] = TokenKind.Plus,
            ['-'] = TokenKind.Minus,
            ['*'] = TokenKind.Asterisk,
            ['/'] = TokenKind.Slash,
            ['%'] = TokenKind.Percent,
            ['^'] = TokenKind.Caret,
            ['('] = TokenKind.LeParen,
            [')'] = TokenKind.RiParen,
            [','] = TokenKind.Comma,
        };

        private char Peek => _index < _input.Length ? _input[_index] : '\0';

        private void Advance() => ++_index;

        internal (TokenKind kind, string name, Expects? expected) NextToken()
        {
            SkipWhile(char.IsWhiteSpace);

            if (Peek == '\0')
            {
                return (TokenKind.EOF, "", null);
            }
            if (char.IsAsciiDigit(Peek))
            {
                string intPart = TakeWhile(char.IsAsciiDigit);
                Expects expected = (intPart == "0") ? Expects.Dot : (Expects.Digit | Expects.Dot);
                if (Peek == '.')
                {
                    Advance();
                    string floatPart = TakeWhile(char.IsAsciiDigit);
                    return (TokenKind.Number, $"{intPart}.{floatPart}", Expects.DigitAfterDot);
                }
                return (TokenKind.Number, intPart, expected);
            }
            if (char.IsAsciiLetter(Peek))
            {
                return (TokenKind.Name, TakeWhile(char.IsAsciiLetter), null);
            }
            if (_operators.TryGetValue(Peek, out TokenKind kind))
            {
                Advance();
                return (kind, "", null);
            }
            if (Peek == ':')
            {
                Advance();
                if (Peek != '=') throw new ParseException("Expected '=' after ':'", 0);
                Advance();
                return (TokenKind.Store, "", null);
            }

            throw new ParseException($"Illegal character: '{Peek}'", 0);
        }

        private void SkipWhile(Predicate<char> predicate)
        {
            while (predicate(Peek))
            {
                Advance();
            }
        }

        private string TakeWhile(Predicate<char> predicate)
        {
            var buf = new StringBuilder();
            while (predicate(Peek))
            {
                buf.Append(Peek);
                Advance();
            }
            return buf.ToString();
        }
    }
}
