using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LemonUI;
using LSPD_First_Response.Mod.API;
using Rage;
using Rage.Native;
using InvestigacaoBR.Data;
using InvestigacaoBR.Services;
using InvestigacaoBR.UI;

namespace InvestigacaoBR.Core
{
    public class EntryPoint : Plugin
    {
        public const string PluginName = "InvestigacaoBR";
        public const string PluginVersion = "0.1.0";

        private GameFiber _mainFiber;
        private bool _isRunning;

        private ObjectPool _pool;
        private DetectiveService _detectiveService;
        private CasoService _casoService;
        private CenaService _cenaService;
        private LaboratorioService _laboratorioService;
        private CameraService _cameraService;
        private MandadoService _mandadoService;
        private GeradorCasos _geradorCasos;
        private PartnerService _partnerService;      // 5B
        private EventoVivoService _eventoVivoService;   // 5C
        private MenuSelecaoCasos _menuSelecaoCasos;
        private MenuDetetive _menuDetetive;
        private MenuInterrogatorio _menuInterrogatorio;

        private int _ticksBlip;
        private int _ticksFuga;
        private int _ticksParceiro;  // 5B: throttle do partner tick

        private readonly HashSet<Guid> _fugasAtivadas = new HashSet<Guid>();

        public override void Initialize()
        {
            Config.Carregar();
            Logger.Info($"{PluginName} v{PluginVersion} - Initialize() chamado.");
            Functions.OnOnDutyStateChanged += OnOnDutyStateChanged;
        }

        public override void Finally()
        {
            Logger.Info("Finally() chamado. Descarregando plugin...");
            try
            {
                CameraService.Desativar();
                _partnerService?.Parar(); // 5B: despawna parceiro
                PararSistema();
                Functions.OnOnDutyStateChanged -= OnOnDutyStateChanged;
                Logger.Info("Plugin descarregado com sucesso.");
            }
            catch (Exception ex) { Logger.Exception(ex, "Finally()"); }
        }

        private void OnOnDutyStateChanged(bool onDuty)
        {
            Logger.Info($"OnOnDutyStateChanged: {(onDuty ? "ENTROU EM" : "SAIU DE")} servico.");
            if (onDuty) IniciarSistema();
            else PararSistema();
        }

        private void IniciarSistema()
        {
            if (_isRunning) { Logger.Warn("IniciarSistema: ja em execucao."); return; }
            _isRunning = true;
            Logger.State("Sistema investigativo", "Parado", "Rodando");

            try
            {
                if (_pool == null) ConstruirServicosEUI();

                _casoService.Inicializar();
                TimelineService.Inicializar(_casoService);

                // Nome do personagem do LSPDFR
                string nomeJogador = "Detetive";
                try
                {
                    var persona = Functions.GetPersonaForPed(Game.LocalPlayer.Character);
                    if (persona != null) nomeJogador = $"{persona.Forename} {persona.Surname}".Trim();
                }
                catch { }

                _detectiveService.Inicializar(nomeJogador);

                // 5B: inicia parceiro com base no perfil carregado
                int indiceParceiro = _detectiveService.Perfil?.IndiceParceiro ?? 0;
                _partnerService.Iniciar(indiceParceiro);

                _laboratorioService.RetomarAnalisesPendentes(_casoService.ObterTodos());
                _mandadoService.RestaurarRastreamentos(_casoService.ObterTodos());
                _geradorCasos.GarantirPool();

                Logger.Info("Sistema pronto.");
            }
            catch (Exception ex) { Logger.Exception(ex, "IniciarSistema"); }

            _mainFiber = GameFiber.StartNew(MainLoop, $"{PluginName}.MainFiber");
        }

        private void ConstruirServicosEUI()
        {
            _pool = new ObjectPool();

            PersonaService personaService = new PersonaService();
            _casoService = new CasoService(new CasoRepository());
            _cenaService = new CenaService(personaService);
            _laboratorioService = new LaboratorioService(_casoService);
            _cameraService = new CameraService(_casoService);
            _mandadoService = new MandadoService(_casoService);
            _geradorCasos = new GeradorCasos(_casoService);
            _detectiveService = new DetectiveService();
            _partnerService = new PartnerService();
            _eventoVivoService = new EventoVivoService(_casoService);

            _menuSelecaoCasos = new MenuSelecaoCasos(_pool, _casoService, _cenaService, _geradorCasos);
            _menuDetetive = new MenuDetetive(_pool, _casoService, _cenaService,
                                      _laboratorioService, _cameraService, _mandadoService,
                                      _geradorCasos, _detectiveService, _partnerService);
            _menuInterrogatorio = new MenuInterrogatorio(
                                      _pool, _casoService, _detectiveService, _partnerService);

            Logger.Info("Servicos e UI construidos.");
        }

        private void PararSistema()
        {
            if (!_isRunning) return;
            _isRunning = false;
            Logger.State("Sistema investigativo", "Rodando", "Parado");
            try
            {
                _partnerService?.Parar();
                _mandadoService?.RemoverTodos();
                try { NativeFunction.Natives.RENDER_SCRIPT_CAMS(false, false, 0, true, false); } catch { }
            }
            catch (Exception ex) { Logger.Exception(ex, "PararSistema"); }
            _mainFiber = null;
        }

        private void MainLoop()
        {
            Logger.Info("MainFiber iniciado.");
            try
            {
                CarregarTodosDicionarios();
                Game.DisplayNotification("WEB_LOSSANTOSPOLICEDEPT", "WEB_LOSSANTOSPOLICEDEPT",
                    "InvestigacaoBR", "~b~LSPD~w~", "Sistema carregado!");
            }
            catch (Exception ex) { Logger.Exception(ex, "MainLoop/inicializacao"); }

            while (_isRunning)
            {
                try
                {
                    _pool?.Process();
                    ProcessarTeclas();
                    ProcessarBlipProximidade();
                    ProcessarFugaSuspeitos();
                    ProcessarParceiro();      // 5B
                    ProcessarEventosVivos();  // 5C
                }
                catch (Exception ex) { Logger.Exception(ex, "MainLoop/iteracao"); }
                GameFiber.Yield();
            }

            Logger.Info("MainFiber encerrado.");
        }

        // ===== CHECKS DO LOOP =====

        private void ProcessarTeclas()
        {
            if (_pool == null) return;
            bool teclaMenuCasos = Game.IsKeyDown(Config.TeclaMenuCasos);
            bool teclaMenuDetetive = Game.IsKeyDown(Config.TeclaMenuDetetive);

            if (teclaMenuCasos || teclaMenuDetetive)
            {
                if (_pool.AreAnyVisible)
                {
                    _menuSelecaoCasos?.Fechar();
                    _menuDetetive?.Fechar();
                    _menuInterrogatorio?.Fechar();
                }
                else if (teclaMenuCasos) _menuSelecaoCasos?.Abrir();
                else _menuDetetive?.Abrir();
            }

            if (Game.IsKeyDown(Config.TeclaInterrogar) && !_pool.AreAnyVisible) TentarInterrogar();
            if (Game.IsKeyDown(Config.TeclaLimparCena)) LimparCenasAtivas();
        }

        private void ProcessarBlipProximidade()
        {
            _ticksBlip++;
            if (_ticksBlip < 30) return;
            _ticksBlip = 0;
            if (_cenaService == null || Game.LocalPlayer?.Character == null) return;
            _cenaService.ProcessarBlipsProximidade(Game.LocalPlayer.Character.Position, Config.RaioBlipProximidade);
        }

        private void ProcessarFugaSuspeitos()
        {
            _ticksFuga++;
            if (_ticksFuga < 30) return;
            _ticksFuga = 0;
            if (_casoService == null || Game.LocalPlayer?.Character == null) return;
            Vector3 posJogador = Game.LocalPlayer.Character.Position;
            float raioFuga = Config.RaioFuga;

            foreach (Caso caso in _casoService.ObterDoDetetive())
            {
                if (caso.Status != StatusCaso.Aberto) continue;
                foreach (PedDoCaso ped in caso.Peds)
                {
                    if (!ped.EhCulpadoReal || !ped.MandadoEmitido) continue;
                    if (_fugasAtivadas.Contains(ped.Id)) continue;
                    if (ped.PedVivo == null || !ped.PedVivo.Exists() || ped.PedVivo.IsDead) continue;
                    if (Vector3.Distance(posJogador, ped.PedVivo.Position) > raioFuga) continue;
                    try
                    {
                        ped.PedVivo.BlockPermanentEvents = false;
                        NativeFunction.Natives.TASK_SMART_FLEE_PED(
                            ped.PedVivo, Game.LocalPlayer.Character, 200f, -1, false, false);
                        _fugasAtivadas.Add(ped.Id);
                        Notificacao.Alerta($"{ped.Nome} esta fugindo! Intercepte-o.");
                        Logger.Info($"Suspeito '{ped.Nome}' em fuga.");
                    }
                    catch (Exception ex) { Logger.Exception(ex, $"ProcessarFugaSuspeitos '{ped.Nome}'"); }
                }
            }
        }

        /// <summary>5B: Tick do parceiro a cada 60 frames (~1s). Mais leve que todo frame.</summary>
        private void ProcessarParceiro()
        {
            _ticksParceiro++;
            if (_ticksParceiro < 60) return;
            _ticksParceiro = 0;
            if (Game.LocalPlayer?.Character == null) return;
            _partnerService?.Tick();
        }

        /// <summary>5C: Tick dos eventos vivos — EventoVivoService controla timer interno.</summary>
        private void ProcessarEventosVivos()
        {
            _eventoVivoService?.Tick();
        }

        // ===== ACOES =====

        private void TentarInterrogar()
        {
            if (_casoService == null || _menuInterrogatorio == null) return;
            Vector3 posJogador = Game.LocalPlayer.Character.Position;
            float raio = Config.RaioInterrogacao;
            PedDoCaso pedAlvo = null;
            Caso casoAlvo = null;
            float menorDist = float.MaxValue;

            foreach (Caso caso in _casoService.ObterDoDetetive())
            {
                if (caso.Status != StatusCaso.Aberto) continue;
                foreach (PedDoCaso ped in caso.Peds)
                {
                    if (ped.PedVivo == null || !ped.PedVivo.Exists() || ped.SpawnarMorto) continue;
                    float dist = Vector3.Distance(posJogador, ped.PedVivo.Position);
                    if (dist < raio && dist < menorDist) { menorDist = dist; pedAlvo = ped; casoAlvo = caso; }
                }
            }

            if (pedAlvo != null) _menuInterrogatorio.AbrirParaPed(pedAlvo, casoAlvo);
            else Notificacao.Aviso($"Nenhum individuo proximo ({raio:F0} m). Chegue mais perto.");
        }

        private void LimparCenasAtivas()
        {
            if (_casoService == null || _cenaService == null) return;
            int limpas = 0;
            foreach (Caso caso in _casoService.ObterDoDetetive())
                if (_cenaService.CenaMontada(caso.Id)) { _cenaService.LimparCena(caso); limpas++; }
            if (limpas > 0) { Notificacao.Aviso($"END: {limpas} cena(s) limpa(s)."); Logger.Info($"END: {limpas} cena(s)."); }
        }

        private void CarregarTodosDicionarios()
        {
            string[] dicts = {
                "WEB_LOSSANTOSPOLICEDEPT","CHAR_DAVE","CHAR_BLOCKED","CHAR_CALL911",
                "CHAR_MP_FIB_CONTACT","CHAR_FILMNOIR","CHAR_MAUDE","CHAR_GANGAPP",
                "CHAR_AMMUNATION","CHAR_BANK_MAZE","CHAR_DETONATEPHONE","CHAR_DETONATEBOMB","CHAR_CARSITE"
            };
            foreach (string d in dicts) CarregarDicionarioTextura(d);
        }

        private void CarregarDicionarioTextura(string txtDict)
        {
            if (!NativeFunction.Natives.HAS_STREAMED_TEXTURE_DICT_LOADED<bool>(txtDict))
            {
                NativeFunction.Natives.REQUEST_STREAMED_TEXTURE_DICT(txtDict, true);
                int t = 0;
                while (!NativeFunction.Natives.HAS_STREAMED_TEXTURE_DICT_LOADED<bool>(txtDict) && t++ < 300)
                    GameFiber.Yield();
            }
        }
    }
}