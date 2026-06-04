using System;
using System.Windows.Forms; // Keys
using Rage;
using Rage.Native;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Renderiza as cameras de seguranca via funcoes NATIVAS (estaveis entre versoes do RPH):
    /// cria a cam, aponta para a cena, aplica filtro CCTV e renderiza ate o jogador sair. Ao final
    /// restaura a camera do jogo, marca a gravacao como revisada (libera a info) e salva.
    /// </summary>
    public class CameraService
    {
        private const Keys TeclaSair = Keys.Back; // Backspace sai da camera

        private readonly CasoService _casoService;

        public CameraService(CasoService casoService)
        {
            _casoService = casoService;
        }

        public void Visualizar(GravacaoCamera gravacao)
        {
            if (gravacao == null)
            {
                Logger.Warn("CameraService.Visualizar: gravacao nula.");
                return;
            }

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
                    Game.DisplayHelp($"Camera: {gravacao.Local}. Pressione BACKSPACE para sair.");

                    while (true)
                    {
                        if (Game.IsKeyDown(TeclaSair))
                        {
                            break;
                        }
                        GameFiber.Yield();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, $"CameraService.Visualizar '{gravacao.Local}'");
                }
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
                        Game.DisplayNotification($"~g~CAMERA~s~~n~{gravacao.Local}: {gravacao.InfoRevelada}");
                    }

                    Logger.Info($"Camera '{gravacao.Local}' encerrada.");
                }
            }, "InvestigacaoBR.Camera");
        }

        private static void AplicarFiltroCctv(bool ativar)
        {
            try
            {
                if (ativar)
                {
                    NativeFunction.Natives.SET_TIMECYCLE_MODIFIER("scanline_cam");
                }
                else
                {
                    NativeFunction.Natives.CLEAR_TIMECYCLE_MODIFIER();
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "AplicarFiltroCctv");
            }
        }
    }
}