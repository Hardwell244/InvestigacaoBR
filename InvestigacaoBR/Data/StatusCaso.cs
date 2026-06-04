namespace InvestigacaoBR.Data
{
    /// <summary>
    /// Status (estado do ciclo de vida) de um caso investigativo.
    /// Define tambem em qual menu o caso aparece:
    ///   - Disponivel  -> Menu de Selecao de Casos ("Pegar Casos" na delegacia)
    ///   - demais       -> Menu do Detetive (casos que o jogador ja aceitou)
    ///
    /// Toda transicao de status deve ser logada via Logger.State(...) por quem altera.
    ///
    /// Valores inteiros FIXOS e explicitos: o status e serializado em disco, entao
    /// congelar os numeros evita quebrar casos salvos se a ordem mudar no futuro.
    /// NUNCA reutilize um numero ja usado.
    /// </summary>
    public enum StatusCaso
    {
        /// <summary>No pool de casos da delegacia, ainda nao aceito pelo detetive.</summary>
        Disponivel = 0,

        /// <summary>Aceito e em investigacao ativa. Estado padrao apos o jogador pegar o caso.</summary>
        Aberto = 1,

        /// <summary>Engavetado (cold case). Sem investigacao ativa, mas pode ser reaberto.</summary>
        Arquivado = 2,

        /// <summary>Encerrado com sucesso (culpado identificado/preso). Estado final.</summary>
        Resolvido = 3
    }
}