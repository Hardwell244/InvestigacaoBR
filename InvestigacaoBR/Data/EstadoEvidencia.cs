namespace InvestigacaoBR.Data
{
    /// <summary>
    /// Estado de uma evidencia ao longo do fluxo fisico da investigacao:
    /// na cena -> coletada pelo detetive -> entregue ao laboratorio -> analisada (laudo pronto).
    ///
    /// Como o estado e persistido em disco, os valores inteiros sao FIXOS de proposito.
    /// NUNCA reutilize um numero ja usado, para nao quebrar casos salvos.
    /// </summary>
    public enum EstadoEvidencia
    {
        /// <summary>Ainda na cena do crime, nao coletada. Estado inicial de toda evidencia.</summary>
        NaCena = 0,

        /// <summary>Coletada pelo detetive e em posse, ainda nao enviada ao laboratorio.</summary>
        Coletada = 1,

        /// <summary>Entregue ao laboratorio, aguardando analise (delay in-game).</summary>
        NoLab = 2,

        /// <summary>Analise concluida: laudo forense (e DNA, se houver) disponivel.</summary>
        Analisada = 3
    }
}