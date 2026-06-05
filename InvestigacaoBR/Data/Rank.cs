namespace InvestigacaoBR.Data
{
    /// <summary>
    /// Patentes da carreira do detetive, em ordem crescente.
    /// XP necessario: Agente=0, DetIII=500, DetII=1500, DetI=3000, Inspetor=6000, Delegado=12000.
    /// </summary>
    public enum Rank
    {
        Agente = 0,
        DetetiveIII = 1,
        DetetiveII = 2,
        DetetiveI = 3,
        Inspetor = 4,
        Delegado = 5
    }
}