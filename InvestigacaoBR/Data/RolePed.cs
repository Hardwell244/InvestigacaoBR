namespace InvestigacaoBR.Data
{
    /// <summary>
    /// Papel dinamico de um Ped dentro de um caso investigativo.
    /// E mutavel ao longo da investigacao: por exemplo, uma Testemunha ou
    /// Pessoa de Interesse pode ser promovida a Culpado quando surgem novas pistas.
    /// Cada mudanca deve ser logada via Logger.State(...) por quem altera o valor.
    ///
    /// Os valores inteiros sao FIXOS e explicitos de proposito: como o Role e
    /// serializado em disco, congelar os numeros evita que casos salvos quebrem
    /// caso a ordem dos itens mude no futuro. NUNCA reutilize um numero ja usado.
    /// </summary>
    public enum RolePed
    {
        /// <summary>Ainda nao classificado. Estado inicial de todo Ped recem-adicionado ao caso.</summary>
        Indefinido = 0,

        /// <summary>Presenciou o evento. Fonte de informacao, nao e alvo da investigacao.</summary>
        Testemunha = 1,

        /// <summary>Possivel envolvimento. Sob observacao, mas sem acusacao formal.</summary>
        PessoaDeInteresse = 2,

        /// <summary>Descartado como envolvido. Oficialmente inocentado no caso.</summary>
        Inocente = 3,

        /// <summary>Suspeito/Culpado oficial do caso.</summary>
        Culpado = 4
    }
}