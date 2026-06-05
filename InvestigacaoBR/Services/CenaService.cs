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
            public Vector3 Origem;
            public readonly List<Rage.Object> PropsCenario = new List<Rage.Object>();
        }

        private readonly Dictionary<Guid, CenaAtiva> _cenas = new Dictionary<Guid, CenaAtiva>();

        public CenaService(PersonaService personaService)
        {
            _personaService = personaService ?? new PersonaService();
        }

        private static Vector3 OrigemCena(Caso caso) => new Vector3(caso.CenaX, caso.CenaY, caso.CenaZ);

        // ===== SPAWN =====

        public void SpawnarCena(Caso caso)
        {
            if (caso == null) { Logger.Warn("SpawnarCena: caso nulo."); return; }
            if (_cenas.ContainsKey(caso.Id)) { Logger.Info($"SpawnarCena ignorado: cena '{caso.Titulo}' ja montada."); return; }

            Logger.Info($"Montando cena do caso '{caso.Titulo}'...");
            Vector3 origem = OrigemCena(caso);
            CenaAtiva cena = new CenaAtiva { Origem = origem };

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

            if (pedDoCaso.NaoSpawnarNaCena) return; // culpado ausente da cena (fugiu)

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
                    SpawnarDecalSangue(pos);
                    Logger.Info($"Ped '{pedDoCaso.Nome}' spawnado MORTO na cena.");
                }
                else
                {
                    // G1: comportamento baseado no papel do ped
                    AtribuirComportamento(ped, pedDoCaso);
                    Logger.Info($"Ped '{pedDoCaso.Nome}' spawnado na cena [{pedDoCaso.Role}].");
                }
            }
            catch (Exception ex) { Logger.Exception(ex, $"SpawnarPed '{pedDoCaso.Nome}'"); }
        }

        private static void AtribuirComportamento(Ped ped, PedDoCaso pedDoCaso)
        {
            try
            {
                if (pedDoCaso.EhCulpadoReal)
                {
                    // Suspeito: anda nervosamente, mas sem fugir ainda (fuga so quando jogador chega perto)
                    ped.Tasks.Wander();
                }
                else if (pedDoCaso.Role == RolePed.Testemunha)
                {
                    // Testemunha: para no lugar, nervosa
                    ped.Tasks.StandStill(int.MaxValue);
                }
                else
                {
                    // Civil / indefinido: anda normalmente
                    ped.Tasks.Wander();
                }
            }
            catch (Exception ex) { Logger.Exception(ex, $"AtribuirComportamento '{pedDoCaso.Nome}'"); }
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
                prop.Rotation = new Rotator(0f, 0f, Aleatorio.Real(0f, 360f));
                AssentarNoChaoAsync(prop, pos.Z);

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
                        AssentarNoChaoAsync(barreira, pos.Z);
                        cena.PropsCenario.Add(barreira);
                    }
                }
                Logger.Info($"Fita de isolamento spawnada no caso '{caso.Titulo}'.");
            }
            catch (Exception ex) { Logger.Exception(ex, "SpawnarFitaIsolamento"); }
        }

        // ===== LIMPEZA =====

        public void LimparCena(Caso caso)
        {
            if (caso == null) return;
            Logger.Info($"Limpando visuais de '{caso.Titulo}'...");

            foreach (PedDoCaso pedDoCaso in caso.Peds)
            {
                if (pedDoCaso.EstaSpawnado)
                {
                    try
                    {
                        // Libera como ambient — continuam no mundo
                        pedDoCaso.PedVivo.IsPersistent = false;
                        if (!pedDoCaso.SpawnarMorto)
                            pedDoCaso.PedVivo.BlockPermanentEvents = false;
                    }
                    catch (Exception ex) { Logger.Exception(ex, "LimparCena/ped"); }
                }
                pedDoCaso.PedVivo = null;
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

            Logger.Info($"Visuais de '{caso.Titulo}' removidos. Peds liberados como ambient.");
        }

        public void RemoverCenaCompleta(Caso caso)
        {
            if (caso == null) return;
            LimparCena(caso);
            _cenas.Remove(caso.Id);
            Logger.Info($"Cena de '{caso.Titulo}' removida por completo.");
        }

        public bool CenaMontada(Guid casoId) => _cenas.ContainsKey(casoId);

        public void ProcessarBlipsProximidade(Vector3 posJogador, float raio = 10f)
        {
            foreach (var kvp in _cenas)
            {
                CenaAtiva cena = kvp.Value;
                if (cena.Blip == null || !cena.Blip.Exists()) continue;
                if (Vector3.Distance(posJogador, cena.Origem) <= raio)
                {
                    try { cena.Blip.IsRouteEnabled = false; cena.Blip.Delete(); } catch { }
                    cena.Blip = null;
                }
            }
        }

        // ===== HELPERS =====

        private static void AssentarNoChaoAsync(Rage.Object prop, float zOriginal)
        {
            GameFiber.StartNew(() =>
            {
                try
                {
                    int[] intervalos = { 60, 60, 120, 300, 600 };
                    foreach (int espera in intervalos)
                    {
                        for (int i = 0; i < espera; i++)
                        {
                            GameFiber.Yield();
                            if (prop == null || !prop.Exists()) return;
                        }
                        NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(prop);
                        GameFiber.Yield();
                        if (prop == null || !prop.Exists()) return;
                        if (Math.Abs(prop.Position.Z - zOriginal) > 0.1f)
                        {
                            prop.IsPositionFrozen = true;
                            return;
                        }
                    }
                    if (prop != null && prop.Exists()) prop.IsPositionFrozen = true;
                }
                catch (Exception ex) { Logger.Exception(ex, "AssentarNoChaoAsync"); }
            }, "InvestigacaoBR.AssentarProp");
        }

        private static void SpawnarDecalSangue(Vector3 pos)
        {
            try
            {
                NativeFunction.Natives.ADD_DECAL<int>(
                    14, pos.X, pos.Y, pos.Z,
                    0f, 1f, 0f, 0f, 0f, 1f,
                    1.5f, 1.5f,
                    0.8f, 0f, 0f, 1f,
                    false, false);
            }
            catch (Exception ex) { Logger.Exception(ex, "SpawnarDecalSangue"); }
        }
    }
}