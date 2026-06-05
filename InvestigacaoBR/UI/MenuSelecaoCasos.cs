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

        public MenuSelecaoCasos(ObjectPool pool, CasoService casoService,
            CenaService cenaService, GeradorCasos geradorCasos)
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

        public void Fechar() { _menu.Visible = false; }

        private void RebuildLista()
        {
            _menu.Clear();

            List<Caso> casos = new List<Caso>(_casoService.ObterDisponiveis());
            if (casos.Count == 0)
            {
                _menu.Add(new NativeItem("Nenhum caso disponivel",
                    "Aguarde novos casos ou use a Mesa de Trabalho para gerar.")
                { Enabled = false });
                return;
            }

            foreach (Caso caso in casos)
            {
                Caso c = caso;
                string desc = $"{c.DescricaoGeral.Substring(0, System.Math.Min(c.DescricaoGeral.Length, 60))}...  |  Peds: {c.Peds.Count}  |  Evidencias: {c.Evidencias.Count}";
                NativeItem item = new NativeItem(c.Titulo, desc);
                item.Activated += (s, e) =>
                {
                    if (_casoService.AceitarCaso(c.Id))
                    {
                        _cenaService.SpawnarCena(c);
                        _geradorCasos.GarantirPool();
                        NotificarCasoAceito(c);   // Fase 4: icone contextual por tipo
                        Logger.Menu("SelecaoCasos", $"aceitar '{c.Titulo}'");
                    }
                    _menu.Visible = false;
                };
                _menu.Add(item);
            }
        }

        /// <summary>
        /// Fase 4: escolhe o icone e a mensagem de acordo com o tipo do caso aceito,
        /// dando feedback imediato e contextual ao jogador sobre o que esperar na cena.
        /// </summary>
        private static void NotificarCasoAceito(Caso caso)
        {
            string t = caso.Titulo ?? "";

            if (t.StartsWith("Assassinato"))
                Notificacao.Policial($"{caso.Titulo}: oficial abatido. Prioridade maxima.");

            else if (t.StartsWith("Sequestro"))
                Notificacao.Urgente($"{caso.Titulo}: vitima desaparecida. Cada minuto importa.");

            else if (t.StartsWith("Incendio"))
                Notificacao.Incendio($"{caso.Titulo}: area em chamas. Localize o incendiario.");

            else if (t.StartsWith("Chacina"))
                Notificacao.Gangue($"{caso.Titulo}: multiplas vitimas. Tiroteio entre gangues.");

            else if (t.StartsWith("Homicidio") || t.StartsWith("Latrocinio"))
                Notificacao.Alerta($"{caso.Titulo}: vitima no local. Cena ativa.");

            else if (t.StartsWith("Trafico Armas"))
                Notificacao.Armas($"{caso.Titulo}: suspeitos armados. Cuidado ao se aproximar.");

            else if (t.StartsWith("Trafico") || t.StartsWith("Lab"))
                Notificacao.Gangue($"{caso.Titulo}: investigacao de narcoticos iniciada.");

            else if (t.StartsWith("Lavagem"))
                Notificacao.Financeiro($"{caso.Titulo}: rastreamento financeiro ativo.");

            else if (t.StartsWith("Carjacking"))
                Notificacao.Veiculo($"{caso.Titulo}: vitima presente na cena. Coletar depoimento.");

            else if (t.StartsWith("Invasao"))
                Notificacao.Alerta($"{caso.Titulo}: residencia invadida. Suspeitos possivelmente presentes.");

            else if (t.StartsWith("Roubo de Carga"))
                Notificacao.Gangue($"{caso.Titulo}: quadrilha ativa na area industrial.");

            else
                Notificacao.Sucesso($"{caso.Titulo} aceito. Siga o blip laranja.");
        }
    }
}