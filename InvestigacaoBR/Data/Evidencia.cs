using System;
using System.Xml.Serialization;
using InvestigacaoBR.Core;

namespace InvestigacaoBR.Data
{
    /// <summary>
    /// Uma evidencia do caso. Reune:
    ///  - exibicao (titulo, descricao);
    ///  - o fluxo fisico (Estado: NaCena -> Coletada -> NoLab -> Analisada);
    ///  - a VERDADE autorada revelada pela pericia (PerfilDnaId, ResultadoForense);
    ///  - dados de spawn no mundo (modelo do prop + offset relativo a origem da cena) e o
    ///    vinculo, somente em memoria, com o prop vivo.
    ///
    /// PerfilDnaId e ResultadoForense ja existem desde o inicio (sao autorados na ficha),
    /// mas so devem ser EXIBIDOS ao jogador quando Estado == Analisada. Isso e regra de UI;
    /// o dado em si fica guardado aqui o tempo todo.
    ///
    /// Tudo e persistido, exceto PropVivo (vinculo de sessao).
    /// </summary>
    public class Evidencia
    {
        /// <summary>Identidade estavel da evidencia, independente da sessao.</summary>
        public Guid Id { get; set; }

        /// <summary>Rotulo curto exibido no menu (ex.: "Casquilho 9mm").</summary>
        public string Titulo { get; set; }

        /// <summary>Detalhes/observacoes da evidencia.</summary>
        public string Descricao { get; set; }

        /// <summary>Estado no fluxo fisico. Altere via Coletar / EnviarAoLab / ConcluirAnalise.</summary>
        public EstadoEvidencia Estado { get; set; }

        /// <summary>Data/hora IN-GAME em que foi coletada. DateTime.MinValue = ainda na cena.</summary>
        public DateTime DataColeta { get; set; }

        /// <summary>
        /// VERDADE AUTORADA: id do perfil de DNA que a evidencia carrega (ex.: "DNA-001").
        /// Vazio = evidencia sem DNA. Cruzado com o PerfilDnaId dos Peds para identificar suspeito.
        /// Revelar na UI apenas quando Estado == Analisada.
        /// </summary>
        public string PerfilDnaId { get; set; }

        /// <summary>VERDADE AUTORADA: texto do laudo forense, revelado quando Estado == Analisada.</summary>
        public string ResultadoForense { get; set; }

        /// <summary>
        /// Modelo do prop que representa a evidencia no mundo (ex.: "prop_cs_bin_03").
        /// Vazio = usar um marcador padrao no chao (sem prop especifico).
        /// </summary>
        public string ModeloProp { get; set; }

        /// <summary>Offset X da evidencia em relacao a origem da cena do crime.</summary>
        public float OffsetX { get; set; }

        /// <summary>Offset Y da evidencia em relacao a origem da cena do crime.</summary>
        public float OffsetY { get; set; }

        /// <summary>Offset Z da evidencia em relacao a origem da cena do crime.</summary>
        public float OffsetZ { get; set; }

        /// <summary>
        /// Prop vivo no mundo durante a sessao atual. NAO persistido. Usado para coletar
        /// (deletar ao recolher) e para a limpeza pela tecla END.
        /// </summary>
        [XmlIgnore]
        public Rage.Object PropVivo { get; set; }

        /// <summary>True se a evidencia carrega DNA autorado.</summary>
        [XmlIgnore]
        public bool PossuiDna => !string.IsNullOrEmpty(PerfilDnaId);

        /// <summary>Construtor sem parametros: OBRIGATORIO para serializacao XML. Nao remover.</summary>
        public Evidencia()
        {
            Id = Guid.NewGuid();
            Titulo = string.Empty;
            Descricao = string.Empty;
            Estado = EstadoEvidencia.NaCena;
            DataColeta = DateTime.MinValue;
            PerfilDnaId = string.Empty;
            ResultadoForense = string.Empty;
            ModeloProp = string.Empty;
        }

        /// <summary>Conveniencia: cria uma evidencia ja com titulo e descricao.</summary>
        public Evidencia(string titulo, string descricao) : this()
        {
            Titulo = string.IsNullOrEmpty(titulo) ? string.Empty : titulo;
            Descricao = string.IsNullOrEmpty(descricao) ? string.Empty : descricao;
        }

        // ----- Transicoes de estado (com log centralizado) -----

        /// <summary>Cena -> Coletada. Registra a data in-game da coleta.</summary>
        public bool Coletar(DateTime dataInGame)
        {
            if (Estado != EstadoEvidencia.NaCena)
            {
                Logger.Warn($"Coletar ignorado para '{Titulo}': estado atual {Estado}, esperado NaCena.");
                return false;
            }

            EstadoEvidencia anterior = Estado;
            Estado = EstadoEvidencia.Coletada;
            DataColeta = dataInGame;
            Logger.State($"Evidencia '{Titulo}'", anterior.ToString(), Estado.ToString());
            return true;
        }

        /// <summary>Coletada -> NoLab.</summary>
        public bool EnviarAoLab()
        {
            if (Estado != EstadoEvidencia.Coletada)
            {
                Logger.Warn($"EnviarAoLab ignorado para '{Titulo}': estado atual {Estado}, esperado Coletada.");
                return false;
            }

            EstadoEvidencia anterior = Estado;
            Estado = EstadoEvidencia.NoLab;
            Logger.State($"Evidencia '{Titulo}'", anterior.ToString(), Estado.ToString());
            return true;
        }

        /// <summary>NoLab -> Analisada. A partir daqui, laudo e DNA podem ser exibidos.</summary>
        public bool ConcluirAnalise()
        {
            if (Estado != EstadoEvidencia.NoLab)
            {
                Logger.Warn($"ConcluirAnalise ignorado para '{Titulo}': estado atual {Estado}, esperado NoLab.");
                return false;
            }

            EstadoEvidencia anterior = Estado;
            Estado = EstadoEvidencia.Analisada;
            Logger.State($"Evidencia '{Titulo}'", anterior.ToString(), Estado.ToString());
            return true;
        }
    }
}