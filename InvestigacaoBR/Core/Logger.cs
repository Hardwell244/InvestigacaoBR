using System;
using Rage;

namespace InvestigacaoBR.Core
{
    /// <summary>
    /// Logger centralizado do InvestigacaoBR.
    /// Encapsula Game.LogTrivial para padronizar todos os registros do plugin no
    /// RagePluginHook.log: ciclo de vida, transicoes de estado, persistencia,
    /// cliques de menu e excecoes. O prefixo unico facilita filtrar nossos logs.
    /// </summary>
    public static class Logger
    {
        // Prefixo unico de todos os logs deste plugin. Use Ctrl+F por este texto
        // no RagePluginHook.log para isolar apenas o que veio do InvestigacaoBR.
        private const string Prefix = "[InvestigacaoBR]";

        /// <summary>Informacao geral de fluxo (carregou, iniciou, executou X).</summary>
        public static void Info(string mensagem)
        {
            Game.LogTrivial($"{Prefix} [INFO] {mensagem}");
        }

        /// <summary>Algo inesperado mas nao fatal (faltou dado, fallback acionado).</summary>
        public static void Warn(string mensagem)
        {
            Game.LogTrivial($"{Prefix} [AVISO] {mensagem}");
        }

        /// <summary>Erro recuperavel que merece atencao na hora de depurar.</summary>
        public static void Error(string mensagem)
        {
            Game.LogTrivial($"{Prefix} [ERRO] {mensagem}");
        }

        /// <summary>
        /// Transicao de estado: ciclo de vida do plugin, on/off duty,
        /// mudanca de status de caso, mudanca de Role de um Ped, etc.
        /// </summary>
        public static void State(string contexto, string de, string para)
        {
            Game.LogTrivial($"{Prefix} [ESTADO] {contexto}: '{de}' -> '{para}'");
        }

        /// <summary>Operacoes de salvar/carregar casos em disco (XML/JSON).</summary>
        public static void Persistence(string operacao, string detalhe)
        {
            Game.LogTrivial($"{Prefix} [PERSIST] {operacao} | {detalhe}");
        }

        /// <summary>
        /// Interacao de menu LemonUI: abertura, selecao de item, clique de acao.
        /// </summary>
        public static void Menu(string menu, string acao)
        {
            Game.LogTrivial($"{Prefix} [MENU] {menu} -> {acao}");
        }

        /// <summary>
        /// Registra uma excecao capturada (com stack trace) sem derrubar o plugin.
        /// Use sempre dentro de try/catch nos pontos criticos.
        /// </summary>
        public static void Exception(Exception ex, string contexto)
        {
            if (ex == null)
            {
                Game.LogTrivial($"{Prefix} [EXCECAO] {contexto}: excecao nula recebida.");
                return;
            }

            Game.LogTrivial($"{Prefix} [EXCECAO] {contexto}: {ex.GetType().Name} - {ex.Message}");
            Game.LogTrivial($"{Prefix} [EXCECAO] StackTrace: {ex.StackTrace}");
        }
    }
}