"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.activate = activate;
exports.deactivate = deactivate;
const vscode = __importStar(require("vscode"));
const path = __importStar(require("path"));
const COMMAND_ID = 'nbvg.watch';
const CLI = 'nbvg.exe';
const CLI_SUBCOMMAND = 'watch';
/** Terminals keyed by file path, so the same file reuses its terminal. */
const terminals = new Map();
function activate(context) {
    context.subscriptions.push(vscode.commands.registerCommand(COMMAND_ID, (uri) => {
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
    }));
    context.subscriptions.push(vscode.window.onDidCloseTerminal((closed) => {
        for (const [key, terminal] of terminals) {
            if (terminal === closed) {
                terminals.delete(key);
            }
        }
    }), { dispose: () => terminals.clear() });
}
function deactivate() {
    terminals.clear();
}
/**
 * The editor title button passes the editor's resource Uri as the first argument.
 * Fall back to the active editor when the command is invoked from elsewhere.
 */
function resolveTargetPath(uri) {
    if (uri instanceof vscode.Uri && uri.scheme === 'file') {
        return uri.fsPath;
    }
    const active = vscode.window.activeTextEditor;
    if (active && active.document.uri.scheme === 'file') {
        return active.document.uri.fsPath;
    }
    return undefined;
}
function runWatch(filePath) {
    const terminal = getTerminal(filePath);
    terminal.show(true);
    terminal.sendText(`${CLI} ${CLI_SUBCOMMAND} ${quote(filePath)}`);
}
function getTerminal(filePath) {
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
function quote(value) {
    return `"${value.replace(/"/g, '\\"')}"`;
}
//# sourceMappingURL=extension.js.map