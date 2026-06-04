using Rage;

namespace InvestigacaoBR.Core
{
    /// <summary>
    /// Notificacoes no padrao visual do LSPDFR (icone + banner colorido).
    /// Centraliza os parametros de textura para nao repetir em todo o projeto.
    /// Use sempre Notificacao.Info/Sucesso/etc. em vez de Game.DisplayNotification puro.
    /// </summary>
    public static class Notificacao
    {
        private const string Dict = "WEB_LOSSANTOSPOLICEDEPT";
        private const string Sprite = "WEB_LOSSANTOSPOLICEDEPT";
        private const string App = "InvestigacaoBR";

        public static void Mostrar(string assunto, string mensagem)
            => Game.DisplayNotification(Dict, Sprite, App, assunto, mensagem);

        /// <summary>Azul — informacoes gerais da investigacao.</summary>
        public static void Info(string mensagem) => Mostrar("~b~Detetive~w~", mensagem);

        /// <summary>Amarelo — avisos e estados intermediarios.</summary>
        public static void Aviso(string mensagem) => Mostrar("~y~Aviso~w~", mensagem);

        /// <summary>Vermelho — erros ou bloqueios.</summary>
        public static void Alerta(string mensagem) => Mostrar("~r~Alerta~w~", mensagem);

        /// <summary>Verde — acoes concluidas com sucesso.</summary>
        public static void Sucesso(string mensagem) => Mostrar("~g~Sucesso~w~", mensagem);

        /// <summary>Roxo — emissao de mandado e rastreamento.</summary>
        public static void Mandado(string mensagem) => Mostrar("~p~Mandado~w~", mensagem);

        /// <summary>Laranja — resultados do laboratorio.</summary>
        public static void Lab(string mensagem) => Mostrar("~o~Laboratorio~w~", mensagem);

        /// <summary>Branco/cinza — cameras e inteligencia visual.</summary>
        public static void Camera(string mensagem) => Mostrar("~c~Camera~w~", mensagem);
    }
}