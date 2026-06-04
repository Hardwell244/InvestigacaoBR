using System;
using System.Collections.Generic;
using Rage;
using Rage.Native;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    public class CenaService
    {
        private readonly PersonaService _personaService;

        private class CenaAtiva
        {
            public Blip Blip;
            public readonly List<Rage.Object> PropsCenario = new List<Rage.Object>();
        }

        private readonly Dictionary<Guid, CenaAtiva> _cenas = new Dictionary<Guid, CenaAtiva>();

        public CenaService(PersonaService personaService)
        {
            _personaService = personaService ?? new PersonaService();
        }

        private static Vector3 OrigemCena(Caso caso)
            => new Vector3(caso.CenaX, caso.CenaY, caso.CenaZ);

        // ===== SPAWN =====

        public void SpawnarCena(Caso caso)
        {
            if (caso == null) { Logger.Warn("SpawnarCena: caso nulo."); return; }
            if (_cenas.ContainsKey(caso.Id))
            {
                Logger.Info($"SpawnarCena ignorado: cena '{caso.Titulo}' ja montada.");
                return;
            }

            Logger.Info($"Montando cena do caso '{caso.Titulo}'...");
            Vector3 origem = OrigemCena(caso);
            CenaAtiva cena = new CenaAtiva();

            try
            {
                cena.Blip = new Blip(origem) { Color = System.Drawing.Color.OrangeRed, Scale = 0.9f };
                cena.Blip.IsRouteEnabled = true;
            }
            catch (Exception ex) { Logger.Exception(ex, "SpawnarCena/Blip"); }

            foreach (PedDoCaso p in caso.Peds) SpawnarPed(p, origem);
            foreach (Evidencia ev in caso.Evidencias) SpawnarEvidencia(ev, origem);

            _cenas[caso.Id] = cena;
            Logger.Info($"Cena '{caso.Titulo}' montada. Peds: {caso.Peds.Count}, evidencias: {caso.Evidencias.Count}.");
        }

        private void SpawnarPed(PedDoCaso pedDoCaso, Vector3 origem)
        {
            if (pedDoCaso == null || string.IsNullOrEmpty(pedDoCaso.ModeloPed)) return;
            try
            {
                Vector3 pos = origem + new Vector3(pedDoCaso.OffsetX, pedDoCaso.OffsetY, pedDoCaso.OffsetZ);
                Ped ped = new Ped(new Model(pedDoCaso.ModeloPed), pos, pedDoCaso.Heading);

                if (ped == null || !ped.Exists())
                {
                    Logger.Warn($"Falha ao spawnar ped '{pedDoCaso.Nome}' (modelo '{pedDoCaso.ModeloPed}').");
                    return;
                }

                ped.IsPersistent = true;
                ped.BlockPermanentEvents = true;
                pedDoCaso.PedVivo = ped;

                _personaService.AplicarIdentidade(ped, pedDoCaso);

                if (pedDoCaso.SpawnarMorto)
                {
                    ped.Kill();
                    SpawnarDecalSangue(pos); // fix #6: poca de sangue no chao
                    Logger.Info($"Ped '{pedDoCaso.Nome}' spawnado MORTO na cena.");
                }
                else
                {
                    Logger.Info($"Ped '{pedDoCaso.Nome}' spawnado na cena.");
                }
            }
            catch (Exception ex) { Logger.Exception(ex, $"SpawnarPed '{pedDoCaso.Nome}'"); }
        }

        private void SpawnarEvidencia(Evidencia ev, Vector3 origem)
        {
            if (ev == null || string.IsNullOrEmpty(ev.ModeloProp)) return;
            try
            {
                Vector3 pos = origem + new Vector3(ev.OffsetX, ev.OffsetY, ev.OffsetZ);
                Rage.Object prop = new Rage.Object(new Model(ev.ModeloProp), pos);

                if (prop == null || !prop.Exists())
                {
                    Logger.Warn($"Falha ao spawnar prop '{ev.Titulo}' (modelo '{ev.ModeloProp}').");
                    return;
                }

                prop.IsPersistent = true;

                // fix #4: rotacao aleatoria no plano, assenta no chao, congela para nao flutuar
                prop.Rotation = new Rotator(0f, 0f, Aleatorio.Real(0f, 360f));
                NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(prop);
                prop.IsPositionFrozen = true;

                ev.PropVivo = prop;
                Logger.Info($"Prop '{ev.Titulo}' spawnado.");
            }
            catch (Exception ex) { Logger.Exception(ex, $"SpawnarEvidencia '{ev.Titulo}'"); }
        }

        public void SpawnarFitaIsolamento(Caso caso)
        {
            if (caso == null) return;
            if (!_cenas.TryGetValue(caso.Id, out CenaAtiva cena))
            {
                Logger.Warn($"SpawnarFitaIsolamento: cena '{caso.Titulo}' nao montada.");
                return;
            }
            try
            {
                Vector3 origem = OrigemCena(caso);
                const string modelo = "prop_barrier_work05";
                const int qtd = 6;
                const float raio = 5f;

                for (int i = 0; i < qtd; i++)
                {
                    double ang = (Math.PI * 2.0 / qtd) * i;
                    Vector3 pos = origem + new Vector3((float)(Math.Cos(ang) * raio), (float)(Math.Sin(ang) * raio), 0f);
                    Rage.Object barreira = new Rage.Object(new Model(modelo), pos);
                    if (barreira != null && barreira.Exists())
                    {
                        barreira.IsPersistent = true;
                        NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(barreira);
                        barreira.IsPositionFrozen = true;
                        cena.PropsCenario.Add(barreira);
                    }
                }
                Logger.Info($"Fita de isolamento spawnada no caso '{caso.Titulo}'.");
            }
            catch (Exception ex) { Logger.Exception(ex, "SpawnarFitaIsolamento"); }
        }

        // ===== LIMPEZA =====

        /// <summary>
        /// Remove TODOS os visuais: peds, props, fita E o blip da cena.
        /// Chamado pela tecla END. Os dados do caso continuam vivos no CasoService.
        /// fix #2: blip do caso agora some junto com o resto.
        /// </summary>
        public void LimparCena(Caso caso)
        {
            if (caso == null) return;
            Logger.Info($"Limpando visuais de '{caso.Titulo}'...");

            foreach (PedDoCaso ped in caso.Peds)
            {
                if (ped.EstaSpawnado)
                    try { ped.PedVivo.Delete(); } catch (Exception ex) { Logger.Exception(ex, "LimparCena/ped"); }
                ped.PedVivo = null;
            }

            foreach (Evidencia ev in caso.Evidencias)
            {
                if (ev.PropVivo != null && ev.PropVivo.Exists())
                    try { ev.PropVivo.Delete(); } catch (Exception ex) { Logger.Exception(ex, "LimparCena/prop"); }
                ev.PropVivo = null;
            }

            if (_cenas.TryGetValue(caso.Id, out CenaAtiva cena))
            {
                foreach (Rage.Object prop in cena.PropsCenario)
                    if (prop != null && prop.Exists())
                        try { prop.Delete(); } catch (Exception ex) { Logger.Exception(ex, "LimparCena/cenario"); }
                cena.PropsCenario.Clear();

                if (cena.Blip != null)
                {
                    try { cena.Blip.Delete(); } catch { }
                    cena.Blip = null;
                }
            }

            Logger.Info($"Visuais de '{caso.Titulo}' removidos. Dados preservados.");
        }

        /// <summary>Remove cena + dados internos. Blip já é apagado pelo LimparCena.</summary>
        public void RemoverCenaCompleta(Caso caso)
        {
            if (caso == null) return;
            LimparCena(caso);
            _cenas.Remove(caso.Id);
            Logger.Info($"Cena de '{caso.Titulo}' removida por completo.");
        }

        public bool CenaMontada(Guid casoId) => _cenas.ContainsKey(casoId);

        // ===== HELPERS =====

        /// <summary>
        /// fix #6: decal de poca de sangue no chao.
        /// Tipo 8 = blood pool. Se nao aparecer, tente 1 ou 9.
        /// </summary>
        private static void SpawnarDecalSangue(Vector3 pos)
        {
            try
            {
                NativeFunction.Natives.ADD_DECAL<int>(
                    8,
                    pos.X, pos.Y, pos.Z,
                    0f, 1f, 0f,
                    0f, 0f, 1f,
                    1.5f, 1.5f,
                    0.8f, 0f, 0f, 1f,
                    false, false);
            }
            catch (Exception ex) { Logger.Exception(ex, "SpawnarDecalSangue"); }
        }
    }
}