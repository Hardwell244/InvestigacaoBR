using System;
using System.Collections.Generic;
using System.Linq;
using Rage;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Orquestrador da geracao de casos. Mantem a lista de geradores, semeia o pool de casos
    /// Disponivel ate a meta e repoe quando um e aceito. Pega o tempo IN-GAME (World.DateTime)
    /// para passar aos geradores. Persiste via CasoService.
    /// </summary>
    public class GeradorCasos
    {
        private const int MetaDisponiveis = 3;

        private readonly CasoService _casoService;
        private readonly List<GeradorBase> _geradores;

        public GeradorCasos(CasoService casoService)
        {
            _casoService = casoService;
            _geradores = new List<GeradorBase>
            {
                new GeradorHomicidio(),
                new GeradorTrafico(),
                new GeradorRouboCarga()
            };
        }

        /// <summary>Tempo in-game atual do LSPDFR/RPH (com fallback seguro).</summary>
        private static DateTime AgoraInGame()
        {
            try
            {
                return World.DateTime;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "GeradorCasos.AgoraInGame (fallback para DateTime.Now)");
                return DateTime.Now;
            }
        }

        /// <summary>Gera UM caso de tipo aleatorio (status Disponivel) e o devolve, sem adicionar ao pool.</summary>
        public Caso GerarCasoAleatorio()
        {
            GeradorBase gerador = Aleatorio.Item(_geradores);
            if (gerador == null)
            {
                Logger.Warn("GerarCasoAleatorio: nenhum gerador registrado.");
                return null;
            }

            Caso caso = gerador.Gerar(AgoraInGame());
            Logger.Info($"Orquestrador gerou: '{caso?.Titulo}'.");
            return caso;
        }

        /// <summary>
        /// Garante que o pool tenha pelo menos MetaDisponiveis casos Disponivel. Gera o que faltar
        /// (com variedade de tipos) e adiciona ao CasoService, que persiste. Chamar ao iniciar o
        /// sistema e apos cada aceite, para o "pegar casos" nunca ficar vazio.
        /// </summary>
        public void GarantirPool()
        {
            int disponiveis = _casoService.ObterDisponiveis().Count();
            int faltam = MetaDisponiveis - disponiveis;

            if (faltam <= 0)
            {
                Logger.Info($"Pool ok: {disponiveis} caso(s) disponivel(is). Nada a gerar.");
                return;
            }

            Logger.Info($"Pool com {disponiveis} disponivel(is); gerando {faltam} para atingir {MetaDisponiveis}.");

            // Ordem embaralhada dos geradores -> da variedade de tipos ao semear varios de uma vez.
            List<GeradorBase> ordem = new List<GeradorBase>(_geradores);
            Aleatorio.Embaralhar(ordem);

            for (int i = 0; i < faltam; i++)
            {
                GeradorBase gerador = ordem[i % ordem.Count];
                Caso caso = gerador.Gerar(AgoraInGame());
                if (caso != null)
                {
                    _casoService.AdicionarCaso(caso);
                }
            }
        }
    }
}