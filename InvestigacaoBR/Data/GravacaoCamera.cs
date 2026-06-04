using System;
using InvestigacaoBR.Core;

namespace InvestigacaoBR.Data
{
    /// <summary>
    /// Uma camera de seguranca da area do caso. Guarda a posicao e o alvo (para a renderizacao
    /// nativa pelo CameraService) e a INFO AUTORADA que a "gravacao" revela. A info so deve ser
    /// exibida apos o jogador revisar a camera (Revisada == true).
    ///
    /// Tudo e persistido. Posicao/alvo ficam como floats para serializar limpo (sem Rage.Vector3).
    /// </summary>
    public class GravacaoCamera
    {
        /// <summary>Identidade estavel da camera.</summary>
        public Guid Id { get; set; }

        /// <summary>Rotulo/local da camera (ex.: "Camera da loja - Vinewood Blvd").</summary>
        public string Local { get; set; }

        /// <summary>Posicao X da camera no mundo.</summary>
        public float PosX { get; set; }

        /// <summary>Posicao Y da camera no mundo.</summary>
        public float PosY { get; set; }

        /// <summary>Posicao Z da camera no mundo.</summary>
        public float PosZ { get; set; }

        /// <summary>X do ponto que a camera encara (alvo).</summary>
        public float AlvoX { get; set; }

        /// <summary>Y do ponto que a camera encara (alvo).</summary>
        public float AlvoY { get; set; }

        /// <summary>Z do ponto que a camera encara (alvo).</summary>
        public float AlvoZ { get; set; }

        /// <summary>Campo de visao (FOV) em graus. Padrao 50.</summary>
        public float Fov { get; set; }

        /// <summary>INFO AUTORADA revelada pela gravacao (ex.: descricao do suspeito, placa parcial).</summary>
        public string InfoRevelada { get; set; }

        /// <summary>Se o jogador ja revisou esta camera. Libera a exibicao da InfoRevelada na UI.</summary>
        public bool Revisada { get; set; }

        /// <summary>Construtor sem parametros: OBRIGATORIO para serializacao XML. Nao remover.</summary>
        public GravacaoCamera()
        {
            Id = Guid.NewGuid();
            Local = string.Empty;
            Fov = 50f;
            InfoRevelada = string.Empty;
            Revisada = false;
        }

        /// <summary>Conveniencia: cria uma camera ja com o rotulo do local.</summary>
        public GravacaoCamera(string local) : this()
        {
            Local = string.IsNullOrEmpty(local) ? string.Empty : local;
        }

        /// <summary>Marca a camera como revisada (libera a info na UI) e loga. Idempotente.</summary>
        public bool MarcarRevisada()
        {
            if (Revisada)
            {
                Logger.Info($"MarcarRevisada ignorado para camera '{Local}': ja revisada.");
                return false;
            }

            Revisada = true;
            Logger.State($"Camera '{Local}'", "Nao revisada", "Revisada");
            return true;
        }
    }
}