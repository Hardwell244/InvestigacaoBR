using System;
using Rage;

namespace InvestigacaoBR.Core
{
    /// <summary>
    /// Fonte UNICA e segura do tempo in-game. World.DateTime as vezes lanca
    /// ArgumentOutOfRangeException ("Month must be between one and twelve") quando o relogio do
    /// jogo devolve uma data invalida. Chamado cru (como na coleta de evidencia), isso derrubava
    /// o fiber principal e travava todos os menus. Aqui o acesso e protegido: se o tempo do jogo
    /// falhar, caimos para DateTime.Now. Use SEMPRE TempoJogo.Agora() no lugar de World.DateTime.
    /// </summary>
    public static class TempoJogo
    {
        // Evita poluir o log: avisa do problema do relogio uma unica vez por sessao.
        private static bool _jaAvisou;

        /// <summary>Tempo in-game atual, com fallback seguro para o tempo real se o jogo falhar.</summary>
        public static DateTime Agora()
        {
            try
            {
                return World.DateTime;
            }
            catch (Exception)
            {
                if (!_jaAvisou)
                {
                    Logger.Warn("World.DateTime invalido (relogio do jogo). Usando tempo real como fallback. [aviso unico]");
                    _jaAvisou = true;
                }
                return DateTime.Now;
            }
        }
    }
}