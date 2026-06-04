using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    public class GeradorCasos
    {
        private const int MetaDisponiveis = 3;

        private readonly CasoService _casoService;
        private readonly System.Collections.Generic.List<GeradorBase> _geradores;

        public GeradorCasos(CasoService casoService)
        {
            _casoService = casoService;
            _geradores = new System.Collections.Generic.List<GeradorBase>
            {
                new GeradorHomicidio(),
                new GeradorTrafico(),
                new GeradorRouboCarga()
            };
        }

        /// <summary>Tempo in-game seguro — delega ao TempoJogo para evitar duplicar o try/catch.</summary>
        private static DateTime AgoraInGame() => TempoJogo.Agora();

        public Caso GerarCasoAleatorio()
        {
            GeradorBase gerador = Aleatorio.Item(_geradores);
            if (gerador == null)
            {
                Logger.Warn("GerarCasoAleatorio: nenhum gerador registrado.");
                return null;
            }
            return gerador.Gerar(AgoraInGame());
        }

        /// <summary>Garante que o pool tenha pelo menos MetaDisponiveis casos disponiveis.</summary>
        public void GarantirPool()
        {
            int disponiveis = 0;
            foreach (Caso c in _casoService.ObterDisponiveis()) disponiveis++;

            if (disponiveis >= MetaDisponiveis) return;

            int aGerar = MetaDisponiveis - disponiveis;
            Logger.Info($"Pool com {disponiveis} disponivel(is); gerando {aGerar} para atingir {MetaDisponiveis}.");

            for (int i = 0; i < aGerar; i++)
            {
                Caso novo = GerarCasoAleatorio();
                if (novo != null) _casoService.AdicionarCaso(novo);
            }
        }
    }
}