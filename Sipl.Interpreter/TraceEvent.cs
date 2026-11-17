namespace Sipl.Interpreter;

using Sipl.Core.Ast;
using System.Collections.Generic;

// Модель для відслідковування виконання (Trace)
public record TraceEvent(
    string EventType,      // "BeforeStatement", "AfterStatement", "VariableChanged", "EvaluatedCondition"
    AstNode Node,          // Вузол дерева, який зараз виконується
    Dictionary<string, int> MemorySnapshot, // Стан пам'яті на момент події
    string Message = ""    // Додатковий опис
);