using System;
using System.IO;
using Rage;

namespace InvestigacaoBR.Core
{
    /// <summary>
    /// Logger centralizado. Grava em dois destinos:
    ///  1) RagePluginHook.log  — via Game.LogTrivial (compatibilidade).
    ///  2) Plugins\LSPDFR\InvestigacaoBR\investigacaobr.log — arquivo proprio, facil de filtrar.
    /// fix #11
    /// </summary>
    public static class Logger
    {
        private const string Prefix = "[InvestigacaoBR]";
        private static readonly string LogDir = Path.Combine("Plugins", "LSPDFR", "InvestigacaoBR");
        private static readonly string LogFile = Path.Combine("Plugins", "LSPDFR", "InvestigacaoBR", "investigacaobr.log");
        private static bool _cabecalhoEscrito;

        public static void Info(string mensagem)
        {
            string linha = $"{Prefix} [INFO] {mensagem}";
            Game.LogTrivial(linha);
            GravarArquivo("INFO", mensagem);
        }

        public static void Warn(string mensagem)
        {
            string linha = $"{Prefix} [AVISO] {mensagem}";
            Game.LogTrivial(linha);
            GravarArquivo("AVISO", mensagem);
        }

        public static void Error(string mensagem)
        {
            string linha = $"{Prefix} [ERRO] {mensagem}";
            Game.LogTrivial(linha);
            GravarArquivo("ERRO", mensagem);
        }

        public static void State(string contexto, string de, string para)
        {
            string mensagem = $"{contexto}: '{de}' -> '{para}'";
            Game.LogTrivial($"{Prefix} [ESTADO] {mensagem}");
            GravarArquivo("ESTADO", mensagem);
        }

        public static void Persistence(string operacao, string detalhe)
        {
            string mensagem = $"{operacao} | {detalhe}";
            Game.LogTrivial($"{Prefix} [PERSIST] {mensagem}");
            GravarArquivo("PERSIST", mensagem);
        }

        public static void Menu(string menu, string acao)
        {
            string mensagem = $"{menu} -> {acao}";
            Game.LogTrivial($"{Prefix} [MENU] {mensagem}");
            GravarArquivo("MENU", mensagem);
        }

        public static void Exception(Exception ex, string contexto)
        {
            if (ex == null) { Warn($"Excecao nula em '{contexto}'."); return; }
            string mensagem = $"{contexto}: {ex.GetType().Name} - {ex.Message}";
            Game.LogTrivial($"{Prefix} [EXCECAO] {mensagem}");
            Game.LogTrivial($"{Prefix} [EXCECAO] StackTrace: {ex.StackTrace}");
            GravarArquivo("EXCECAO", $"{mensagem}\n  {ex.StackTrace}");
        }

        // ----- Arquivo proprio -----

        private static void GravarArquivo(string nivel, string mensagem)
        {
            try
            {
                if (!_cabecalhoEscrito)
                {
                    if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
                    File.AppendAllText(LogFile,
                        $"\r\n========== SESSAO {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========\r\n");
                    _cabecalhoEscrito = true;
                }
                File.AppendAllText(LogFile,
                    $"{DateTime.Now:HH:mm:ss.fff} [{nivel,-7}] {mensagem}\r\n");
            }
            catch
            {
                // Falhar no arquivo de log nunca deve derrubar o plugin.
            }
        }
    }
}