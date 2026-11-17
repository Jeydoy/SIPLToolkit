import * as path from 'path';
import * as fs from 'fs';
import { ExtensionContext } from 'vscode';
import {
  LanguageClient,
  LanguageClientOptions,
  ServerOptions
} from 'vscode-languageclient/node';

let client: LanguageClient | undefined;

export function activate(context: ExtensionContext): void {
  const bundledDll = context.asAbsolutePath(path.join('server', 'Sipl.LanguageServer.dll'));
  const devCsproj = path.resolve(context.extensionPath, '..', 'Sipl.LanguageServer', 'Sipl.LanguageServer.csproj');

  const serverArgs = fs.existsSync(bundledDll)
    ? [bundledDll]
    : ['run', '--project', devCsproj, '--no-build'];

  const serverOptions: ServerOptions = {
    run: { command: 'dotnet', args: serverArgs },
    debug: { command: 'dotnet', args: serverArgs }
  };

  const clientOptions: LanguageClientOptions = {
    documentSelector: [{ scheme: 'file', language: 'sipl' }]
  };

  client = new LanguageClient(
    'siplLanguageServer',
    'SIPL Language Server',
    serverOptions,
    clientOptions
  );

  client.start();
}

export function deactivate(): Thenable<void> | undefined {
  return client ? client.stop() : undefined;
}