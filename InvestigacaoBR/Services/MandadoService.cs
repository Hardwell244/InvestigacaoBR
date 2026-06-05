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
        private readonly Dictionary<Guid, List<Ped>> _associados = new Dictionary<Guid, List<Ped>>();

        public MandadoService(CasoService casoService) { _casoService = casoService; }

        public bool Emitir(PedDoCaso ped)
        {
            if (ped == null) { Logger.Warn("MandadoService.Emitir: ped nulo."); return false; }

            if (!ped.EmitirMandado()) { CriarBlipRastreamento(ped); return false; }

            _casoService.Salvar();

            string tel = string.IsNullOrEmpty(ped.RegistroTelefonico)
                ? "Sem registros telefonicos relevantes."
                : ped.RegistroTelefonico;

            Notificacao.Mandado($"{ped.Nome}: {tel}");
            CriarBlipRastreamento(ped);
            SpawnarAssociadosAsync(ped);
            return true;
        }

        private void CriarBlipRastreamento(PedDoCaso ped)
        {
            bool temLocal = !(ped.LocalConhecidoX == 0f && ped.LocalConhecidoY == 0f && ped.LocalConhecidoZ == 0f);
            if (!temLocal) { Logger.Info($"Mandado de '{ped.Nome}': sem localizacao conhecida para rastrear."); return; }
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

        private void SpawnarAssociadosAsync(PedDoCaso ped)
        {
            bool temLocal = !(ped.LocalConhecidoX == 0f && ped.LocalConhecidoY == 0f && ped.LocalConhecidoZ == 0f);
            if (!temLocal || _associados.ContainsKey(ped.Id)) return;

            Guid pedId = ped.Id;
            string nome = ped.Nome;
            Vector3 local = new Vector3(ped.LocalConhecidoX, ped.LocalConhecidoY, ped.LocalConhecidoZ);

            GameFiber.StartNew(() =>
            {
                try
                {
                    GameFiber.Sleep(5000); // FIX: Sleep previsivel em vez de loop FPS-dependente

                    List<Ped> spawned = new List<Ped>();
                    int qtd = Aleatorio.Inteiro(1, 3);
                    Logger.Info($"Spawnando {qtd} associado(s) de '{nome}'.");

                    for (int i = 0; i < qtd; i++)
                    {
                        double ang = (Math.PI * 2.0 / Math.Max(1, qtd)) * i + Aleatorio.Real(-0.3f, 0.3f);
                        float raio = Aleatorio.Real(2f, 5f);
                        Vector3 pos = local + new Vector3((float)(Math.Cos(ang) * raio), (float)(Math.Sin(ang) * raio), 0f);

                        Ped assoc = new Ped(new Model(Aleatorio.Item(PoolsCaso.ModelosSuspeito)), pos, Aleatorio.Real(0f, 360f));
                        GameFiber.Yield();
                        if (assoc == null || !assoc.Exists()) continue;

                        assoc.IsPersistent = true;
                        assoc.BlockPermanentEvents = false;
                        assoc.Tasks.Wander();
                        spawned.Add(assoc);
                    }

                    if (spawned.Count > 0)
                    {
                        _associados[pedId] = spawned;
                        Notificacao.Mandado($"Associados de {nome} localizados. Dirija-se ao ponto roxo.");
                        Logger.Info($"{spawned.Count} associado(s) spawnado(s) para '{nome}'.");
                    }
                }
                catch (Exception ex) { Logger.Exception(ex, $"SpawnarAssociadosAsync '{nome}'"); }
            }, "InvestigacaoBR.Associados");
        }

        public void RemoverRastreamento(Guid pedId)
        {
            if (_blipsRastreamento.TryGetValue(pedId, out Blip blip))
            {
                if (blip != null) try { blip.Delete(); } catch { }
                _blipsRastreamento.Remove(pedId);
            }

            if (_associados.TryGetValue(pedId, out List<Ped> assocs))
            {
                foreach (Ped a in assocs)
                    try
                    {
                        if (a != null && a.Exists())
                        {
                            a.Tasks.Clear();
                            a.Dismiss(); // FIX: Dismiss() libera corretamente, nao apenas desmarca persistent
                        }
                    }
                    catch (Exception ex) { Logger.Exception(ex, "RemoverRastreamento/associado"); }
                _associados.Remove(pedId);
            }
        }

        public void RemoverTodos()
        {
            foreach (Blip b in _blipsRastreamento.Values)
                if (b != null) try { b.Delete(); } catch { }
            _blipsRastreamento.Clear();

            foreach (List<Ped> assocs in _associados.Values)
                foreach (Ped a in assocs)
                    try
                    {
                        if (a != null && a.Exists()) { a.Tasks.Clear(); a.Dismiss(); }
                    }
                    catch { }
            _associados.Clear();
        }

        /// <summary>
        /// FIX CRITICO: so restaura blips de casos ABERTOS.
        /// Antes restaurava blips de casos ja RESOLVIDOS/ARQUIVADOS, enchendo o mapa.
        /// </summary>
        public void RestaurarRastreamentos(IEnumerable<Caso> casos)
        {
            if (casos == null) return;
            foreach (Caso caso in casos)
            {
                if (caso.Status != StatusCaso.Aberto) continue; // ignorar resolvidos/arquivados
                foreach (PedDoCaso ped in caso.Peds)
                    if (ped.MandadoEmitido) CriarBlipRastreamento(ped);
            }
        }
    }
}