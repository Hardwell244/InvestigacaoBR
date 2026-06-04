using System;
using System.Collections.Generic;
using Rage;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Motor fisico da cena do crime. Cria o blip, spawna peds e props de evidencia (posicao =
    /// origem da cena + offset), spawna a fita de isolamento, e limpa os visuais pela tecla END
    /// mantendo o caso ativo. Tudo logado e protegido por try/catch por entidade — um modelo
    /// invalido nao derruba a cena inteira.
    /// </summary>
    public class CenaService
    {
        private readonly PersonaService _personaService;

        /// <summary>Entidades de cenario de um caso montado (blip + props avulsos como a fita).</summary>
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
        {
            return new Vector3(caso.CenaX, caso.CenaY, caso.CenaZ);
        }

        /// <summary>
        /// Monta a cena fisica do caso: blip + peds (autorados, mortos inclusive) + props de
        /// evidencia. Idempotente: nao remonta se a cena ja existir.
        /// </summary>
        public void SpawnarCena(Caso caso)
        {
            if (caso == null)
            {
                Logger.Warn("SpawnarCena: caso nulo.");
                return;
            }
            if (_cenas.ContainsKey(caso.Id))
            {
                Logger.Info($"SpawnarCena ignorado: cena do caso '{caso.Titulo}' ja montada.");
                return;
            }

            Logger.Info($"Montando cena do caso '{caso.Titulo}'...");
            Vector3 origem = OrigemCena(caso);
            CenaAtiva cena = new CenaAtiva();

            try
            {
                cena.Blip = new Blip(origem)
                {
                    Color = System.Drawing.Color.OrangeRed,
                    Scale = 0.9f
                };
                cena.Blip.IsRouteEnabled = true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "SpawnarCena/Blip");
            }

            foreach (PedDoCaso pedDoCaso in caso.Peds)
            {
                SpawnarPed(pedDoCaso, origem);
            }

            foreach (Evidencia ev in caso.Evidencias)
            {
                SpawnarEvidencia(ev, origem);
            }

            _cenas[caso.Id] = cena;
            Logger.Info($"Cena do caso '{caso.Titulo}' montada. Peds: {caso.Peds.Count}, evidencias: {caso.Evidencias.Count}.");
        }

        private void SpawnarPed(PedDoCaso pedDoCaso, Vector3 origem)
        {
            if (pedDoCaso == null || string.IsNullOrEmpty(pedDoCaso.ModeloPed))
            {
                return;
            }

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

                // Grava a identidade autorada no LSPDFR (best-effort; nao quebra se falhar).
                _personaService.AplicarIdentidade(ped, pedDoCaso);

                if (pedDoCaso.SpawnarMorto)
                {
                    ped.Kill();
                    Logger.Info($"Ped '{pedDoCaso.Nome}' spawnado MORTO na cena.");
                }
                else
                {
                    Logger.Info($"Ped '{pedDoCaso.Nome}' spawnado na cena.");
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"SpawnarPed '{pedDoCaso.Nome}'");
            }
        }

        private void SpawnarEvidencia(Evidencia ev, Vector3 origem)
        {
            if (ev == null || string.IsNullOrEmpty(ev.ModeloProp))
            {
                return;
            }

            try
            {
                Vector3 pos = origem + new Vector3(ev.OffsetX, ev.OffsetY, ev.OffsetZ);
                Rage.Object prop = new Rage.Object(new Model(ev.ModeloProp), pos);

                if (prop == null || !prop.Exists())
                {
                    Logger.Warn($"Falha ao spawnar prop da evidencia '{ev.Titulo}' (modelo '{ev.ModeloProp}').");
                    return;
                }

                prop.IsPersistent = true;
                ev.PropVivo = prop;
                Logger.Info($"Prop da evidencia '{ev.Titulo}' spawnado.");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"SpawnarEvidencia '{ev.Titulo}'");
            }
        }

        /// <summary>
        /// Spawna a fita de isolamento em circulo ao redor da origem. Chamado quando o detetive
        /// isola a area. Requer que a cena ja esteja montada.
        /// </summary>
        public void SpawnarFitaIsolamento(Caso caso)
        {
            if (caso == null)
            {
                return;
            }
            if (!_cenas.TryGetValue(caso.Id, out CenaAtiva cena))
            {
                Logger.Warn($"SpawnarFitaIsolamento: cena do caso '{caso.Titulo}' nao esta montada.");
                return;
            }

            try
            {
                Vector3 origem = OrigemCena(caso);
                const string modeloFita = "prop_barrier_work05";
                const int qtd = 6;
                const float raio = 5f;

                for (int i = 0; i < qtd; i++)
                {
                    double ang = (Math.PI * 2.0 / qtd) * i;
                    Vector3 pos = origem + new Vector3((float)(Math.Cos(ang) * raio), (float)(Math.Sin(ang) * raio), 0f);
                    Rage.Object barreira = new Rage.Object(new Model(modeloFita), pos);
                    if (barreira != null && barreira.Exists())
                    {
                        barreira.IsPersistent = true;
                        cena.PropsCenario.Add(barreira);
                    }
                }

                Logger.Info($"Fita de isolamento spawnada no caso '{caso.Titulo}'.");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "SpawnarFitaIsolamento");
            }
        }

        /// <summary>
        /// LIMPEZA DA TECLA END: remove TODOS os visuais do mundo (peds, props de evidencia,
        /// fita) MAS mantem o blip e os dados. O caso continua Aberto para investigacao.
        /// </summary>
        public void LimparCena(Caso caso)
        {
            if (caso == null)
            {
                return;
            }

            Logger.Info($"Limpando visuais da cena do caso '{caso.Titulo}' (caso permanece ativo)...");

            foreach (PedDoCaso ped in caso.Peds)
            {
                if (ped.EstaSpawnado)
                {
                    try { ped.PedVivo.Delete(); }
                    catch (Exception ex) { Logger.Exception(ex, "LimparCena/ped"); }
                }
                ped.PedVivo = null;
            }

            foreach (Evidencia ev in caso.Evidencias)
            {
                if (ev.PropVivo != null && ev.PropVivo.Exists())
                {
                    try { ev.PropVivo.Delete(); }
                    catch (Exception ex) { Logger.Exception(ex, "LimparCena/prop"); }
                }
                ev.PropVivo = null;
            }

            if (_cenas.TryGetValue(caso.Id, out CenaAtiva cena))
            {
                foreach (Rage.Object prop in cena.PropsCenario)
                {
                    if (prop != null && prop.Exists())
                    {
                        try { prop.Delete(); }
                        catch (Exception ex) { Logger.Exception(ex, "LimparCena/cenario"); }
                    }
                }
                cena.PropsCenario.Clear();
                // Blip preservado de proposito; so sai em RemoverCenaCompleta.
            }

            Logger.Info($"Visuais removidos do caso '{caso.Titulo}'. Blip e dados preservados.");
        }

        /// <summary>
        /// Remove a cena por completo, INCLUSIVE o blip. Usar ao encerrar/arquivar o caso.
        /// </summary>
        public void RemoverCenaCompleta(Caso caso)
        {
            if (caso == null)
            {
                return;
            }

            LimparCena(caso);

            if (_cenas.TryGetValue(caso.Id, out CenaAtiva cena))
            {
                if (cena.Blip != null)
                {
                    try { cena.Blip.Delete(); }
                    catch (Exception ex) { Logger.Exception(ex, "RemoverCenaCompleta/blip"); }
                }
                _cenas.Remove(caso.Id);
            }

            Logger.Info($"Cena do caso '{caso.Titulo}' removida por completo.");
        }

        /// <summary>True se a cena do caso esta montada no mundo agora.</summary>
        public bool CenaMontada(Guid casoId)
        {
            return _cenas.ContainsKey(casoId);
        }
    }
}