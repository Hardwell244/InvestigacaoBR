using System;
using System.Windows.Forms;
using Rage;
using Rage.Native;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    public class CameraService
    {
        private const Keys TeclaSair = Keys.Back;
        private readonly CasoService _casoService;

        // FIX: flag estatica que o EntryPoint.Finally() desativa para encerrar fibers orfas
        private static volatile bool _ativo = true;

        public static void Desativar() { _ativo = false; }

        public CameraService(CasoService casoService) { _casoService = casoService; }

        public void Visualizar(GravacaoCamera gravacao)
        {
            if (gravacao == null) { Logger.Warn("CameraService.Visualizar: gravacao nula."); return; }

            GameFiber.StartNew(() =>
            {
                int cam = 0;
                bool criada = false;
                try
                {
                    Logger.Info($"Abrindo camera '{gravacao.Local}'.");
                    cam = NativeFunction.Natives.CREATE_CAM<int>("DEFAULT_SCRIPTED_CAMERA", true);
                    criada = true;

                    NativeFunction.Natives.SET_CAM_COORD(cam, gravacao.PosX, gravacao.PosY, gravacao.PosZ);
                    NativeFunction.Natives.POINT_CAM_AT_COORD(cam, gravacao.AlvoX, gravacao.AlvoY, gravacao.AlvoZ);
                    NativeFunction.Natives.SET_CAM_FOV(cam, gravacao.Fov);
                    NativeFunction.Natives.SET_CAM_ACTIVE(cam, true);
                    NativeFunction.Natives.RENDER_SCRIPT_CAMS(true, false, 0, true, false);

                    AplicarFiltroCctv(true);
                    Game.DisplayHelp($"Camera: {gravacao.Local}. BACKSPACE para sair.");

                    // FIX: era while(true) — a fiber nunca terminava se o plugin fosse descarregado
                    while (_ativo)
                    {
                        if (Game.IsKeyDown(TeclaSair)) break;
                        GameFiber.Yield();
                    }
                }
                catch (Exception ex) { Logger.Exception(ex, $"CameraService.Visualizar '{gravacao.Local}'"); }
                finally
                {
                    try { NativeFunction.Natives.RENDER_SCRIPT_CAMS(false, false, 0, true, false); } catch { }
                    AplicarFiltroCctv(false);
                    if (criada)
                    {
                        try { NativeFunction.Natives.SET_CAM_ACTIVE(cam, false); } catch { }
                        try { NativeFunction.Natives.DESTROY_CAM(cam, false); } catch { }
                    }
                    if (gravacao.MarcarRevisada())
                    {
                        _casoService.Salvar();
                        Notificacao.Camera($"{gravacao.Local}: {gravacao.InfoRevelada}");
                    }
                    Logger.Info($"Camera '{gravacao.Local}' encerrada.");
                }
            }, "InvestigacaoBR.Camera");
        }

        private static void AplicarFiltroCctv(bool ativar)
        {
            try
            {
                if (ativar) NativeFunction.Natives.SET_TIMECYCLE_MODIFIER("scanline_cam");
                else NativeFunction.Natives.CLEAR_TIMECYCLE_MODIFIER();
            }
            catch (Exception ex) { Logger.Exception(ex, "AplicarFiltroCctv"); }
        }
    }
}