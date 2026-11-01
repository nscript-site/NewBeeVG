import * as vscode from 'vscode';
import * as path from 'path';

const COMMAND_ID = 'nbvg.watch';
const CLI = 'nbvg.exe';
const CLI_SUBCOMMAND = 'watch';

/** Terminals keyed by file path, so the same file reuses its terminal. */
const terminals = new Map<string, vscode.Terminal>();

export function activate(context: vscode.ExtensionContext) {
	context.subscriptions.push(
		vscode.commands.registerCommand(COMMAND_ID, (uri?: vscode.Uri) => {
			const filePath = resolveTargetPath(uri);

			if (!filePath) {
				void vscode.window.showWarningMessage('nbvg: no C# file is currently active.');
				return;
			}

			if (path.extname(filePath).toLowerCase() !== '.cs') {
				void vscode.window.showWarningMessage('nbvg: the active file is not a C# file (.cs).');
				return;
			}

			runWatch(filePath);
		})
	);

	context.subscriptions.push(
		vscode.window.onDidCloseTerminal((closed) => {
			for (const [key, terminal] of terminals) {
				if (terminal === closed) {
					terminals.delete(key);
				}
			}
		}),
		{ dispose: () => terminals.clear() }
	);
}

export function deactivate() {
	terminals.clear();
}

/**
 * The editor title button passes the editor's resource Uri as the first argument.
 * Fall back to the active editor when the command is invoked from elsewhere.
 */
function resolveTargetPath(uri?: vscode.Uri): string | undefined {
	if (uri instanceof vscode.Uri && uri.scheme === 'file') {
		return uri.fsPath;
	}

	const active = vscode.window.activeTextEditor;
	if (active && active.document.uri.scheme === 'file') {
		return active.document.uri.fsPath;
	}

	return undefined;
}

function runWatch(filePath: string): void {
	const terminal = getTerminal(filePath);
	terminal.show(true);
	terminal.sendText(`${CLI} ${CLI_SUBCOMMAND} ${quote(filePath)}`);
}

function getTerminal(filePath: string): vscode.Terminal {
	const existing = terminals.get(filePath);
	if (existing && existing.exitStatus === undefined) {
		return existing;
	}

	const terminal = vscode.window.createTerminal({
		name: `${CLI}: ${path.basename(filePath)}`,
		cwd: path.dirname(filePath),
	});

	terminals.set(filePath, terminal);
	return terminal;
}

function quote(value: string): string {
	return `"${value.replace(/"/g, '\\"')}"`;
}
