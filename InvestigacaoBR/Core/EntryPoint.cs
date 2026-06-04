using System;
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

        private const Keys TeclaMenuCasos = Keys.F6;
        private const Keys TeclaMenuDetetive = Keys.I;
        private const Keys TeclaLimparCena = Keys.End;
        private const Keys TeclaInterrogar = Keys.OemQuestion;

        private GameFiber _mainFiber;
        private bool _isRunning;

        private ObjectPool _pool;
        private CasoService _casoService;
        private CenaService _cenaService;
        private LaboratorioService _laboratorioService;
        private CameraService _cameraService;
        private MandadoService _mandadoService;
        private GeradorCasos _geradorCasos;
        private MenuSelecaoCasos _menuSelecaoCasos;
        private MenuDetetive _menuDetetive;
        private MenuInterrogatorio _menuInterrogatorio;

        public override void Initialize()
        {
            Logger.Info($"{PluginName} v{PluginVersion} - Initialize() chamado.");
            Functions.OnOnDutyStateChanged += OnOnDutyStateChanged;
            Logger.Info("Inscrito em OnOnDutyStateChanged. Aguardando jogador entrar em servico.");
        }

        public override void Finally()
        {
            Logger.Info("Finally() chamado. Descarregando plugin...");
            try
            {
                PararSistema();
                Functions.OnOnDutyStateChanged -= OnOnDutyStateChanged;
                Logger.Info("Plugin descarregado.");
            }
            catch (Exception ex) { Logger.Exception(ex, "Finally()"); }
        }

        private void OnOnDutyStateChanged(bool onDuty)
        {
            Logger.Info($"OnOnDutyStateChanged: jogador {(onDuty ? "ENTROU EM" : "SAIU DE")} servico.");
            if (onDuty) IniciarSistema();
            else PararSistema();
        }

        private void IniciarSistema()
        {
            if (_isRunning) { Logger.Warn("IniciarSistema: ja em execucao. Ignorando."); return; }

            _isRunning = true;
            Logger.State("Sistema investigativo", "Parado", "Rodando");

            try
            {
                if (_pool == null) ConstruirServicosEUI();

                _casoService.Inicializar();
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

            _menuSelecaoCasos = new MenuSelecaoCasos(_pool, _casoService, _cenaService, _geradorCasos);
            _menuDetetive = new MenuDetetive(_pool, _casoService, _cenaService,
                                      _laboratorioService, _cameraService, _mandadoService, _geradorCasos);
            _menuInterrogatorio = new MenuInterrogatorio(_pool, _casoService);

            Logger.Info("Servicos e UI construidos.");
        }

        private void PararSistema()
        {
            if (!_isRunning) return;

            _isRunning = false;
            Logger.State("Sistema investigativo", "Rodando", "Parado");

            try
            {
                _mandadoService?.RemoverTodos();
                try { NativeFunction.Natives.RENDER_SCRIPT_CAMS(false, false, 0, true, false); } catch { }
            }
            catch (Exception ex) { Logger.Exception(ex, "PararSistema/cleanup"); }

            _mainFiber = null;
        }

        private void MainLoop()
        {
            Logger.Info("MainFiber iniciado.");

            try
            {
                CarregarDicionarioTextura("WEB_LOSSANTOSPOLICEDEPT");
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
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "MainLoop/iteracao");
                }
                GameFiber.Yield();
            }

            Logger.Info("MainFiber encerrado.");
        }

        private void ProcessarTeclas()
        {
            if (_pool == null) return;

            bool teclaF6 = Game.IsKeyDown(TeclaMenuCasos);
            bool teclaI = Game.IsKeyDown(TeclaMenuDetetive);

            if (teclaF6 || teclaI)
            {
                if (_pool.AreAnyVisible)
                {
                    _menuSelecaoCasos?.Fechar();
                    _menuDetetive?.Fechar();
                    _menuInterrogatorio?.Fechar();
                }
                else if (teclaF6)
                {
                    _menuSelecaoCasos?.Abrir();
                }
                else
                {
                    _menuDetetive?.Abrir();
                }
            }

            if (Game.IsKeyDown(TeclaInterrogar) && !_pool.AreAnyVisible)
            {
                TentarInterrogar();
            }

            if (Game.IsKeyDown(TeclaLimparCena))
            {
                LimparCenasAtivas();
            }
        }

        private void TentarInterrogar()
        {
            if (_casoService == null || _menuInterrogatorio == null) return;

            Vector3 posJogador = Game.LocalPlayer.Character.Position;
            const float Raio = 3f;

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
                    if (dist < Raio && dist < menorDist) { menorDist = dist; pedAlvo = ped; casoAlvo = caso; }
                }
            }

            if (pedAlvo != null)
                _menuInterrogatorio.AbrirParaPed(pedAlvo, casoAlvo);
            else
                Notificacao.Aviso("Nenhum individuo do caso proximo. Chegue a 3 m de um ped da cena.");
        }

        private void LimparCenasAtivas()
        {
            if (_casoService == null || _cenaService == null) return;

            int limpas = 0;
            foreach (Caso caso in _casoService.ObterDoDetetive())
            {
                if (_cenaService.CenaMontada(caso.Id)) { _cenaService.LimparCena(caso); limpas++; }
            }

            if (limpas > 0)
            {
                Notificacao.Aviso($"END: {limpas} cena(s) limpa(s). Casos seguem ativos.");
                Logger.Info($"END: {limpas} cena(s) limpa(s).");
            }
        }

        private void CarregarDicionarioTextura(string txtDict)
        {
            if (!NativeFunction.Natives.HAS_STREAMED_TEXTURE_DICT_LOADED<bool>(txtDict))
            {
                NativeFunction.Natives.REQUEST_STREAMED_TEXTURE_DICT(txtDict, true);
                while (!NativeFunction.Natives.HAS_STREAMED_TEXTURE_DICT_LOADED<bool>(txtDict))
                    GameFiber.Yield();
            }
        }
    }
}