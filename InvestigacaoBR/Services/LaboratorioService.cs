using System;
using System.Collections.Generic;
using Rage;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    public class LaboratorioService
    {
        private static int EsperaMinMs => Config.DelayLabMinMs;
        private static int EsperaMaxMs => Config.DelayLabMaxMs;

        private readonly CasoService _casoService;

        public LaboratorioService(CasoService casoService) { _casoService = casoService; }

        public bool EnviarParaAnalise(Evidencia evidencia)
        {
            if (evidencia == null) { Logger.Warn("LaboratorioService.EnviarParaAnalise: nula."); return false; }
            if (!evidencia.EnviarAoLab()) return false;

            _casoService.Salvar();

            int espera = Aleatorio.Inteiro(EsperaMinMs, EsperaMaxMs);
            string titulo = evidencia.Titulo;
            Logger.Info($"Evidencia '{titulo}' enviada ao lab. Laudo em ~{espera / 1000}s.");
            Notificacao.Lab($"\"{titulo}\" recebida. Analise em andamento...");

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
                catch (Exception ex) { Logger.Exception(ex, $"LaboratorioService/analise '{titulo}'"); }
            }, "InvestigacaoBR.Lab");

            return true;
        }

        public void RetomarAnalisesPendentes(IEnumerable<Caso> casos)
        {
            if (casos == null) return;
            int concluidas = 0;
            foreach (Caso caso in casos)
                foreach (Evidencia ev in caso.Evidencias)
                    if (ev.Estado == EstadoEvidencia.NoLab && ev.ConcluirAnalise())
                        concluidas++;

            if (concluidas > 0) { _casoService.Salvar(); Logger.Info($"Lab: {concluidas} pendente(s) concluida(s) no startup."); }
        }

        private static void NotificarLaudo(Evidencia evidencia)
        {
            string msg = evidencia.PossuiDna
                ? $"\"{evidencia.Titulo}\": perfil de DNA isolado ({evidencia.PerfilDnaId})."
                : $"\"{evidencia.Titulo}\": {Resumir(evidencia.ResultadoForense)}";
            Notificacao.Lab(msg);
            Logger.Info($"Laudo: '{evidencia.Titulo}' (DNA: {(evidencia.PossuiDna ? evidencia.PerfilDnaId : "n/a")}).");
        }

        private static string Resumir(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "analise concluida.";
            return texto.Length <= 60 ? texto : texto.Substring(0, 57) + "...";
        }
    }
}