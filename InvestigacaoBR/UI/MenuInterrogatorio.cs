using System;
using LemonUI;
using LemonUI.Menus;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;
using InvestigacaoBR.Services;

namespace InvestigacaoBR.UI
{
    /// <summary>
    /// Menu de interrogatorio por proximidade. Quando o detetive se aproxima de um ped
    /// de um caso ativo (tecla G), este menu exibe perguntas cujas respostas dependem
    /// do Role atual do ped — testemunha ajuda, culpado nega, inocente nao sabe.
    /// fix #13A
    /// </summary>
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

            NativeItem q1 = new NativeItem("\"O que voce viu aqui?\"", "Pergunta sobre o evento.");
            NativeItem q2 = new NativeItem("\"Conhece alguem suspeito?\"", "Pergunta sobre suspeitos na area.");
            NativeItem q3 = new NativeItem("\"Onde estava quando aconteceu?\"", "Questiona o alibi.");

            q1.Activated += (s, e) => Notificacao.Info(RespostaQ1());
            q2.Activated += (s, e) => Notificacao.Info(RespostaQ2());
            q3.Activated += (s, e) => Notificacao.Info(RespostaQ3());

            // Marcar como testemunha se ainda indefinido
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
            _menu.Add(new NativeItem("─────────") { Enabled = false });
            _menu.Add(encerrar);
        }

        // ===== RESPOSTAS POR ROLE =====

        private string RespostaQ1()
        {
            switch (_ped.Role)
            {
                case RolePed.Testemunha:
                    return Aleatorio.Item(new System.Collections.Generic.List<string>
                    {
                        $"{_ped.Nome}: \"Eu vi uma pessoa saindo correndo daqui uns 20 minutos atras. Roupas escuras, andava rapido. Nao vi o rosto direito.\"",
                        $"{_ped.Nome}: \"Tinha um cara parado aqui por um bom tempo antes disso acontecer. Me pareceu estranho. Foi embora na direcao norte.\"",
                        $"{_ped.Nome}: \"Ouvi uns barulhos mas nao quis me meter. Vi quando uma pessoa foi embora com pressa por la.\"",
                    });

                case RolePed.Culpado:
                    return Aleatorio.Item(new System.Collections.Generic.List<string>
                    {
                        $"{_ped.Nome}: \"Cheguei agora, nao vi nada. To so passando por aqui.\"",
                        $"{_ped.Nome}: \"Nao sei de nada. Pra que essa conversa?\"",
                        $"{_ped.Nome}: \"Olha, eu nao quero encrenca. Nao vi coisa alguma.\"",
                    });

                case RolePed.Inocente:
                    return Aleatorio.Item(new System.Collections.Generic.List<string>
                    {
                        $"{_ped.Nome}: \"Eu estava de costas quando aconteceu. Ouvi alguma coisa mas nao vi nada. Sinto muito.\"",
                        $"{_ped.Nome}: \"Nao vi nada, estava no telefone. Quando olhei, ja tinha gente correndo.\"",
                    });

                case RolePed.PessoaDeInteresse:
                    return Aleatorio.Item(new System.Collections.Generic.List<string>
                    {
                        $"{_ped.Nome}: \"Por que voce ta me perguntando isso? Eu nao vi nada. Pode falar com outro.\"",
                        $"{_ped.Nome}: \"Vi algumas pessoas. Nao sei te dizer mais do que isso.\"",
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
                    return Aleatorio.Item(new System.Collections.Generic.List<string>
                    {
                        $"{_ped.Nome}: \"Tinha um cara que nunca vi por aqui ficando dando voltinha nessa area nos ultimos dias.\"",
                        $"{_ped.Nome}: \"O sujeito que vi indo embora nao e daqui do bairro. Rosto nao sei, mas o estilo era diferente.\"",
                    });

                case RolePed.Culpado:
                    return Aleatorio.Item(new System.Collections.Generic.List<string>
                    {
                        $"{_ped.Nome}: \"Nao conhco ninguem suspeito. Aqui e um lugar tranquilo.\"",
                        $"{_ped.Nome}: \"Todo mundo aqui e trabalhador. Nao sei do que voce ta falando.\"",
                    });

                case RolePed.Inocente:
                    return $"{_ped.Nome}: \"Nao, nenhum. Sou novo por aqui, nao conhco ninguem da area.\"";

                case RolePed.PessoaDeInteresse:
                    return $"{_ped.Nome}: \"Eu nao fico apontando o dedo pra ninguem. Cuida do seu servico.\"";

                default:
                    return $"{_ped.Nome}: \"Nao sei te dizer.\"";
            }
        }

        private string RespostaQ3()
        {
            switch (_ped.Role)
            {
                case RolePed.Testemunha:
                    return Aleatorio.Item(new System.Collections.Generic.List<string>
                    {
                        $"{_ped.Nome}: \"Estava bem aqui, tentando ligar pra alguem. Faz uns 15 a 20 minutos que tudo isso comecou.\"",
                        $"{_ped.Nome}: \"Estava sentado ali, esperando. Vi tudo de longe mas fiquei com medo de chegar perto.\"",
                    });

                case RolePed.Culpado:
                    return Aleatorio.Item(new System.Collections.Generic.List<string>
                    {
                        $"{_ped.Nome}: \"Cheguei bem depois. Quando apareci aqui ja tinha esse movimento todo.\"",
                        $"{_ped.Nome}: \"Eu so passei por aqui por acaso. Alibi? To brincando comigo?\"",
                    });

                case RolePed.Inocente:
                    return $"{_ped.Nome}: \"Estava no quarteirae seguinte. Vim ver o que era quando vi o movimento.\"";

                case RolePed.PessoaDeInteresse:
                    return $"{_ped.Nome}: \"Faz uma hora que to aqui. Mas isso e assunto meu, nao e?\"";

                default:
                    return $"{_ped.Nome}: \"Por aqui mesmo, passando.\"";
            }
        }
    }
}