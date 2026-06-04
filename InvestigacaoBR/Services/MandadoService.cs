using System;
using System.Collections.Generic;
using Rage;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    public class MandadoService
    {
        private readonly CasoService _casoService;
        private readonly Dictionary<Guid, Blip> _blipsRastreamento = new Dictionary<Guid, Blip>();

        public MandadoService(CasoService casoService) { _casoService = casoService; }

        public bool Emitir(PedDoCaso ped)
        {
            if (ped == null) { Logger.Warn("MandadoService.Emitir: ped nulo."); return false; }

            if (!ped.EmitirMandado())
            {
                CriarBlipRastreamento(ped);
                return false;
            }

            _casoService.Salvar();

            string tel = string.IsNullOrEmpty(ped.RegistroTelefonico)
                ? "Sem registros telefonicos relevantes."
                : ped.RegistroTelefonico;

            Notificacao.Mandado($"{ped.Nome}: {tel}");
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
            if (_blipsRastreamento.ContainsKey(ped.Id)) return;

            try
            {
                Vector3 local = new Vector3(ped.LocalConhecidoX, ped.LocalConhecidoY, ped.LocalConhecidoZ);
                Blip blip = new Blip(local) { Color = System.Drawing.Color.Purple, Scale = 0.9f };
                blip.IsRouteEnabled = true;
                _blipsRastreamento[ped.Id] = blip;
                Logger.Info($"Blip de rastreamento criado para '{ped.Nome}'.");
            }
            catch (Exception ex) { Logger.Exception(ex, $"CriarBlipRastreamento '{ped.Nome}'"); }
        }

        public void RemoverRastreamento(Guid pedId)
        {
            if (_blipsRastreamento.TryGetValue(pedId, out Blip blip))
            {
                if (blip != null) try { blip.Delete(); } catch { }
                _blipsRastreamento.Remove(pedId);
            }
        }

        public void RemoverTodos()
        {
            foreach (Blip blip in _blipsRastreamento.Values)
                if (blip != null) try { blip.Delete(); } catch { }
            _blipsRastreamento.Clear();
        }

        public void RestaurarRastreamentos(IEnumerable<Caso> casos)
        {
            if (casos == null) return;
            foreach (Caso caso in casos)
                foreach (PedDoCaso ped in caso.Peds)
                    if (ped.MandadoEmitido) CriarBlipRastreamento(ped);
        }
    }
}