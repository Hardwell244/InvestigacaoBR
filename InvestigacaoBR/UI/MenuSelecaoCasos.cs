using System.Collections.Generic;
using LemonUI;
using LemonUI.Menus;
using Rage;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;
using InvestigacaoBR.Services;

namespace InvestigacaoBR.UI
{
    /// <summary>
    /// Menu de selecao de casos (o "pegar casos" da delegacia). Lista os casos Disponivel; ao
    /// escolher um, aceita (Disponivel -> Aberto), spawna a cena fisica e repoe o pool. Recebe o
    /// ObjectPool da LemonUI (criado e processado pelo EntryPoint).
    /// </summary>
    public class MenuSelecaoCasos
    {
        private readonly NativeMenu _menu;
        private readonly CasoService _casoService;
        private readonly CenaService _cenaService;
        private readonly GeradorCasos _geradorCasos;

        public MenuSelecaoCasos(ObjectPool pool, CasoService casoService, CenaService cenaService, GeradorCasos geradorCasos)
        {
            _casoService = casoService;
            _cenaService = cenaService;
            _geradorCasos = geradorCasos;

            _menu = new NativeMenu("INVESTIGACAO", "Casos Disponiveis");
            pool.Add(_menu);
        }

        /// <summary>Reconstroi a lista de casos disponiveis e abre o menu.</summary>
        public void Abrir()
        {
            Recarregar();
            _menu.Visible = true;
            Logger.Menu("SelecaoCasos", "aberto");
        }

        private void Recarregar()
        {
            _menu.Clear();

            List<Caso> disponiveis = new List<Caso>(_casoService.ObterDisponiveis());
            if (disponiveis.Count == 0)
            {
                NativeItem vazio = new NativeItem("Nenhum caso disponivel", "Volte mais tarde para novos casos.")
                {
                    Enabled = false
                };
                _menu.Add(vazio);
                return;
            }

            foreach (Caso caso in disponiveis)
            {
                string descricao = $"{caso.DescricaoGeral}~n~Peds: {caso.Peds.Count} | Evidencias: {caso.Evidencias.Count} | Cameras: {caso.Cameras.Count}";
                NativeItem item = new NativeItem(caso.Titulo, descricao);

                Caso capturado = caso; // captura para o closure do evento
                item.Activated += (sender, args) => AceitarCaso(capturado);

                _menu.Add(item);
            }
        }

        private void AceitarCaso(Caso caso)
        {
            Logger.Menu("SelecaoCasos", $"aceitar '{caso.Titulo}'");

            if (!_casoService.AceitarCaso(caso.Id))
            {
                Game.DisplayNotification("~r~Nao foi possivel aceitar o caso.");
                return;
            }

            _cenaService.SpawnarCena(caso);   // blip + peds + evidencias no mundo
            _geradorCasos.GarantirPool();     // repoe o pool

            Game.DisplayNotification($"~g~CASO ACEITO~s~~n~{caso.Titulo}. Siga o blip ate a cena.");
            _menu.Visible = false;
        }
    }
}