namespace Sipl.Parser;

using System.Text;
using Sipl.Core.Models;

public class Lexer
{
    private readonly string _source;
    private int _position;
    private int _line = 1;
    private int _column = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "begin", TokenType.Begin }, { "end", TokenType.End },
        { "while", TokenType.While }, { "do", TokenType.Do },
        { "if", TokenType.If }, { "then", TokenType.Then }, { "else", TokenType.Else },
        { "skip", TokenType.Skip }, { "not", TokenType.Not }, { "or", TokenType.Or },
        { "div", TokenType.Div }, { "mod", TokenType.Mod }
    };

    public Lexer(string source)
    {
        _source = source ?? string.Empty;
    }

    public IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();
        Token token;
        do
        {
            token = NextToken();
            tokens.Add(token);
        } while (token.Type != TokenType.EOF);

        return tokens;
    }

    private Token NextToken()
    {
        SkipWhitespace();

        if (_position >= _source.Length)
            return new Token(TokenType.EOF, "", new SourceSpan(_line, _column, 0));

        char c = CurrentChar;
        int startLine = _line;
        int startColumn = _column;

        // Числа
        if (char.IsDigit(c))
            return LexNumber(startLine, startColumn);

        // Ідентифікатори та ключові слова
        if (char.IsLetter(c) || c == '_')
            return LexIdentifierOrKeyword(startLine, startColumn);

        // Оператори та символи
        return LexSymbol(startLine, startColumn);
    }

    private Token LexNumber(int line, int col)
    {
        int startPos = _position;
        while (_position < _source.Length && char.IsDigit(CurrentChar))
        {
            Advance();
        }
        string value = _source[startPos.._position];
        return new Token(TokenType.Number, value, new SourceSpan(line, col, value.Length));
    }

    private Token LexIdentifierOrKeyword(int line, int col)
    {
        int startPos = _position;
        while (_position < _source.Length && (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_'))
        {
            Advance();
        }
        
        string value = _source[startPos.._position];
        TokenType type = Keywords.TryGetValue(value, out var kwType) ? kwType : TokenType.Identifier;
        
        return new Token(type, value, new SourceSpan(line, col, value.Length));
    }

    private Token LexSymbol(int line, int col)
    {
        char c = CurrentChar;
        Advance(); // З'їдаємо перший символ

        // Перевірка двосимвольних операторів (:= та >=)
        if (c == ':' && CurrentChar == '=')
        {
            Advance();
            return new Token(TokenType.Assign, ":=", new SourceSpan(line, col, 2));
        }
        if (c == '>' && CurrentChar == '=')
        {
            Advance();
            return new Token(TokenType.GreaterOrEqual, ">=", new SourceSpan(line, col, 2));
        }

        // Односимвольні оператори
        TokenType type = c switch
        {
            '=' => TokenType.Equals,
            '>' => TokenType.Greater,
            '+' => TokenType.Plus,
            '-' => TokenType.Minus,
            '*' => TokenType.Multiply,
            '(' => TokenType.LParen,
            ')' => TokenType.RParen,
            ';' => TokenType.Semicolon,
            _ => TokenType.Error
        };

        return new Token(type, c.ToString(), new SourceSpan(line, col, 1));
    }

    private void SkipWhitespace()
    {
        while (_position < _source.Length && char.IsWhiteSpace(CurrentChar))
        {
            if (CurrentChar == '\n')
            {
                _line++;
                _column = 0; // На наступному Advance() стане 1
            }
            Advance();
        }
    }

    private char CurrentChar => _position < _source.Length ? _source[_position] : '\0';

    private void Advance()
    {
        _position++;
        _column++;
    }
}