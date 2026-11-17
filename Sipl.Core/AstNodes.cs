namespace Sipl.Core.Ast;

using Sipl.Core.Models;

public interface IAstVisitor<out T>
{
    T Visit(ProgramNode node);
    T Visit(AssignNode node);
    T Visit(SequenceNode node);
    T Visit(WhileNode node);
    T Visit(SkipNode node);
    
    T Visit(BinaryExprNode node);
    T Visit(NumberNode node);
    T Visit(VariableNode node);
    T Visit(IfNode node);
}

// Базовий клас для всіх вузлів
public abstract class AstNode
{
    public SourceSpan Span { get; init; } = SourceSpan.Empty;
    public abstract T Accept<T>(IAstVisitor<T> visitor);
}

public abstract class Statement : AstNode { }
public abstract class Expression : AstNode { }

// --- Стейтменти (Statements) ---

public class ProgramNode : AstNode
{
    public Statement Body { get; init; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}

public class AssignNode : Statement
{
    public string VariableName { get; init; }
    public Expression Value { get; init; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}

public class SequenceNode : Statement
{
    public IReadOnlyList<Statement> Statements { get; init; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}

public class IfNode : Statement
{
    public Expression Condition { get; init; }
    public Statement ThenBranch { get; init; }
    public Statement ElseBranch { get; init; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}

public class WhileNode : Statement
{
    public Expression Condition { get; init; } // Для простоти Condition поки спадкуємо від Expression
    public Statement Body { get; init; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}

public class SkipNode : Statement
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}

// --- Вирази (Expressions) ---

public class BinaryExprNode : Expression
{
    public Expression Left { get; init; }
    public string Operator { get; init; } // "+", "-", "div", ">=", "or"
    public Expression Right { get; init; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}

public class NumberNode : Expression
{
    public int Value { get; init; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}

public class VariableNode : Expression
{
    public string Name { get; init; }
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}