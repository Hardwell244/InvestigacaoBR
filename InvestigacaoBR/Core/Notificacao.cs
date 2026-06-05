using Rage;

namespace InvestigacaoBR.Core
{
    /// <summary>
    /// Central de notificacoes do InvestigacaoBR.
    /// Cada tipo de evento usa um icone contextual diferente do GTA V:
    /// todos os CHAR_ da lista abaixo funcionam sem REQUEST (confirmado no wiki RAGE MP).
    /// Para manter consistencia, use SEMPRE estes metodos em vez de Game.DisplayNotification direto.
    /// </summary>
    public static class Notificacao
    {
        // ===== ICONES POR CATEGORIA =====
        // Formato: (txtDict, txtSprite) — para CHAR_XXX, dict == sprite.

        // CHAR_DAVE          → Dave Norton (agente do FIB) = investigacao / detetive
        // CHAR_BLOCKED       → icone de bloqueio              = aviso / negado
        // CHAR_CALL911       → operadora do 911               = alerta / emergencia
        // WEB_LOSSANTOSPOLICEDEPT → logo LSPD                = sucesso / oficial
        // CHAR_MAUDE         → Maude (cacadora de recompensas)= mandado / captura
        // CHAR_MP_FIB_CONTACT→ contato do FIB                 = laboratorio / forense
        // CHAR_FILMNOIR      → estética film noir              = camera / vigilancia
        // CHAR_GANGAPP       → gang app                        = crime organizado / gangue
        // CHAR_AMMUNATION    → Ammu-Nation                    = armamento / balistica
        // CHAR_BANK_MAZE     → Maze Bank                      = crime financeiro
        // CHAR_DETONATEPHONE → telefone de detonacao          = urgente / sequestro
        // CHAR_DETONATEBOMB  → bomba                          = incendio / destruicao
        // CHAR_CARSITE       → site de veiculos               = roubo de veiculo
        // CHAR_LS_CUSTOMS    → LS Customs                     = veiculos roubados

        private const string Policia = "WEB_LOSSANTOSPOLICEDEPT";
        private const string Detetive = "CHAR_DAVE";
        private const string Aviso_ = "CHAR_BLOCKED";
        private const string Emergencia = "CHAR_CALL911";
        private const string Forense = "CHAR_MP_FIB_CONTACT";
        private const string Vigilancia = "CHAR_FILMNOIR";
        private const string Captura = "CHAR_MAUDE";
        private const string Gangues_ = "CHAR_GANGAPP";
        private const string Balistica = "CHAR_AMMUNATION";
        private const string Banco_ = "CHAR_BANK_MAZE";
        private const string Detonacao = "CHAR_DETONATEPHONE";
        private const string Explosao = "CHAR_DETONATEBOMB";
        private const string Automovel = "CHAR_CARSITE";

        // ===== METODO INTERNO =====

        private static void Exibir(string dict, string remetente, string assunto, string mensagem)
            => Game.DisplayNotification(dict, dict, remetente, assunto, mensagem);

        // ===== API PUBLICA — uso geral =====

        /// <summary>Azul — informacoes e atualizacoes da investigacao em curso.</summary>
        public static void Info(string mensagem)
            => Exibir(Detetive, "Detetive", "~b~INVESTIGACAO~w~", mensagem);

        /// <summary>Amarelo — estados intermediarios e lembretes do sistema.</summary>
        public static void Aviso(string mensagem)
            => Exibir(Aviso_, "Sistema", "~y~AVISO~w~", mensagem);

        /// <summary>Vermelho — perigo iminente ou bloqueio critico.</summary>
        public static void Alerta(string mensagem)
            => Exibir(Emergencia, "Dispatch", "~r~ALERTA~w~", mensagem);

        /// <summary>Verde / LSPD — acao concluida com sucesso.</summary>
        public static void Sucesso(string mensagem)
            => Exibir(Policia, "LSPD", "~g~CONCLUIDO~w~", mensagem);

        /// <summary>Roxo — mandado emitido, rastreamento de suspeito ativado.</summary>
        public static void Mandado(string mensagem)
            => Exibir(Captura, "Tribunal", "~p~MANDADO~w~", mensagem);

        /// <summary>Azul / FIB — laudo do laboratorio forense ou match de DNA.</summary>
        public static void Lab(string mensagem)
            => Exibir(Forense, "Lab Forense", "~b~LAUDO FORENSE~w~", mensagem);

        /// <summary>Cinza — resultado de revisao de camera de seguranca.</summary>
        public static void Camera(string mensagem)
            => Exibir(Vigilancia, "Monitoramento", "~c~CAMERA~w~", mensagem);

        // ===== API PUBLICA — por tipo de crime =====

        /// <summary>Laranja / Gang — chacina, trafico, crime organizado.</summary>
        public static void Gangue(string mensagem)
            => Exibir(Gangues_, "Inteligencia", "~o~CRIME ORGANIZADO~w~", mensagem);

        /// <summary>Vermelho / Ammu-Nation — trafico de armas, balistica.</summary>
        public static void Armas(string mensagem)
            => Exibir(Balistica, "Balistica", "~r~ARMAMENTO~w~", mensagem);

        /// <summary>Verde / Maze Bank — lavagem de dinheiro, crimes financeiros.</summary>
        public static void Financeiro(string mensagem)
            => Exibir(Banco_, "Crimes Financeiros", "~g~FINANCEIRO~w~", mensagem);

        /// <summary>Vermelho intenso — sequestro ou situacao de risco de vida.</summary>
        public static void Urgente(string mensagem)
            => Exibir(Detonacao, "Dispatch", "~r~*** URGENTE ***~w~", mensagem);

        /// <summary>Vermelho / bomba — incendio criminoso ou explosao.</summary>
        public static void Incendio(string mensagem)
            => Exibir(Explosao, "Bombeiros", "~r~INCENDIO CRIMINOSO~w~", mensagem);

        /// <summary>Amarelo / carros — roubo de veiculo ou carjacking.</summary>
        public static void Veiculo(string mensagem)
            => Exibir(Automovel, "Patrulha", "~y~ROUBO DE VEICULO~w~", mensagem);

        /// <summary>Vermelho / LSPD — assassinato de policial. Prioridade maxima.</summary>
        public static void Policial(string mensagem)
            => Exibir(Policia, "~r~LSPD~w~", "~r~OFICIAL CAIDO~w~", mensagem);
    }
}