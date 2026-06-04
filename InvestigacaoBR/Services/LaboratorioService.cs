using System;
using System.Collections.Generic;
using Rage;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Laboratorio forense. Recebe evidencias coletadas, transita Coletada -> NoLab, espera um
    /// delay (em ms de jogo) num GameFiber, e ao final conclui a analise (NoLab -> Analisada,
    /// liberando DNA/laudo), notifica o jogador e persiste. Retoma analises pendentes no startup.
    /// </summary>
    public class LaboratorioService
    {
        private const int EsperaMinMs = 60000;   // ~1 min
        private const int EsperaMaxMs = 150000;   // ~2.5 min

        private readonly CasoService _casoService;

        public LaboratorioService(CasoService casoService)
        {
            _casoService = casoService;
        }

        /// <summary>
        /// Envia uma evidencia para analise: Coletada -> NoLab, inicia o delay e, ao concluir,
        /// libera DNA/laudo + notifica + salva. Retorna true se foi aceita para analise.
        /// </summary>
        public bool EnviarParaAnalise(Evidencia evidencia)
        {
            if (evidencia == null)
            {
                Logger.Warn("LaboratorioService.EnviarParaAnalise: evidencia nula.");
                return false;
            }

            if (!evidencia.EnviarAoLab())
            {
                // EnviarAoLab ja loga o motivo (estado diferente de Coletada).
                return false;
            }

            _casoService.Salvar();

            int espera = Aleatorio.Inteiro(EsperaMinMs, EsperaMaxMs);
            string titulo = evidencia.Titulo;
            Logger.Info($"Evidencia '{titulo}' enviada ao lab. Laudo em ~{espera / 1000}s de jogo.");
            Game.DisplayNotification($"~b~LABORATORIO~s~~n~Evidencia \"{titulo}\" recebida. Analise em andamento...");

            GameFiber.StartNew(() =>
            {
                try
                {
                    GameFiber.Sleep(espera);

                    if (evidencia.ConcluirAnalise())
                    {
                        _casoService.Salvar();
                        NotificarLaudo(evidencia);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, $"LaboratorioService/analise '{titulo}'");
                }
            }, "InvestigacaoBR.Lab");

            return true;
        }

        /// <summary>
        /// Conclui imediatamente qualquer analise que ficou em NoLab (ex.: o jogo foi recarregado
        /// enquanto a evidencia estava no lab). Assim nada fica preso. Chamar no startup do sistema.
        /// </summary>
        public void RetomarAnalisesPendentes(IEnumerable<Caso> casos)
        {
            if (casos == null)
            {
                return;
            }

            int concluidas = 0;
            foreach (Caso caso in casos)
            {
                foreach (Evidencia ev in caso.Evidencias)
                {
                    if (ev.Estado == EstadoEvidencia.NoLab && ev.ConcluirAnalise())
                    {
                        concluidas++;
                    }
                }
            }

            if (concluidas > 0)
            {
                _casoService.Salvar();
                Logger.Info($"Lab: {concluidas} analise(s) pendente(s) concluida(s) no startup.");
            }
        }

        private static void NotificarLaudo(Evidencia evidencia)
        {
            string msg = evidencia.PossuiDna
                ? $"~g~LAUDO PRONTO~s~~n~\"{evidencia.Titulo}\": perfil de DNA isolado ({evidencia.PerfilDnaId})."
                : $"~g~LAUDO PRONTO~s~~n~\"{evidencia.Titulo}\": {Resumir(evidencia.ResultadoForense)}";

            Game.DisplayNotification(msg);
            Logger.Info($"Laudo concluido: '{evidencia.Titulo}' (DNA: {(evidencia.PossuiDna ? evidencia.PerfilDnaId : "n/a")}).");
        }

        private static string Resumir(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return "analise concluida.";
            }
            return texto.Length <= 60 ? texto : texto.Substring(0, 57) + "...";
        }
    }
}