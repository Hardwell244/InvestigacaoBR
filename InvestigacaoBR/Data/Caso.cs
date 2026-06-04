using System;
using System.Collections.Generic;
using System.Linq;
using InvestigacaoBR.Core;

namespace InvestigacaoBR.Data
{
    /// <summary>
    /// Modelo central do sistema: um caso investigativo completo. Amarra os Peds (com papeis
    /// e verdade autorada), as evidencias, as cameras da area, o status do ciclo de vida, a
    /// data in-game, a localizacao da cena e o estado do procedimento fisico na cena.
    ///
    /// Mutacoes passam por metodos que logam e validam a ordem (jurisdicao -> isolar -> processar).
    /// As coordenadas ficam em floats (origem da cena); o CenaService combina com os offsets dos
    /// peds/evidencias para achar a posicao absoluta de spawn.
    /// </summary>
    public class Caso
    {
        // ----- Identidade / ciclo de vida -----
        public Guid Id { get; set; }
        public string Titulo { get; set; }
        public string DescricaoGeral { get; set; }
        public StatusCaso Status { get; set; }
        public DateTime DataAbertura { get; set; }

        // ----- Localizacao da cena (origem para spawns relativos) -----
        public float CenaX { get; set; }
        public float CenaY { get; set; }
        public float CenaZ { get; set; }
        public float CenaHeading { get; set; }

        // ----- Estado do procedimento fisico na cena -----
        /// <summary>Detetive assumiu a jurisdicao da cena. Primeiro passo do procedimento.</summary>
        public bool JurisdicaoAssumida { get; set; }

        /// <summary>Area isolada (fita/perimetro). Requer jurisdicao assumida.</summary>
        public bool CenaIsolada { get; set; }

        /// <summary>Cena processada (pericia liberou a coleta). Requer cena isolada.</summary>
        public bool CenaProcessada { get; set; }

        // ----- Colecoes -----
        public List<PedDoCaso> Peds { get; set; }
        public List<Evidencia> Evidencias { get; set; }
        public List<GravacaoCamera> Cameras { get; set; }

        // ----- Construtores -----
        /// <summary>Construtor sem parametros: OBRIGATORIO para serializacao XML. Nao remover.</summary>
        public Caso()
        {
            Id = Guid.NewGuid();
            Titulo = string.Empty;
            DescricaoGeral = string.Empty;
            Status = StatusCaso.Disponivel;
            DataAbertura = DateTime.MinValue;
            JurisdicaoAssumida = false;
            CenaIsolada = false;
            CenaProcessada = false;
            Peds = new List<PedDoCaso>();
            Evidencias = new List<Evidencia>();
            Cameras = new List<GravacaoCamera>();
        }

        /// <summary>Conveniencia. dataAbertura deve ser o tempo IN-GAME do LSPDFR.</summary>
        public Caso(string titulo, string descricaoGeral, DateTime dataAbertura) : this()
        {
            Titulo = string.IsNullOrEmpty(titulo) ? string.Empty : titulo;
            DescricaoGeral = string.IsNullOrEmpty(descricaoGeral) ? string.Empty : descricaoGeral;
            DataAbertura = dataAbertura;
        }

        // ----- Status (com log) -----
        public void AlterarStatus(StatusCaso novoStatus)
        {
            if (novoStatus == Status)
            {
                Logger.Info($"AlterarStatus ignorado no caso '{Titulo}': status ja e {Status}.");
                return;
            }

            StatusCaso anterior = Status;
            Status = novoStatus;
            Logger.State($"Status do caso '{Titulo}'", anterior.ToString(), novoStatus.ToString());
        }

        // ----- Colecoes (com log) -----
        public void AdicionarPed(PedDoCaso ped)
        {
            if (ped == null)
            {
                Logger.Warn($"AdicionarPed recebeu nulo no caso '{Titulo}'. Ignorando.");
                return;
            }

            Peds.Add(ped);
            Logger.Info($"Ped '{ped.Nome}' ({ped.Role}) adicionado ao caso '{Titulo}'. Total: {Peds.Count}.");
        }

        public void AdicionarEvidencia(Evidencia evidencia)
        {
            if (evidencia == null)
            {
                Logger.Warn($"AdicionarEvidencia recebeu nulo no caso '{Titulo}'. Ignorando.");
                return;
            }

            Evidencias.Add(evidencia);
            Logger.Info($"Evidencia '{evidencia.Titulo}' adicionada ao caso '{Titulo}'. Total: {Evidencias.Count}.");
        }

        public void AdicionarCamera(GravacaoCamera camera)
        {
            if (camera == null)
            {
                Logger.Warn($"AdicionarCamera recebeu nulo no caso '{Titulo}'. Ignorando.");
                return;
            }

            Cameras.Add(camera);
            Logger.Info($"Camera '{camera.Local}' adicionada ao caso '{Titulo}'. Total: {Cameras.Count}.");
        }

        // ----- Procedimento fisico na cena (ordem validada + log) -----

        /// <summary>1o passo: detetive assume a jurisdicao da cena. Idempotente.</summary>
        public bool AssumirJurisdicao()
        {
            if (JurisdicaoAssumida)
            {
                Logger.Info($"AssumirJurisdicao ignorado no caso '{Titulo}': ja assumida.");
                return false;
            }

            JurisdicaoAssumida = true;
            Logger.State($"Jurisdicao do caso '{Titulo}'", "Nao assumida", "Assumida");
            return true;
        }

        /// <summary>2o passo: isola a area. Requer jurisdicao assumida.</summary>
        public bool IsolarCena()
        {
            if (!JurisdicaoAssumida)
            {
                Logger.Warn($"IsolarCena bloqueado no caso '{Titulo}': assuma a jurisdicao primeiro.");
                return false;
            }
            if (CenaIsolada)
            {
                Logger.Info($"IsolarCena ignorado no caso '{Titulo}': ja isolada.");
                return false;
            }

            CenaIsolada = true;
            Logger.State($"Cena do caso '{Titulo}'", "Nao isolada", "Isolada");
            return true;
        }

        /// <summary>3o passo: processa a cena (libera a coleta de evidencias). Requer cena isolada.</summary>
        public bool ProcessarCena()
        {
            if (!CenaIsolada)
            {
                Logger.Warn($"ProcessarCena bloqueado no caso '{Titulo}': isole a cena primeiro.");
                return false;
            }
            if (CenaProcessada)
            {
                Logger.Info($"ProcessarCena ignorado no caso '{Titulo}': ja processada.");
                return false;
            }

            CenaProcessada = true;
            Logger.State($"Processamento do caso '{Titulo}'", "Pendente", "Concluido");
            return true;
        }

        // ----- Consultas -----

        /// <summary>Ped pelo Id estavel, ou null.</summary>
        public PedDoCaso ObterPedPorId(Guid pedId)
        {
            return Peds.FirstOrDefault(p => p.Id == pedId);
        }

        /// <summary>
        /// Ped cujo perfil de DNA bate com o informado, ou null. Usado para identificar o
        /// suspeito quando o laudo de uma evidencia revela um PerfilDnaId.
        /// </summary>
        public PedDoCaso ObterPedPorPerfilDna(string perfilDnaId)
        {
            if (string.IsNullOrEmpty(perfilDnaId))
            {
                return null;
            }
            return Peds.FirstOrDefault(p => p.PossuiDna && p.PerfilDnaId == perfilDnaId);
        }

        /// <summary>Peds atualmente marcados como Culpado.</summary>
        public IEnumerable<PedDoCaso> ObterCulpados()
        {
            return Peds.Where(p => p.Role == RolePed.Culpado);
        }

        /// <summary>True se o caso esta ativo para investigacao (aceito e nao encerrado).</summary>
        public bool EstaEmInvestigacao()
        {
            return Status == StatusCaso.Aberto;
        }
    }
}