using System.Globalization;
using System.Text;

namespace Calculator;

internal class ParseException(string Message) : Exception(Message);

internal struct Parser
{
    private const int RECURSION_DEPTH_LIMIT = 64;

    private int _depth;
    private TokenKind _kind;
    private string _name;
    private Tokenizer _tokenizer;

    public Parser(string input)
    {
        _depth = 0;
        _tokenizer = new(input);
        (_kind, _name) = _tokenizer.NextToken();
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
        [BinaryOp.Add] = 2,
        [BinaryOp.Subtract] = 2,
        [BinaryOp.Multiply] = 3,
        [BinaryOp.Divide] = 3,
        [BinaryOp.Remainder] = 3,
        [BinaryOp.Power] = 4,
    };

    public Ast Parse()
    {
        Ast ast = ParseBinary();

        switch (_kind)
        {
        case TokenKind.EOF:
            return ast;

        case TokenKind.RiParen:
            throw new ParseException("Unexpected closing parenthesis");

        default:
            throw new ParseException("Expected end of input");
        }
    }

    private Ast ParseBinary(int precedence = 1)
    {
        Ast x = ParsePrefix();

        while (_operators.TryGetValue(_kind, out BinaryOp op) && _operatorPrecedence[op] >= precedence)
        {
            NextToken();
            x = new Ast.BinaryOperation(x, ParseBinary(_operatorPrecedence[op] + 1), op);
        }

        return x;
    }

    private Ast ParsePrefix()
    {
        switch (_kind)
        {
        case TokenKind.Minus:
            NextToken();
            return new Ast.UnaryOperation(ParsePrefix(), UnaryOp.Negate);

        case TokenKind.Plus:
            NextToken();
            return ParsePrefix();

        default:
            return ParseOperand();
        }
    }

    private Ast ParseOperand()
    {
        switch (_kind)
        {
        case TokenKind.Name:
            string name = _name;
            NextToken();

            if (_kind == TokenKind.LeParen)
            {
                NextToken();
                List<Ast> args = [];
                while (true)
                {
                    args.Add(ParseBinary());

                    if (_kind == TokenKind.RiParen) break;
                    else if (_kind == TokenKind.Comma) NextToken();
                    else throw new ParseException("Expected comma or end of argument list");
                }
                NextToken();
                return new Ast.Function(name, args.ToArray());
            }

            return new Ast.Variable(name);

        case TokenKind.Number:
            _name = _name.Replace(',', '.');
            double.TryParse(_name, NumberStyles.Any, CultureInfo.InvariantCulture, out double value);
            NextToken();
            return new Ast.Number(value);

        case TokenKind.LeParen:
            NextToken();

            ++_depth;
            if (_depth > RECURSION_DEPTH_LIMIT) throw new ParseException("Expression is too nested");

            var expression = ParseBinary();

            if (_kind != TokenKind.RiParen) throw new ParseException("Expected closing parenthesis");
            NextToken();
            --_depth;

            return expression;

        default:
            throw new ParseException("Expected operand");
        }
    }

    private void NextToken()
    {
        if (_kind != TokenKind.EOF)
        {
            (_kind, _name) = _tokenizer.NextToken();
        }
    }

    internal struct Tokenizer(string input)
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

        internal (TokenKind kind, string name) NextToken()
        {
            SkipWhile(char.IsWhiteSpace);

            if (Peek == '\0')
            {
                return (TokenKind.EOF, "");
            }
            if (char.IsAsciiDigit(Peek))
            {
                string intPart = TakeWhile(char.IsAsciiDigit);
                if (Peek == '.')
                {
                    Advance();
                    string floatPart = TakeWhile(char.IsAsciiDigit);
                    return (TokenKind.Number, $"{intPart}.{floatPart}");
                }
                return (TokenKind.Number, intPart);
            }
            if (char.IsAsciiLetter(Peek))
            {
                return (TokenKind.Name, TakeWhile(char.IsAsciiLetter));
            }
            if (_operators.TryGetValue(Peek, out TokenKind kind))
            {
                Advance();
                return (kind, "");
            }

            throw new ParseException($"Illegal character: '{Peek}'");
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

    internal enum TokenKind
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
    }
}
