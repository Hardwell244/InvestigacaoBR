using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Diario cronologico dos casos. Classe estatica (como Logger) — basta chamar
    /// TimelineService.Registrar(...) de qualquer lugar apos Inicializar().
    /// As entradas ficam em Caso.Timeline e sao salvas junto com o caso no proximo Salvar().
    /// </summary>
    public static class TimelineService
    {
        private static CasoService _casoService;

        public static void Inicializar(CasoService casoService)
        {
            _casoService = casoService;
            Logger.Info("TimelineService inicializado.");
        }

        /// <summary>
        /// Adiciona uma entrada ao diario do caso.
        /// Tipo: "SISTEMA" | "DETETIVE" | "LAB" | "MANDADO" | "PARCEIRO"
        /// </summary>
        public static void Registrar(Guid casoId, string texto, string tipo = "SISTEMA")
        {
            if (_casoService == null || casoId == Guid.Empty) return;
            try
            {
                Caso caso = _casoService.ObterPorId(casoId);
                if (caso == null) return;

                caso.Timeline.Add(new TimelineEntry(TempoJogo.Agora(), tipo, texto));
            }
            catch (Exception ex) { Logger.Exception(ex, $"TimelineService.Registrar '{tipo}'"); }
        }

        /// <summary>Busca o casoId de uma evidencia iterando os casos em memoria.</summary>
        public static Guid EncontrarCasoPorEvidencia(Evidencia evidencia)
        {
            if (_casoService == null || evidencia == null) return Guid.Empty;
            foreach (Caso c in _casoService.ObterTodos())
                foreach (Evidencia e in c.Evidencias)
                    if (e == evidencia) return c.Id;
            return Guid.Empty;
        }
    }
}