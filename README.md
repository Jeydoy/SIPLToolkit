# SIPLToolkit

![CI & Release](https://github.com/Jeydoy/SIPLToolkit/actions/workflows/ci.yml/badge.svg)

Інструментальний комплекс для мови програмування **SIPL** (*Simple Imperative Programming Language*), реалізований на платформі **.NET 10** та **TypeScript**. Проєкт охоплює повний цикл обробки програм: лексичний та синтаксичний аналіз, побудову AST, інтерпретацію з трасуванням станів, консольний інтерфейс (CLI), сервер мовного протоколу (LSP) та розширення для Visual Studio Code.

---

## Зведена таблиця реалізованих критеріїв (10.0 балів)

| Код критерію | Модуль / Компонент | Опис реалізації | Бали |
| :--- | :--- | :--- | :---: |
| **CORE-01** | `grammar/sipl.ebnf`, `Sipl.Parser`, `Sipl.Tests` | Формальна EBNF-граматика, лексер і рекурсивний низхідний парсер; $\ge 10$ позитивних та $\ge 10$ негативних тестів граматики | **1.5** |
| **CORE-02** | `Sipl.Core`, `Sipl.Parser` | Типізовані вузли AST, позиціонування в коді (`SourceSpan`), модель діагностик (`Diagnostic`) та фасад аналізу `SiplAnalyzer` | **1.5** |
| **CORE-03** | `Sipl.Interpreter`, `Sipl.Tests` | Інтерпретатор операційної семантики SIPL, обчислення фінального стану пам'яті, покриття модулів ядра тестами $\ge 80\%$ | **2.0** |
| **CORE-04** | `Sipl.Interpreter` | Покрокове трасування подій виконання (`VariableChanged`, `EvaluatedCondition`) та захист від нескінченних циклів | **1.0** |
| **CORE-05** | `Sipl.Cli` | Консольна утиліта з командами `parse`, `check`, `run`, `trace`, структурованим виводом `--json` та кодами завершення (`0` / `1`) | **0.5** |
| **IDE-04** | `Sipl.LanguageServer`, `vscode-sipl` | LSP-сервер (OmniSharp, JSON-RPC over stdio) + клієнт VS Code: live-діагностика, `Hover`, `Completion`, `Outline` (`DocumentSymbol`) та `SemanticTokens` | **2.0** |
| **REL-01** | `README.md`, `CHANGELOG.md`, `examples/` | Повна документація проєкту, специфікація граматики, історія змін та набір прикладів `.sipl`-програм | **0.5** |
| **REL-02** | `.github/workflows/ci.yml` | GitHub Actions CI/CD: автоматична збірка, прогін модульних та протокольних тестів і публікація релізу за тегом `v*` | **0.5** |
| **REL-04** | `vscode-sipl/*.vsix` | Автономне пакування розширення VS Code у `.vsix` із вбудованим бінарником `Sipl.LanguageServer` | **0.5** |
| **Разом** | | | **10.0** |

---

## Структура проєкту

* `grammar/sipl.ebnf` — формальна EBNF-граматика мови SIPL.
* `examples/` — приклади програм мовою SIPL (`power.sipl`, `factorial.sipl`, `gcd.sipl`, `branching.sipl`, `invalid_syntax.sipl`).
* `Sipl.Core/` — моделі AST, токени, `SourceSpan` та `Diagnostic`.
* `Sipl.Parser/` — лексичний аналізатор (`Lexer`), синтаксичний аналізатор (`Parser`) та фасад `SiplAnalyzer`.
* `Sipl.Interpreter/` — інтерпретатор (`SiplInterpreter`) і модель подій трасування (`TraceEvent`).
* `Sipl.Cli/` — консольний застосунок.
* `Sipl.LanguageServer/` — сервер Language Server Protocol на базі `OmniSharp.Extensions.LanguageServer`.
* `vscode-sipl/` — клієнтське розширення для Visual Studio Code.
* `Sipl.Tests/` — модульні та інтеграційні тести (xUnit).

---

## Використання CLI (`Sipl.Cli`)

Запуск здійснюється з кореневої директорії репозиторію через `dotnet run --project Sipl.Cli -- <команда> <файл> [--json]`.

### 1. Перевірка синтаксису (`check`)
Перевіряє файл на наявність лексичних і синтаксичних помилок (повертає код виходу `0` при успіху або `1` при помилці):
```bash
dotnet run --project Sipl.Cli -- check examples/power.sipl
dotnet run --project Sipl.Cli -- check examples/invalid_syntax.sipl --json