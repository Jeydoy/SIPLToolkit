namespace Sipl.Parser;

using System;
using System.Collections.Generic;
using Sipl.Core.Ast;
using Sipl.Core.Models;

public class Parser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _position;

    public Parser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
    }

    public ProgramNode Parse()
    {
        var statement = ParseStatement();
        return new ProgramNode { Body = statement, Span = statement.Span };
    }

    // --- Стейтменти ---

    private Statement ParseStatement()
    {
        var token = Peek();
        return token.Type switch
        {
            TokenType.Begin => ParseBlock(),
            TokenType.Identifier => ParseAssignment(),
            TokenType.While => ParseWhile(),
            TokenType.Skip => ParseSkip(),
            TokenType.If => ParseIf(), 
            _ => throw Error($"Очікувався оператор, але знайдено '{token.Value}'")
        };
    }

    private SequenceNode ParseBlock()
    {
        var beginToken = Consume(TokenType.Begin, "Очікувалося 'begin'");
        var statements = new List<Statement> { ParseStatement() };

        while (Match(TokenType.Semicolon))
        {
            statements.Add(ParseStatement());
        }

        var endToken = Consume(TokenType.End, "Очікувалося 'end' після блоку");
        
        return new SequenceNode 
        { 
            Statements = statements,
            Span = new SourceSpan(beginToken.Span.Line, beginToken.Span.Column, 
                                  (endToken.Span.Column + endToken.Span.Length) - beginToken.Span.Column)
        };
    }

    private AssignNode ParseAssignment()
    {
        var idToken = Consume(TokenType.Identifier, "Очікувалася назва змінної");
        Consume(TokenType.Assign, "Очікувалося ':=' після змінної");
        
        var value = ParseExpression();
        
        return new AssignNode 
        { 
            VariableName = idToken.Value, 
            Value = value,
            Span = idToken.Span // Спрощено: беремо координати змінної
        };
    }

    private WhileNode ParseWhile()
    {
        var whileToken = Consume(TokenType.While, "Очікувалося 'while'");
        var condition = ParseExpression(); // В SIPL умови можна розбирати як вирази
        Consume(TokenType.Do, "Очікувалося 'do' після умови циклу");
        var body = ParseStatement();

        return new WhileNode 
        { 
            Condition = condition, 
            Body = body,
            Span = whileToken.Span 
        };
    }

    private SkipNode ParseSkip()
    {
        var skipToken = Consume(TokenType.Skip, "Очікувалося 'skip'");
        return new SkipNode { Span = skipToken.Span };
    }

    // --- Вирази (Арифметика та Умови) ---

    private Expression ParseExpression()
    {
        var expr = ParseTerm();

        while (Match(TokenType.Plus) || Match(TokenType.Minus) || Match(TokenType.Greater) || Match(TokenType.Equals) || Match(TokenType.GreaterOrEqual))
        {
            var operatorToken = Previous();
            var right = ParseTerm();
            expr = new BinaryExprNode 
            { 
                Left = expr, 
                Operator = operatorToken.Value, 
                Right = right,
                Span = expr.Span 
            };
        }

        return expr;
    }

    private IfNode ParseIf()
{
    var ifToken = Consume(TokenType.If, "Очікувалося 'if'");
    var condition = ParseExpression();
    
    Consume(TokenType.Then, "Очікувалося 'then' після умови");
    var thenBranch = ParseStatement();
    
    Consume(TokenType.Else, "Очікувалося 'else' після гілки then");
    var elseBranch = ParseStatement();

    return new IfNode 
    { 
        Condition = condition, 
        ThenBranch = thenBranch, 
        ElseBranch = elseBranch,
        Span = new SourceSpan(ifToken.Span.Line, ifToken.Span.Column, 
            (elseBranch.Span.Column + elseBranch.Span.Length) - ifToken.Span.Column) 
    };
}

    private Expression ParseTerm()
    {
        var factor = ParseFactor();

        while (Match(TokenType.Multiply) || Match(TokenType.Div) || Match(TokenType.Mod))
        {
            var operatorToken = Previous();
            var right = ParseFactor();
            factor = new BinaryExprNode 
            { 
                Left = factor, 
                Operator = operatorToken.Value, 
                Right = right,
                Span = factor.Span
            };
        }

        return factor;
    }

    private Expression ParseFactor()
    {
        if (Match(TokenType.Number))
        {
            return new NumberNode { Value = int.Parse(Previous().Value), Span = Previous().Span };
        }
        if (Match(TokenType.Identifier))
        {
            return new VariableNode { Name = Previous().Value, Span = Previous().Span };
        }
        if (Match(TokenType.LParen))
        {
            var expr = ParseExpression();
            Consume(TokenType.RParen, "Очікувалася ')' після виразу");
            return expr;
        }

        throw Error($"Неочікуваний токен у виразі: '{Peek().Value}'");
    }

    // --- Допоміжні методи ---

    private bool Match(TokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }
        return false;
    }

    private Token Consume(TokenType type, string errorMessage)
    {
        if (Check(type)) return Advance();
        throw Error($"{errorMessage} на рядку {Peek().Span.Line}, колонка {Peek().Span.Column}");
    }

    private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;
    
    private Token Advance()
    {
        if (!IsAtEnd()) _position++;
        return Previous();
    }

    private bool IsAtEnd() => Peek().Type == TokenType.EOF;
    private Token Peek() => _tokens[_position];
    private Token Previous() => _tokens[_position - 1];

    private Exception Error(string message)
    {
        // Для Language Server тут краще збирати помилки у список, а не кидати Exception,
        // але для CLI-компілятора виняток наразі підійде.
        return new Exception($"Syntax Error: {message}");
    }
}