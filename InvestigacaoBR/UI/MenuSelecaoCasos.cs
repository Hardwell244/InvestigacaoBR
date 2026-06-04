using System.Collections.Generic;
using LemonUI;
using LemonUI.Menus;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;
using InvestigacaoBR.Services;

namespace InvestigacaoBR.UI
{
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

        public void Abrir()
        {
            RebuildLista();
            _menu.Visible = true;
            Logger.Menu("SelecaoCasos", "aberto");
        }

        /// <summary>fix #7: fecha o menu (toggle pelo EntryPoint).</summary>
        public void Fechar()
        {
            _menu.Visible = false;
        }

        private void RebuildLista()
        {
            _menu.Clear();

            List<Caso> casos = new List<Caso>(_casoService.ObterDisponiveis());
            if (casos.Count == 0)
            {
                _menu.Add(new NativeItem("Nenhum caso disponivel", "Aguarde novos casos.") { Enabled = false });
                return;
            }

            foreach (Caso caso in casos)
            {
                Caso c = caso;
                string desc = $"Local: ({c.CenaX:F0}, {c.CenaY:F0}) | Peds: {c.Peds.Count} | Evidencias: {c.Evidencias.Count}";
                NativeItem item = new NativeItem(c.Titulo, desc);
                item.Activated += (s, e) =>
                {
                    if (_casoService.AceitarCaso(c.Id))
                    {
                        _cenaService.SpawnarCena(c);
                        _geradorCasos.GarantirPool();
                        Notificacao.Sucesso($"Caso aceito: {c.Titulo}. Siga o blip.");
                        Logger.Menu("SelecaoCasos", $"aceitar '{c.Titulo}'");
                    }
                    _menu.Visible = false;
                };
                _menu.Add(item);
            }
        }
    }
}