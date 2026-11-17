namespace Sipl.Parser;

using Sipl.Core.Models;

public enum TokenType
{
    // Ключові слова
    Begin, End, While, Do, If, Then, Else, Skip, Not, Or, Div, Mod,
    
    // Динамічні значення
    Identifier, Number,
    
    // Оператори та символи
    Assign,         // :=
    Equals,         // =
    Greater,        // >
    GreaterOrEqual, // >=
    Plus,           // +
    Minus,          // -
    Multiply,       // *
    LParen,         // (
    RParen,         // )
    Semicolon,      // ;
    
    // Спеціальні
    EOF,            
    Error           // Невідомий символ
}

public record Token(TokenType Type, string Value, SourceSpan Span);