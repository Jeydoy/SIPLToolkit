namespace Sipl.Interpreter;

using System;
using System.Collections.Generic;
using Sipl.Core.Ast;

public class SiplInterpreter : IAstVisitor<object>
{
    // Стан пам'яті: зберігає значення всіх змінних
    public Dictionary<string, int> Memory { get; } = new();
    
    // Подія (коллбек), яку може слухати CLI або VS Code Debugger
    public Action<TraceEvent>? OnTrace { get; set; }

    // Головний метод запуску
    public void Run(ProgramNode program)
    {
        program.Accept(this);
    }

    private void EmitTrace(string type, AstNode node, string msg = "")
    {
        // Передаємо копію пам'яті, щоб зберегти історію
        OnTrace?.Invoke(new TraceEvent(type, node, new Dictionary<string, int>(Memory), msg));
    }

    // --- Виконання стейтментів ---

    public object Visit(ProgramNode node)
    {
        node.Body.Accept(this);
        return null!;
    }

    public object Visit(SequenceNode node)
    {
        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }
        return null!;
    }

    public object Visit(AssignNode node)
    {
        EmitTrace("BeforeStatement", node, $"Присвоювання {node.VariableName}");
        
        int value = (int)node.Value.Accept(this);
        Memory[node.VariableName] = value;
        
        EmitTrace("VariableChanged", node, $"{node.VariableName} := {value}");
        return null!;
    }

    public object Visit(WhileNode node)
    {
        int limit = 10000; // CORE-03: Контроль нескінченних циклів
        int iterations = 0;

        while (true)
        {
            EmitTrace("BeforeConditionCheck", node.Condition);
            bool conditionResult = (bool)node.Condition.Accept(this);
            EmitTrace("EvaluatedCondition", node.Condition, conditionResult.ToString());

            if (!conditionResult) break;

            if (iterations++ > limit)
                throw new Exception("Runtime Error: Перевищено ліміт кроків (нескінченний цикл).");

            node.Body.Accept(this);
        }
        return null!;
    }

    public object Visit(SkipNode node)
    {
        EmitTrace("Statement", node, "Skip");
        return null!;
    }

    // --- Обчислення виразів ---

    public object Visit(NumberNode node)
    {
        return node.Value;
    }

    public object Visit(VariableNode node)
    {
        if (Memory.TryGetValue(node.Name, out int val))
            return val;
        
        throw new Exception($"Runtime Error: Змінна '{node.Name}' не ініціалізована.");
    }

    public object Visit(IfNode node)
{
    EmitTrace("BeforeConditionCheck", node.Condition);
    bool conditionResult = (bool)node.Condition.Accept(this);
    EmitTrace("EvaluatedCondition", node.Condition, conditionResult.ToString());

    if (conditionResult)
    {
        node.ThenBranch.Accept(this);
    }
    else
    {
        node.ElseBranch.Accept(this);
    }
    
    return null!;
}

    public object Visit(BinaryExprNode node)
    {
        // Обчислюємо ліву і праву частину (через рекурсію)
        var left = node.Left.Accept(this);
        var right = node.Right.Accept(this);

        return node.Operator switch
        {
            // Математика (повертає int)
            "+" => (int)left + (int)right,
            "-" => (int)left - (int)right,
            "*" => (int)left * (int)right,
            "div" => (int)left / (int)right,
            "mod" => (int)left % (int)right,

            // Умови (повертає bool)
            "=" => (int)left == (int)right,
            ">" => (int)left > (int)right,
            ">=" => (int)left >= (int)right,
            "or" => (bool)left || (bool)right,

            _ => throw new Exception($"Runtime Error: Невідомий оператор '{node.Operator}'")
        };
    }
}