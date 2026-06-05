using System;

namespace InvestigacaoBR.Data
{
    /// <summary>
    /// Entrada no diario cronologico de um caso.
    /// Autor indica a origem: SISTEMA, DETETIVE, LAB, MANDADO, PARCEIRO.
    /// Serializada dentro de Caso.Timeline no casos.xml.
    /// </summary>
    public class TimelineEntry
    {
        public DateTime DataHora { get; set; }
        public string Autor { get; set; }
        public string Texto { get; set; }

        /// <summary>Construtor sem parametros: obrigatorio para serializacao XML.</summary>
        public TimelineEntry() { }

        public TimelineEntry(DateTime dataHora, string autor, string texto)
        {
            DataHora = dataHora;
            Autor = autor;
            Texto = texto;
        }
    }
}