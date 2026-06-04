using System;
using System.Collections.Generic;
using Rage;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Emissao de mandados. Ao emitir o mandado de um ped: libera o registro telefonico (via
    /// EmitirMandado), notifica, e cria um blip de rastreamento na localizacao conhecida para o
    /// jogador ir ate la e abordar o suspeito. Mantem e restaura os blips por ped.
    /// </summary>
    public class MandadoService
    {
        private readonly CasoService _casoService;
        private readonly Dictionary<Guid, Blip> _blipsRastreamento = new Dictionary<Guid, Blip>();

        public MandadoService(CasoService casoService)
        {
            _casoService = casoService;
        }

        /// <summary>
        /// Emite o mandado do ped: libera telefone, notifica, cria o blip de rastreamento e salva.
        /// Retorna true se o mandado foi emitido agora (false se ja estava emitido).
        /// </summary>
        public bool Emitir(PedDoCaso ped)
        {
            if (ped == null)
            {
                Logger.Warn("MandadoService.Emitir: ped nulo.");
                return false;
            }

            if (!ped.EmitirMandado())
            {
                // Ja emitido: garante que o blip de rastreamento esteja ativo.
                CriarBlipRastreamento(ped);
                return false;
            }

            _casoService.Salvar();

            string tel = string.IsNullOrEmpty(ped.RegistroTelefonico)
                ? "Sem registros telefonicos relevantes."
                : ped.RegistroTelefonico;
            Game.DisplayNotification($"~p~MANDADO~s~~n~{ped.Nome}: {tel}");

            CriarBlipRastreamento(ped);
            return true;
        }

        private void CriarBlipRastreamento(PedDoCaso ped)
        {
            bool temLocal = !(ped.LocalConhecidoX == 0f && ped.LocalConhecidoY == 0f && ped.LocalConhecidoZ == 0f);
            if (!temLocal)
            {
                Logger.Info($"Mandado de '{ped.Nome}': sem localizacao conhecida para rastrear.");
                return;
            }
            if (_blipsRastreamento.ContainsKey(ped.Id))
            {
                return; // ja existe
            }

            try
            {
                Vector3 local = new Vector3(ped.LocalConhecidoX, ped.LocalConhecidoY, ped.LocalConhecidoZ);
                Blip blip = new Blip(local)
                {
                    Color = System.Drawing.Color.Purple,
                    Scale = 0.9f
                };
                blip.IsRouteEnabled = true;
                _blipsRastreamento[ped.Id] = blip;
                Logger.Info($"Blip de rastreamento criado para '{ped.Nome}'.");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"CriarBlipRastreamento '{ped.Nome}'");
            }
        }

        /// <summary>Remove o blip de rastreamento de um ped (ex.: apos abordar/encerrar).</summary>
        public void RemoverRastreamento(Guid pedId)
        {
            if (_blipsRastreamento.TryGetValue(pedId, out Blip blip))
            {
                if (blip != null)
                {
                    try { blip.Delete(); } catch { }
                }
                _blipsRastreamento.Remove(pedId);
            }
        }

        /// <summary>Remove todos os blips de rastreamento (ex.: ao descarregar o plugin).</summary>
        public void RemoverTodos()
        {
            foreach (Blip blip in _blipsRastreamento.Values)
            {
                if (blip != null)
                {
                    try { blip.Delete(); } catch { }
                }
            }
            _blipsRastreamento.Clear();
        }

        /// <summary>
        /// Recria os blips dos peds que ja tinham mandado emitido (apos reload). Chamar no startup.
        /// </summary>
        public void RestaurarRastreamentos(IEnumerable<Caso> casos)
        {
            if (casos == null)
            {
                return;
            }

            foreach (Caso caso in casos)
            {
                foreach (PedDoCaso ped in caso.Peds)
                {
                    if (ped.MandadoEmitido)
                    {
                        CriarBlipRastreamento(ped);
                    }
                }
            }
        }
    }
}