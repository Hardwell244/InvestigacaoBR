using System;
using System.Collections.Generic;
using LemonUI;
using LemonUI.Menus;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;
using InvestigacaoBR.Services;

namespace InvestigacaoBR.UI
{
    public class MenuInterrogatorio
    {
        private readonly NativeMenu _menu;
        private readonly CasoService _casoService;
        private readonly DetectiveService _detectiveService; // 5D
        private readonly PartnerService _partnerService;   // 5B
        private PedDoCaso _ped;
        private Caso _caso;

        public MenuInterrogatorio(ObjectPool pool, CasoService casoService,
            DetectiveService detectiveService, PartnerService partnerService)
        {
            _casoService = casoService;
            _detectiveService = detectiveService;
            _partnerService = partnerService;
            _menu = new NativeMenu("INTERROGATORIO", "Abordagem");
            pool.Add(_menu);
        }

        public void AbrirParaPed(PedDoCaso ped, Caso caso)
        {
            _ped = ped;
            _caso = caso;
            Rebuild();
            _menu.Visible = true;
            Logger.Menu("Interrogatorio", $"aberto para '{ped.Nome}' [{ped.Role}]");
        }

        public void Fechar() { _menu.Visible = false; }

        private void Rebuild()
        {
            _menu.Clear();
            if (_ped == null) return;

            string nome = _ped.DataNascimento != DateTime.MinValue ? _ped.Nome : "Individuo nao identificado";
            _menu.Name = nome;

            // ===== Perguntas =====
            NativeItem q1 = new NativeItem("\"O que voce viu aqui?\"", "Pergunta sobre o evento e o suspeito.");
            NativeItem q2 = new NativeItem("\"Conhece alguem suspeito nessa area?\"", "Pergunta sobre suspeitos e relacoes.");
            NativeItem q3 = new NativeItem("\"Onde estava quando aconteceu?\"", "Questiona alibi e horario.");
            NativeItem q4 = new NativeItem("\"Pode descrever melhor o que viu?\"", "Mais detalhes — so para testemunha.");

            q4.Enabled = _ped.Role == RolePed.Testemunha;

            q1.Activated += (s, e) => Notificacao.Info(RespostaQ1());
            q2.Activated += (s, e) => Notificacao.Info(RespostaQ2());
            q3.Activated += (s, e) => Notificacao.Info(RespostaQ3());
            q4.Activated += (s, e) => Notificacao.Info(RespostaQ4());

            _menu.Add(q1);
            _menu.Add(q2);
            _menu.Add(q3);
            _menu.Add(q4);

            // ===== Registrar como testemunha =====
            if (_ped.Role == RolePed.Indefinido)
            {
                NativeItem marcar = new NativeItem("Registrar como Testemunha", "Classifica este individuo como testemunha.");
                marcar.Activated += (s, e) =>
                {
                    _ped.AlterarRole(RolePed.Testemunha);
                    _casoService.Salvar();
                    TimelineService.Registrar(_caso.Id, $"{_ped.Nome} registrado como Testemunha.", "DETETIVE");
                    Notificacao.Sucesso($"{_ped.Nome} registrado como Testemunha.");
                    Rebuild();
                };
                _menu.Add(marcar);
            }

            // ===== 5D: Propina (so para culpados) =====
            if (_ped.EhCulpadoReal)
            {
                int valor = ValorPropina(_caso?.Titulo ?? "");
                NativeItem iPropina = new NativeItem(
                    $"Aceitar propina (${valor}k)",
                    $"~r~Corrupcao.~w~ Fechar os olhos em troca de ${valor}.000. Rep -12. Int -15.");
                NativeItem iRecusar = new NativeItem(
                    "Recusar propina",
                    "Manter a integridade. Rep +2. Int +3.");

                iPropina.Activated += (s, e) =>
                {
                    _detectiveService?.RegistrarPropina(valor);
                    _partnerService?.ComentarPropina(_caso.Id);
                    TimelineService.Registrar(_caso.Id,
                        $"[CORRUPCAO] {_ped.Nome} ofereceu propina de ${valor}k. Aceita.", "CORRUPCAO");
                    _ped.AlterarRole(RolePed.Inocente); // encobre o culpado
                    _casoService.Salvar();
                    Notificacao.Aviso($"${valor}.000 'transferidos'. Nao aparece no relatorio.");
                    Fechar();
                };

                iRecusar.Activated += (s, e) =>
                {
                    _detectiveService?.RegistrarPropinarRecusada();
                    _partnerService?.ComentarPropina(_caso.Id);
                    TimelineService.Registrar(_caso.Id,
                        $"{_ped.Nome} ofereceu propina de ${valor}k. Recusada.", "DETETIVE");
                    _casoService.Salvar();
                    Notificacao.Sucesso("Propina recusada. Integridade mantida.");
                    Rebuild();
                };

                _menu.Add(new NativeItem("--- Proposta ---") { Enabled = false });
                _menu.Add(iPropina);
                _menu.Add(iRecusar);
            }

            // ===== Encerrar =====
            NativeItem encerrar = new NativeItem("Encerrar abordagem");
            encerrar.Activated += (s, e) => Fechar();
            _menu.Add(encerrar);
        }

        // ===== RESPOSTAS =====

        private string RespostaQ1()
        {
            switch (_ped.Role)
            {
                case RolePed.Testemunha:
                    string dir = DirecaoFugaCulpado();
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Vi alguem saindo correndo em direcao ao {dir}. Roupas escuras, andava muito rapido.\"",
                        $"{_ped.Nome}: \"Tinha um individuo indo embora pelo lado {dir}. Entrou numa rua lateral e sumiu.\"",
                        $"{_ped.Nome}: \"Ouvi o barulho e quando olhei, vi uma silhueta correndo sentido {dir}.\"",
                    });
                case RolePed.Culpado:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Cheguei agora. Nao vi absolutamente nada. Por que to sendo interrogado?\"",
                        $"{_ped.Nome}: \"To passando por aqui so. Nao meto o nariz em coisa dos outros.\"",
                        $"{_ped.Nome}: \"Nao sei de nada. Fala com outro.\"",
                    });
                case RolePed.Inocente:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Estava de costas. Ouvi alguma coisa mas nao vi ninguem. Sinto muito.\"",
                        $"{_ped.Nome}: \"Estava no telefone. Quando levantei a cabeca ja tinha gente correndo.\"",
                    });
                case RolePed.PessoaDeInteresse:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Por que ta me perguntando isso? Pode falar com outro.\"",
                        $"{_ped.Nome}: \"Vi algumas pessoas. Nao sei te dizer mais.\"",
                    });
                default:
                    return $"{_ped.Nome}: \"Nao tenho nada a declarar.\"";
            }
        }

        private string RespostaQ2()
        {
            switch (_ped.Role)
            {
                case RolePed.Testemunha:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Tinha um cara que nunca vi por aqui ficando circulando nessa area.\"",
                        $"{_ped.Nome}: \"O sujeito que vi indo embora nao e daqui. O estilo era diferente.\"",
                    });
                case RolePed.Culpado:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Nao conhco ninguem suspeito. Aqui e tranquilo.\"",
                        $"{_ped.Nome}: \"Todo mundo aqui e trabalhador.\"",
                    });
                default:
                    return $"{_ped.Nome}: \"Nao conhco ninguem por aqui.\"";
            }
        }

        private string RespostaQ3()
        {
            switch (_ped.Role)
            {
                case RolePed.Testemunha:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Estava bem aqui, esperando uma ligacao. Faz tipo 20 minutos.\"",
                        $"{_ped.Nome}: \"Estava sentado ali. Vi a movimentacao de longe.\"",
                    });
                case RolePed.Culpado:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Cheguei agora. Quando isso aconteceu eu nem tinha chegado.\"",
                        $"{_ped.Nome}: \"Alibi? Nao tenho que provar nada. To so passando.\"",
                    });
                default:
                    return $"{_ped.Nome}: \"Por aqui mesmo, passando.\"";
            }
        }

        private string RespostaQ4()
        {
            if (_ped.Role != RolePed.Testemunha)
                return $"{_ped.Nome}: \"Nao tenho mais nada a acrescentar.\"";
            string dir = DirecaoFugaCulpado();
            return Aleatorio.Item(new List<string>
            {
                $"{_ped.Nome}: \"Era alto, medio. Roupas escuras. Saiu em direcao ao {dir} sem olhar pra tras.\"",
                $"{_ped.Nome}: \"Moreno, roupas simples. Foi pra direcao {dir} rapidao. Entrou num lugar por la.\"",
            });
        }

        private string DirecaoFugaCulpado()
        {
            if (_caso == null) return "direcao desconhecida";
            PedDoCaso culpado = null;
            foreach (PedDoCaso p in _caso.Peds) if (p.EhCulpadoReal) { culpado = p; break; }
            if (culpado == null || (culpado.LocalConhecidoX == 0f && culpado.LocalConhecidoY == 0f)) return "direcao desconhecida";
            float dx = culpado.LocalConhecidoX - _caso.CenaX;
            float dy = culpado.LocalConhecidoY - _caso.CenaY;
            if (Math.Abs(dy) >= Math.Abs(dx)) return dy >= 0f ? "norte" : "sul";
            return dx >= 0f ? "leste" : "oeste";
        }

        private static int ValorPropina(string titulo)
        {
            if (titulo.StartsWith("Assassinato")) return 50;
            if (titulo.StartsWith("Sequestro")) return 40;
            if (titulo.StartsWith("Lavagem")) return 35;
            if (titulo.StartsWith("Trafico Armas")) return 30;
            if (titulo.StartsWith("Chacina")) return 25;
            if (titulo.StartsWith("Trafico") || titulo.StartsWith("Lab")) return 20;
            return 15;
        }
    }
}