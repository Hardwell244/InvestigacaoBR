using System;
using System.Collections.Generic;
using System.Linq;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Gerente central dos casos em memoria. Mantem a lista mestre carregada, expoe as
    /// operacoes de criacao/aceite/atualizacao e persiste no disco (via CasoRepository) a
    /// cada mudanca. E a UNICA porta de entrada para mexer nos casos: os menus consomem
    /// este servico, nunca o repositorio ou as listas diretamente.
    ///
    /// Deve existir UMA instancia, dona da lista mestre durante a sessao.
    /// </summary>
    public class CasoService
    {
        private readonly CasoRepository _repository;
        private List<Caso> _casos;

        /// <summary>Usa um repositorio com o caminho padrao.</summary>
        public CasoService() : this(new CasoRepository())
        {
        }

        /// <summary>Permite injetar um repositorio customizado (util para testes).</summary>
        public CasoService(CasoRepository repository)
        {
            _repository = repository ?? new CasoRepository();
            _casos = new List<Caso>();
        }

        /// <summary>
        /// Carrega a lista mestre do disco para a memoria. Chame UMA vez ao iniciar o sistema
        /// (ex.: quando o jogador entra em servico).
        /// </summary>
        public void Inicializar()
        {
            _casos = _repository.Carregar();
            Logger.Info($"CasoService inicializado com {_casos.Count} caso(s) em memoria.");
        }

        /// <summary>Persiste a lista mestre atual no disco. Retorna true em sucesso.</summary>
        public bool Salvar()
        {
            return _repository.Salvar(_casos);
        }

        // ----- Criacao / insercao -----

        /// <summary>
        /// Cria um novo caso (status Disponivel), adiciona a lista mestre, salva e o retorna.
        /// dataInGame deve ser o tempo IN-GAME do LSPDFR.
        /// </summary>
        public Caso CriarCaso(string titulo, string descricaoGeral, DateTime dataInGame)
        {
            Caso caso = new Caso(titulo, descricaoGeral, dataInGame);
            _casos.Add(caso);
            Logger.Info($"Caso criado: '{caso.Titulo}' (Id {caso.Id}) com status {caso.Status}.");
            Salvar();
            return caso;
        }

        /// <summary>Adiciona um caso ja construido a lista mestre e salva.</summary>
        public bool AdicionarCaso(Caso caso)
        {
            if (caso == null)
            {
                Logger.Warn("AdicionarCaso recebeu nulo. Ignorando.");
                return false;
            }
            if (_casos.Any(c => c.Id == caso.Id))
            {
                Logger.Warn($"AdicionarCaso ignorado: ja existe caso com Id {caso.Id}.");
                return false;
            }

            _casos.Add(caso);
            Logger.Info($"Caso adicionado: '{caso.Titulo}' (Id {caso.Id}).");
            Salvar();
            return true;
        }

        // ----- Transicoes de estado -----

        /// <summary>
        /// O detetive aceita um caso do pool: transita de Disponivel para Aberto. Falha
        /// (logando) se o caso nao existir ou nao estiver Disponivel.
        /// </summary>
        public bool AceitarCaso(Guid casoId)
        {
            Caso caso = ObterPorId(casoId);
            if (caso == null)
            {
                Logger.Warn($"AceitarCaso: caso {casoId} nao encontrado.");
                return false;
            }
            if (caso.Status != StatusCaso.Disponivel)
            {
                Logger.Warn($"AceitarCaso ignorado: caso '{caso.Titulo}' nao esta Disponivel (atual: {caso.Status}).");
                return false;
            }

            caso.AlterarStatus(StatusCaso.Aberto);
            Salvar();
            return true;
        }

        /// <summary>Atualiza o status de um caso (Aberto/Arquivado/Resolvido) e salva.</summary>
        public bool AtualizarStatus(Guid casoId, StatusCaso novoStatus)
        {
            Caso caso = ObterPorId(casoId);
            if (caso == null)
            {
                Logger.Warn($"AtualizarStatus: caso {casoId} nao encontrado.");
                return false;
            }

            caso.AlterarStatus(novoStatus);
            Salvar();
            return true;
        }

        // ----- Consultas -----

        /// <summary>Caso pelo Id estavel, ou null se nao existir.</summary>
        public Caso ObterPorId(Guid casoId)
        {
            return _casos.FirstOrDefault(c => c.Id == casoId);
        }

        /// <summary>Todos os casos (lista somente leitura, snapshot).</summary>
        public IReadOnlyList<Caso> ObterTodos()
        {
            return _casos.ToList().AsReadOnly();
        }

        /// <summary>Casos do pool da delegacia (Disponivel) — para o Menu de Selecao.</summary>
        public IEnumerable<Caso> ObterDisponiveis()
        {
            return _casos.Where(c => c.Status == StatusCaso.Disponivel).ToList();
        }

        /// <summary>Casos ja aceitos pelo detetive (qualquer status != Disponivel) — Menu do Detetive.</summary>
        public IEnumerable<Caso> ObterDoDetetive()
        {
            return _casos.Where(c => c.Status != StatusCaso.Disponivel).ToList();
        }

        /// <summary>Casos filtrados por um status especifico.</summary>
        public IEnumerable<Caso> ObterPorStatus(StatusCaso status)
        {
            return _casos.Where(c => c.Status == status).ToList();
        }
    }
}