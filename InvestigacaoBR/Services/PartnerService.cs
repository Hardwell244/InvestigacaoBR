using System;
using System.Collections.Generic;
using Rage;
using Rage.Native;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// 5B: Gerencia o parceiro de investigacao do detetive.
    /// O parceiro spawnado segue o jogador, faz comentarios contextuais
    /// via notificacao e registra falas no diario do caso (TimelineService).
    /// </summary>
    public class PartnerService
    {
        // ===== PARCEIROS DISPONIVEIS =====

        public static readonly List<Partner> Parceiros = new List<Partner>
        {
            new Partner
            {
                Nome          = "Det. Miller",
                ModeloPed     = "s_m_y_cop_01",
                Personalidade = PersonalidadeParceiro.Correto,
                Descricao     = "Correto e rigoroso. Nao aceita desvios. Confiavel no departamento.",
                ComentariosChegada   = new List<string>
                {
                    "Det. Miller: \"Metodologia correta — evidencias antes de qualquer acusacao.\"",
                    "Det. Miller: \"Perimetro limpo. Vamos pelo livro, como sempre.\"",
                    "Det. Miller: \"Cena preservada. Bom começo.\"",
                },
                ComentariosEvidencia = new List<string>
                {
                    "Det. Miller: \"Bom achado. Isso vai direto para o processo.\"",
                    "Det. Miller: \"Cada evidencia e um tijolo na condenacao.\"",
                    "Det. Miller: \"Cataloga e carimba. Procedimento correto.\"",
                },
                ComentariosResolucao = new List<string>
                {
                    "Det. Miller: \"Caso encerrado do jeito certo. Sem atalhos.\"",
                    "Det. Miller: \"Mais um na ficha. Servico bem feito.\"",
                },
                ComentariosPropina = new List<string>
                {
                    "Det. Miller: \"Se aceitar isso, eu mesmo preencho o relatorio contra voce. Fica esperto.\"",
                    "Det. Miller: \"Propina? Sai da minha frente antes que eu te prenda junto.\"",
                }
            },
            new Partner
            {
                Nome          = "Det. Torres",
                ModeloPed     = "s_m_m_business_01",
                Personalidade = PersonalidadeParceiro.ZonaCinza,
                Descricao     = "Experiente e pragmatico. Tem principios, mas sabe dobrar regras.",
                ComentariosChegada   = new List<string>
                {
                    "Det. Torres: \"Cena fresca. Quem chegou primeiro escreve a historia.\"",
                    "Det. Torres: \"Esse padrao parece familiar. Ja vi antes.\"",
                    "Det. Torres: \"Vamos ver o que o local nos conta antes de decidir o caminho.\"",
                },
                ComentariosEvidencia = new List<string>
                {
                    "Det. Torres: \"Guardou bem. Nao tudo precisa ir pro relatorio completo.\"",
                    "Det. Torres: \"Isso tem valor. Tanto juridico quanto... pratico.\"",
                    "Det. Torres: \"Interessante. Guarda isso com cuidado.\"",
                },
                ComentariosResolucao = new List<string>
                {
                    "Det. Torres: \"Mais um no historico. Poderia ter sido mais rapido, mas ta bom.\"",
                    "Det. Torres: \"Funciona. Nao foi perfeito, mas funciona.\"",
                },
                ComentariosPropina = new List<string>
                {
                    "Det. Torres: \"Depende do numero. Nao sou idiota, mas tambem nao sou idiota ao contrario.\"",
                    "Det. Torres: \"Ouvi a proposta. Vou fingir que nao ouvi. Decide rapido.\"",
                }
            },
            new Partner
            {
                Nome          = "Det. Johnson",
                ModeloPed     = "s_m_y_sheriff_01",
                Personalidade = PersonalidadeParceiro.Corrupto,
                Descricao     = "Conhece os dois lados. Sempre encontra o caminho mais lucrativo.",
                ComentariosChegada   = new List<string>
                {
                    "Det. Johnson: \"Ja resolvi casos assim sem nem chegar na cena. Sabe como?\"",
                    "Det. Johnson: \"Tem sempre alguem que quer que isso suma rapido.\"",
                    "Det. Johnson: \"Interessante. Esse tipo de caso tem potencial... financeiro.\"",
                },
                ComentariosEvidencia = new List<string>
                {
                    "Det. Johnson: \"Esse pedaço aqui... podia sumir sem ninguem notar.\"",
                    "Det. Johnson: \"Catalogou tudo? Certeza que precisa de tudo isso no arquivo?\"",
                    "Det. Johnson: \"Boa evidencia. Vale mais viva do que catalogada, as vezes.\"",
                },
                ComentariosResolucao = new List<string>
                {
                    "Det. Johnson: \"Fechado. Podia ter sido mais lucrativo, mas ta bom.\"",
                    "Det. Johnson: \"Outra ficha. Pelo menos essa e limpa... na teoria.\"",
                },
                ComentariosPropina = new List<string>
                {
                    "Det. Johnson: \"Presta atencao no numero. As vezes e mais do que o salario anual.\"",
                    "Det. Johnson: \"Vai encarar? Eu ficaria de costas nessa hora. Por precaucao.\"",
                }
            }
        };

        // ===== ESTADO =====

        private Partner _parceiro;
        private Ped _pedParceiro;
        private bool _ativo;
        private int _ticksComentario;
        private int _proximoComentario;
        private static readonly Random _rnd = new Random();

        public bool TemParceiro => _parceiro != null;
        public string NomeParceiro => _parceiro?.Nome ?? "Nenhum";
        public int IndiceAtual { get; private set; } = 0;

        // ===== INICIALIZACAO =====

        public void Iniciar(int indiceParceiro)
        {
            IndiceAtual = Math.Max(0, Math.Min(indiceParceiro, Parceiros.Count - 1));
            _parceiro = Parceiros[IndiceAtual];
            _ativo = true;
            _proximoComentario = Aleatorio.Inteiro(1800, 3600); // 30-60s a 60fps
            Logger.Info($"PartnerService: parceiro selecionado — '{_parceiro.Nome}' ({_parceiro.Personalidade}).");

            SpawnarParceiro();
        }

        public void Parar()
        {
            _ativo = false;
            DespawnarParceiro();
        }

        public void SelecionarParceiro(int indice, DetectiveService detectiveService)
        {
            if (indice == IndiceAtual) return;
            DespawnarParceiro();
            Iniciar(indice);
            detectiveService?.Perfil?.Let(p => p.IndiceParceiro = indice);
            detectiveService?.Salvar();
        }

        // ===== TICK (chamado do MainLoop do EntryPoint) =====

        public void Tick()
        {
            if (!_ativo || _parceiro == null) return;
            if (_pedParceiro == null || !_pedParceiro.Exists())
            {
                SpawnarParceiro(); // re-spawna se sumiu
                return;
            }

            // Segue o jogador se ficou longe
            try
            {
                Ped jogador = Game.LocalPlayer.Character;
                if (jogador == null) return;
                float dist = Vector3.Distance(_pedParceiro.Position, jogador.Position);
                if (dist > 5f)
                {
                    NativeFunction.Natives.TASK_GO_TO_ENTITY(
                        _pedParceiro, jogador, -1, 1.5f, 1.0f, 1073741824, 0);
                }
            }
            catch { }

            // Comentario aleatorio de fundo
            _ticksComentario++;
            if (_ticksComentario >= _proximoComentario)
            {
                _ticksComentario = 0;
                _proximoComentario = Aleatorio.Inteiro(3600, 7200); // 1-2 min
                ComentarioAmbiente();
            }
        }

        // ===== COMENTARIOS CONTEXTUAIS =====

        public void ComentarChegadaCena(Caso caso)
        {
            if (_parceiro == null || caso == null) return;
            string fala = Aleatorio.Item(_parceiro.ComentariosChegada);
            Notificacao.Info(fala);
            TimelineService.Registrar(caso.Id, fala, "PARCEIRO");
        }

        public void ComentarEvidenciaEncontrada(Guid casoId)
        {
            if (_parceiro == null) return;
            string fala = Aleatorio.Item(_parceiro.ComentariosEvidencia);
            Notificacao.Info(fala);
            TimelineService.Registrar(casoId, fala, "PARCEIRO");
        }

        public void ComentarResolucao(Caso caso)
        {
            if (_parceiro == null || caso == null) return;
            string fala = Aleatorio.Item(_parceiro.ComentariosResolucao);
            Notificacao.Sucesso(fala);
            TimelineService.Registrar(caso.Id, fala, "PARCEIRO");
        }

        public void ComentarPropina(Guid casoId)
        {
            if (_parceiro == null) return;
            string fala = Aleatorio.Item(_parceiro.ComentariosPropina);
            Notificacao.Aviso(fala);
            TimelineService.Registrar(casoId, fala, "PARCEIRO");
        }

        // ===== HELPERS PRIVADOS =====

        private void SpawnarParceiro()
        {
            if (_parceiro == null) return;
            try
            {
                Ped jogador = Game.LocalPlayer?.Character;
                if (jogador == null) return;

                Vector3 pos = jogador.Position + new Vector3(1.5f, 0.5f, 0f);
                _pedParceiro = new Ped(new Model(_parceiro.ModeloPed), pos, jogador.Heading);

                if (_pedParceiro == null || !_pedParceiro.Exists())
                {
                    Logger.Warn($"PartnerService: falha ao spawnar '{_parceiro.Nome}'.");
                    return;
                }

                _pedParceiro.IsPersistent = true;
                _pedParceiro.BlockPermanentEvents = false;
                Logger.Info($"PartnerService: '{_parceiro.Nome}' spawnado.");
            }
            catch (Exception ex) { Logger.Exception(ex, "PartnerService.SpawnarParceiro"); }
        }

        private void DespawnarParceiro()
        {
            try
            {
                if (_pedParceiro != null && _pedParceiro.Exists())
                {
                    _pedParceiro.Tasks.Clear();
                    _pedParceiro.Dismiss();
                }
            }
            catch { }
            _pedParceiro = null;
        }

        private void ComentarioAmbiente()
        {
            // Comentario generico de presenca, baseado na personalidade
            if (_parceiro == null) return;
            switch (_parceiro.Personalidade)
            {
                case PersonalidadeParceiro.Correto:
                    Notificacao.Info($"{_parceiro.Nome}: \"Tudo em dia no departamento? Espero que sim.\"");
                    break;
                case PersonalidadeParceiro.ZonaCinza:
                    Notificacao.Info($"{_parceiro.Nome}: \"Anos nessa funcao e ainda me surpreendo com a besteira humana.\"");
                    break;
                case PersonalidadeParceiro.Corrupto:
                    Notificacao.Info($"{_parceiro.Nome}: \"Tem sempre uma oportunidade esperando em cada caso, so saber olhar.\"");
                    break;
            }
        }
    }

    // Extensao auxiliar para evitar null check verboso
    internal static class ObjectExt
    {
        public static void Let<T>(this T obj, Action<T> action) where T : class
        {
            if (obj != null) action(obj);
        }
    }
}