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
        private PedDoCaso _ped;
        private Caso _caso;

        public MenuInterrogatorio(ObjectPool pool, CasoService casoService)
        {
            _casoService = casoService;
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

            NativeItem q1 = new NativeItem("\"O que voce viu aqui?\"", "Pergunta sobre o evento e possivel suspeito.");
            NativeItem q2 = new NativeItem("\"Conhece alguem suspeito nessa area?\"", "Pergunta sobre suspeitos e relacoes.");
            NativeItem q3 = new NativeItem("\"Onde estava quando aconteceu?\"", "Questiona alibi e horario.");
            NativeItem q4 = new NativeItem("\"Pode descrever melhor o que viu?\"", "Pede mais detalhes — so para testemunha.");

            q4.Enabled = _ped.Role == RolePed.Testemunha;

            q1.Activated += (s, e) => Notificacao.Info(RespostaQ1());
            q2.Activated += (s, e) => Notificacao.Info(RespostaQ2());
            q3.Activated += (s, e) => Notificacao.Info(RespostaQ3());
            q4.Activated += (s, e) => Notificacao.Info(RespostaQ4Testemunha());

            if (_ped.Role == RolePed.Indefinido)
            {
                NativeItem marcar = new NativeItem("Registrar como Testemunha",
                    "Classifica este individuo como testemunha no caso.");
                marcar.Activated += (s, e) =>
                {
                    _ped.AlterarRole(RolePed.Testemunha);
                    _casoService.Salvar();
                    Notificacao.Sucesso($"{_ped.Nome} registrado como Testemunha.");
                    Rebuild();
                };
                _menu.Add(marcar);
            }

            NativeItem encerrar = new NativeItem("Encerrar abordagem");
            encerrar.Activated += (s, e) => Fechar();

            _menu.Add(q1);
            _menu.Add(q2);
            _menu.Add(q3);
            _menu.Add(q4);
            _menu.Add(encerrar);
        }

        // ===== RESPOSTAS =====

        private string RespostaQ1()
        {
            switch (_ped.Role)
            {
                case RolePed.Testemunha:
                    {
                        // G5: usa a direcao real de fuga do culpado baseada nas coordenadas do caso
                        string dir = DirecaoFugaCulpado();
                        return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Vi uma pessoa saindo correndo em direcao ao {dir}. Roupas escuras, andava muito rapido. Parecia nervosa.\"",
                        $"{_ped.Nome}: \"Tinha um individuo indo embora rapidinho pelo lado {dir}. Entrou numa rua lateral e sumiu.\"",
                        $"{_ped.Nome}: \"Ouvi o barulho e quando olhei, vi uma silhueta correndo sentido {dir}. Nao vi o rosto, mas era alto.\"",
                    });
                    }

                case RolePed.Culpado:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Cheguei aqui agora, nao vi absolutamente nada. Por que to sendo interrogado?\"",
                        $"{_ped.Nome}: \"Olha, to passando por aqui so. Nao meto o nariz em coisa dos outros.\"",
                        $"{_ped.Nome}: \"Nao sei de nada. Quer saber de verdade? Fala com outro.\"",
                    });

                case RolePed.Inocente:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Estava de costas quando aconteceu. Ouvi alguma coisa mas nao vi ninguem. Sinto muito.\"",
                        $"{_ped.Nome}: \"Estava no telefone. Quando levantei a cabeca ja tinha gente correndo por aqui.\"",
                    });

                case RolePed.PessoaDeInteresse:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Por que ta me perguntando isso? Eu nao vi nada. Pode deixar eu ir embora?\"",
                        $"{_ped.Nome}: \"Vi algumas pessoas, sim. Mas nao to aqui pra te contar a vida delas.\"",
                    });

                default:
                    return $"{_ped.Nome}: \"Nao tenho nada a declarar, inspetor.\"";
            }
        }

        private string RespostaQ2()
        {
            switch (_ped.Role)
            {
                case RolePed.Testemunha:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Tinha um cara que eu nunca vi nessa area ficando circulando por aqui ha alguns dias. Parecia estar observando as pessoas.\"",
                        $"{_ped.Nome}: \"Nao reconheco ninguem especificamente, mas o sujeito que vi hoje nao e daqui. O modo de vestir era diferente.\"",
                        $"{_ped.Nome}: \"Passei aqui ontem e vi o mesmo cara encostado ali. Ficava olhando pras pessoas. Me deixou desconfortavel.\"",
                    });

                case RolePed.Culpado:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Nao conhco ninguem suspeito. Todo mundo aqui e honesto.\"",
                        $"{_ped.Nome}: \"Aqui todo mundo se da bem. Suspeito de que? De quem?\"",
                    });

                case RolePed.Inocente:
                    return $"{_ped.Nome}: \"Nao, nenhum. Sou novo nessa area, mal conhco os vizinhos.\"";

                case RolePed.PessoaDeInteresse:
                    return $"{_ped.Nome}: \"Eu nao fico apontando dedo pra ninguem. Cuida do seu servico.\"";

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
                        $"{_ped.Nome}: \"Estava bem aqui esperando uma ligacao. Faz tipo 15 a 20 minutos que aquilo aconteceu. Vi tudo.\"",
                        $"{_ped.Nome}: \"Estava sentado ali na esquina. Vi a movimentacao de longe mas fiquei com medo de me aproximar.\"",
                        $"{_ped.Nome}: \"Fui comprar alguma coisa na loja e quando voltei ja tinha aquela situacao. Isso foi ha uns 25 minutos.\"",
                    });

                case RolePed.Culpado:
                    return Aleatorio.Item(new List<string>
                    {
                        $"{_ped.Nome}: \"Cheguei aqui agora ha pouco. Quando isso aconteceu eu nem tinha chegado ainda.\"",
                        $"{_ped.Nome}: \"Alibi? Eu nao tenho que provar nada. To so passando por aqui.\"",
                    });

                case RolePed.Inocente:
                    return $"{_ped.Nome}: \"Estava no bloco do lado. Vim ver a movimentacao quando ouvi o barulho.\"";

                case RolePed.PessoaDeInteresse:
                    return $"{_ped.Nome}: \"Faz uns 40 minutos que to aqui. Mas isso e la com o meu tempo, nao e?\"";

                default:
                    return $"{_ped.Nome}: \"Por aqui mesmo, passando.\"";
            }
        }

        /// <summary>
        /// G7: Quarta pergunta — so disponivel para Testemunha. Da mais detalhes sobre o suspeito.
        /// </summary>
        private string RespostaQ4Testemunha()
        {
            if (_ped.Role != RolePed.Testemunha)
                return $"{_ped.Nome}: \"Nao tenho mais nada a acrescentar.\"";

            string dir = DirecaoFugaCulpado();
            return Aleatorio.Item(new List<string>
            {
                $"{_ped.Nome}: \"Era alto, medio. Roupas escuras. Saiu em direcao ao {dir} sem olhar pra tras. Parecia que sabia exatamente pra onde ia.\"",
                $"{_ped.Nome}: \"Moreno, roupas simples. Foi pra direcao {dir} rapidao. Vi ele entrar em algum lugar por la, nao sei dizer qual.\"",
                $"{_ped.Nome}: \"Usava capuz. Nao vi a cor da pele nem o rosto. Foi sumindo sentido {dir} e entrou num beco. Rapido demais.\"",
            });
        }

        // ===== HELPER: direcao de fuga do culpado =====

        /// <summary>
        /// G5: Calcula a direcao cardeal do LocalConhecido do culpado em relacao a origem da cena.
        /// Usada para dar pistas de direcao de fuga nas respostas das testemunhas.
        /// </summary>
        private string DirecaoFugaCulpado()
        {
            if (_caso == null) return "direcao desconhecida";

            PedDoCaso culpado = null;
            foreach (PedDoCaso p in _caso.Peds)
                if (p.EhCulpadoReal) { culpado = p; break; }

            if (culpado == null) return "direcao desconhecida";
            if (culpado.LocalConhecidoX == 0f && culpado.LocalConhecidoY == 0f) return "direcao desconhecida";

            float dx = culpado.LocalConhecidoX - _caso.CenaX;
            float dy = culpado.LocalConhecidoY - _caso.CenaY;

            // GTA V: X = leste, Y = norte
            if (Math.Abs(dy) >= Math.Abs(dx))
                return dy >= 0f ? "norte" : "sul";
            else
                return dx >= 0f ? "leste" : "oeste";
        }
    }
}