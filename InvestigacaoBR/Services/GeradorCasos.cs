using System;
using System.Collections.Generic;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    public class GeradorCasos
    {

        private readonly CasoService _casoService;
        private readonly List<GeradorBase> _geradores;

        public GeradorCasos(CasoService casoService)
        {
            _casoService = casoService;
            _geradores = new List<GeradorBase>
            {
                // --- Fase 1 (casos originais) ---
                new GeradorHomicidio(),
                new GeradorTrafico(),
                new GeradorRouboCarga(),

                // --- Fase 3 parte 1 ---
                new GeradorChacina(),
                new GeradorTraficoArmas(),
                new GeradorLaboratorio(),
                new GeradorLatrocinio(),

                // --- Fase 3 parte 2 ---
                new GeradorSequestro(),
                new GeradorIncendio(),
                new GeradorLavagem(),
                new GeradorInvasao(),
                new GeradorRouboVeiculo(),
                new GeradorAssassinatoPolicial()
            };
        }

        private static DateTime AgoraInGame() => TempoJogo.Agora();

        public Caso GerarCasoAleatorio()
        {
            GeradorBase gerador = Aleatorio.Item(_geradores);
            if (gerador == null) { Logger.Warn("GerarCasoAleatorio: nenhum gerador."); return null; }
            return gerador.Gerar(AgoraInGame());
        }

        public void GarantirPool()
        {
            int disponiveis = 0;
            foreach (Caso c in _casoService.ObterDisponiveis()) disponiveis++;
            if (disponiveis >= Config.MetaCasosPool) return;

            int aGerar = Config.MetaCasosPool - disponiveis;
            Logger.Info($"Pool com {disponiveis} disponivel(is); gerando {aGerar}.");
            for (int i = 0; i < aGerar; i++)
            {
                Caso novo = GerarCasoAleatorio();
                if (novo != null) _casoService.AdicionarCaso(novo);
            }
        }
    }
}