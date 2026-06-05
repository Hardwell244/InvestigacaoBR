using System;
using System.Xml.Serialization;

namespace InvestigacaoBR.Data
{
    public class DetectiveProfile
    {
        // ----- Identidade -----
        public string Nome { get; set; } = "Detetive";
        public string Matricula { get; set; } = "0000";

        // ----- Progressao -----
        public int XP { get; set; } = 0;
        public Rank Patente { get; set; } = Rank.Agente;
        public int Reputacao { get; set; } = 50;

        // ----- 5B: Parceiro -----
        public int IndiceParceiro { get; set; } = 0;

        // ----- 5D: Corrupcao -----
        /// <summary>0-100. Comeca em 100. Cai ao aceitar propinas ou prender inocentes.</summary>
        public int Integridade { get; set; } = 100;
        /// <summary>Total acumulado em propinas aceitas (em milhares de dolares).</summary>
        public int DinheiroPropinas { get; set; } = 0;

        // ----- Estatisticas -----
        public int CasosResolvidos { get; set; } = 0;
        public int CasosArquivados { get; set; } = 0;
        public int PrisoesCertas { get; set; } = 0;
        public int PrisoesErradas { get; set; } = 0;
        public int MandadosEmitidos { get; set; } = 0;
        public int EvidenciasColetadas { get; set; } = 0;
        public int PropinaRecusadas { get; set; } = 0;

        // ----- Calculados (nao serializados) -----
        [XmlIgnore] public static readonly int[] XpThresholds = { 0, 500, 1500, 3000, 6000, 12000 };
        [XmlIgnore] public int XpParaProximaPatente => (int)Patente >= XpThresholds.Length - 1 ? int.MaxValue : XpThresholds[(int)Patente + 1];
        [XmlIgnore] public bool PatenteMaxima => Patente == Rank.Delegado;

        /// <summary>Construtor sem parametros: obrigatorio para serializacao XML.</summary>
        public DetectiveProfile() { }

        public void Salvar() { } // chamado pelo DetectiveService

        // Helper fluent para PartnerService
        internal DetectiveProfile Let(Action<DetectiveProfile> action) { action(this); return this; }
    }
}