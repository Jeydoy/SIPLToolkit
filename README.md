Program    ::= Statement
Statement  ::= "begin" StmtList "end"
             | "while" Condition "do" Statement
             | "if" Condition "then" Statement "else" Statement
             | Identifier ":=" Expression
             | "skip"

StmtList   ::= Statement ( ";" Statement )*

Condition  ::= CondTerm ( "or" CondTerm )*
CondTerm   ::= "not" Condition | "(" Condition ")" 
             | Expression RelOp Expression

RelOp      ::= "=" | ">" | ">="

Expression ::= Term ( ( "+" | "-" ) Term )*
Term       ::= Factor ( ( "*" | "div" | "mod" ) Factor )*
Factor     ::= Identifier | Number | "(" Expression ")"

Identifier ::= [a-zA-Z_][a-zA-Z0-9_]*
Number     ::= [0-9]+