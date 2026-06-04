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
    /// <summary>
    /// Ponto de entrada do plugin. O LSPDFR o localiza pela heranca de Plugin e gerencia o ciclo
    /// de vida via Initialize()/Finally(). A logica de gameplay so inicia quando o jogador entra
    /// em servico: aqui montamos o pool LemonUI, os servicos e os menus, processamos tudo no loop
    /// principal e tratamos as teclas F6 (casos), I (detetive) e END (limpar cena ativa).
    /// </summary>
    public class EntryPoint : Plugin
    {
        public const string PluginName = "InvestigacaoBR";
        public const string PluginVersion = "0.1.0";

        // Teclas
        private const Keys TeclaMenuCasos = Keys.F6;
        private const Keys TeclaMenuDetetive = Keys.I;
        private const Keys TeclaLimparCena = Keys.End;

        private GameFiber _mainFiber;
        private bool _isRunning;

        // Pool LemonUI + servicos + menus (construidos uma vez, reaproveitados)
        private ObjectPool _pool;
        private CasoService _casoService;
        private CenaService _cenaService;
        private LaboratorioService _laboratorioService;
        private CameraService _cameraService;
        private MandadoService _mandadoService;
        private GeradorCasos _geradorCasos;
        private MenuSelecaoCasos _menuSelecaoCasos;
        private MenuDetetive _menuDetetive;

        public override void Initialize()
        {
            Logger.Info($"{PluginName} v{PluginVersion} - Initialize() chamado. Plugin carregado.");
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
                Logger.Info("Plugin descarregado com sucesso.");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Finally()");
            }
        }

        private void OnOnDutyStateChanged(bool onDuty)
        {
            Logger.Info($"Evento OnOnDutyStateChanged: jogador {(onDuty ? "ENTROU EM" : "SAIU DE")} servico.");
            if (onDuty)
            {
                IniciarSistema();
            }
            else
            {
                PararSistema();
            }
        }

        private void IniciarSistema()
        {
            if (_isRunning)
            {
                Logger.Warn("IniciarSistema() chamado, mas o sistema ja esta em execucao. Ignorando.");
                return;
            }

            _isRunning = true;
            Logger.State("Sistema investigativo", "Parado", "Rodando");

            try
            {
                if (_pool == null)
                {
                    ConstruirServicosEUI();
                }

                _casoService.Inicializar();
                _laboratorioService.RetomarAnalisesPendentes(_casoService.ObterTodos());
                _mandadoService.RestaurarRastreamentos(_casoService.ObterTodos());
                _geradorCasos.GarantirPool();

                Logger.Info("Sistema pronto. Servicos e pool de casos carregados.");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "IniciarSistema");
            }

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
            _menuDetetive = new MenuDetetive(_pool, _casoService, _cenaService, _laboratorioService, _cameraService, _mandadoService);

            Logger.Info("Servicos e UI construidos.");
        }

        private void PararSistema()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            Logger.State("Sistema investigativo", "Rodando", "Parado");

            try
            {
                _mandadoService?.RemoverTodos();
                try { NativeFunction.Natives.RENDER_SCRIPT_CAMS(false, false, 0, true, false); } catch { }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "PararSistema/cleanup");
            }

            _mainFiber = null;
        }

        private void MainLoop()
        {
            Logger.Info("MainFiber iniciado. Loop principal em execucao.");

            // Inicializacao (textura + notificacao de carregamento) protegida a parte.
            try
            {
                CarregarDicionarioTextura("WEB_LOSSANTOSPOLICEDEPT");
                Game.DisplayNotification("WEB_LOSSANTOSPOLICEDEPT", "WEB_LOSSANTOSPOLICEDEPT", "InvestigacaoBR", "~b~LSPD~w~", "Sistema carregado!");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "MainLoop/inicializacao");
            }

            // LOOP BLINDADO: cada iteracao tem seu proprio try/catch. Se um handler de menu lancar
            // (ex.: World.DateTime invalido, um null inesperado, etc.), o erro e LOGADO e o loop
            // CONTINUA. O plugin nunca mais "morre e nao abre menu" por causa de uma unica excecao.
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
            // So abre menu se nenhum ja estiver visivel (deixa a LemonUI cuidar da navegacao interna).
            if (_pool != null && !_pool.AreAnyVisible)
            {
                if (Game.IsKeyDown(TeclaMenuCasos))
                {
                    _menuSelecaoCasos?.Abrir();
                }
                else if (Game.IsKeyDown(TeclaMenuDetetive))
                {
                    _menuDetetive?.Abrir();
                }
            }

            if (Game.IsKeyDown(TeclaLimparCena))
            {
                LimparCenasAtivas();
            }
        }

        /// <summary>
        /// END: remove os visuais (peds/props/fita) de todas as cenas montadas, mas os casos
        /// continuam ATIVOS para investigacao de dados — exatamente o requisito.
        /// </summary>
        private void LimparCenasAtivas()
        {
            if (_casoService == null || _cenaService == null)
            {
                return;
            }

            int limpas = 0;
            foreach (Caso caso in _casoService.ObterDoDetetive())
            {
                if (_cenaService.CenaMontada(caso.Id))
                {
                    _cenaService.LimparCena(caso);
                    limpas++;
                }
            }

            if (limpas > 0)
            {
                Game.DisplayNotification($"~y~END:~s~ visuais de {limpas} cena(s) removidos. Casos seguem ativos.");
                Logger.Info($"END: {limpas} cena(s) limpa(s); casos permanecem ativos.");
            }
        }

        private void CarregarDicionarioTextura(string txtDict)
        {
            // Verifica se ja nao esta carregado
            if (!NativeFunction.Natives.HAS_STREAMED_TEXTURE_DICT_LOADED<bool>(txtDict))
            {
                // Solicita o carregamento
                NativeFunction.Natives.REQUEST_STREAMED_TEXTURE_DICT(txtDict, true);

                // Aguarda ate estar totalmente carregado na memoria
                while (!NativeFunction.Natives.HAS_STREAMED_TEXTURE_DICT_LOADED<bool>(txtDict))
                {
                    GameFiber.Yield();
                }
            }
        }
    }
}