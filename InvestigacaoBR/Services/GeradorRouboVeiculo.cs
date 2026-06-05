using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Roubo de Veiculo / Carjacking: vitima sobrevivente no local (foi tirada do carro).
    /// Carjacker fugiu no veiculo roubado. Alta chance de testemunhas — aconteceu em area publica.
    /// A vitima presente e a testemunha mais valiosa. Camera de rua frequente.
    /// </summary>
    public class GeradorRouboVeiculo : GeradorBase
    {
        private static readonly string[] CenasRouboVeiculo =
        {
            "Individuo armado aborda motorista parado no semaforo. Expulsa a vitima e foge.",
            "Dois suspeitos abordam o veiculo em estacionamento. Vitima reage — levou golpe — larga o carro.",
            "Carjacker em moto para ao lado do veiculo alvo e ameaca a vitima com arma. Roubo relampago.",
            "Vitima abordada ao estacionar. Carjacker conhecia o modelo do veiculo — acao planejada."
        };

        private static readonly string[] InfosCamCarjacking =
        {
            "Suspeito aborda veiculo parado. Gesto de ameaca com objeto. Motorista sai. Veiculo parte.",
            "Individuo aguardando no canto da rua. Aborda o carro assim que para. Fuga rapida.",
            "Camera do posto captura placa do veiculo e rosto do suspeito antes de entrar no carro da vitima."
        };

        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisRouboVeiculo);
            string cena = Aleatorio.Item(CenasRouboVeiculo);

            Caso caso = new Caso("Roubo de Veiculo", cena, agoraInGame);
            caso.Titulo = $"Carjacking #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaCarjacker = Aleatorio.NovoDnaId();

            // ----- Carjacker (culpado — fugiu no veiculo) -----
            PedDoCaso carjacker = CriarPed(PoolsCaso.ModelosSuspeito,
                "Autor do roubo. Fugiu no veiculo da vitima imediatamente apos o ato.",
                RolePed.Indefinido, 0.1f, 0.1f);
            carjacker.NaoSpawnarNaCena = true;
            carjacker.EhCulpadoReal = true;
            carjacker.PerfilDnaId = dnaCarjacker;
            carjacker.RegistroTelefonico = "Mensagens para receptador de veiculos momentos depois. Contato com desmanche ilegal.";
            if (local != null)
            {
                carjacker.LocalConhecidoX = local.X + Aleatorio.Real(-50f, 50f);
                carjacker.LocalConhecidoY = local.Y + Aleatorio.Real(-50f, 50f);
                carjacker.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(carjacker);

            // ----- Vitima (presente, traumatizada, 2-5 m) -----
            PedDoCaso vitima = CriarPed(PoolsCaso.ModelosVitima,
                "Proprietario do veiculo. Presente no local — pode descrever o autor e o veiculo.",
                RolePed.Testemunha, 2f, 5f);
            caso.AdicionarPed(vitima);

            // ----- Testemunha 1 (passante, 6-12 m) -----
            PedDoCaso testemunha1 = CriarPed(PoolsCaso.ModelosCivil,
                "Transeunte que presenciou o roubo. Possivelmente gravou pelo celular.", RolePed.Indefinido, 6f, 12f);
            caso.AdicionarPed(testemunha1);

            // ----- Testemunha 2 (opcional, 10-18 m) -----
            if (Aleatorio.Chance(60))
            {
                PedDoCaso testemunha2 = CriarPed(PoolsCaso.ModelosCivil,
                    "Segundo passante. Ouviu gritos e viu o veiculo partir.", RolePed.Indefinido, 10f, 18f);
                caso.AdicionarPed(testemunha2);
            }

            // ----- Evidencias -----
            Evidencia chaves = new Evidencia("Chaves da vitima",
                "Chaveiro da vitima derrubado durante o confronto — sangue na corrente.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                ResultadoForense = "DNA do carjacker na parte metalica — forcou a chave da mao da vitima."
            };
            AplicarOffsetEvidencia(chaves, 0.5f, 2f);
            caso.AdicionarEvidencia(chaves);

            Evidencia item = new Evidencia("Item do carjacker",
                "Objeto caido do bolso do assaltante durante o embarque precipitado no veiculo.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                PerfilDnaId = dnaCarjacker,
                ResultadoForense = "DNA isolado. Item comprado ha dois dias em loja do bairro — rastreavel."
            };
            AplicarOffsetEvidencia(item, 0.5f, 2.5f);
            caso.AdicionarEvidencia(item);

            if (Aleatorio.Chance(50))
            {
                Evidencia arma = new Evidencia("Arma usada na abordagem",
                    "Faca ou objeto cortante descartado proximo ao local apos o roubo.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsArmaBranca),
                    PerfilDnaId = dnaCarjacker,
                    ResultadoForense = "Impressao digital e DNA do suspeito. Compativel com o relato da vitima."
                };
                AplicarOffsetEvidencia(arma, 1f, 4f);
                caso.AdicionarEvidencia(arma);
            }

            // ----- Camera (rua / posto) — alta chance -----
            caso.AdicionarCamera(CriarCamera(
                "Camera de rua / estabelecimento",
                Aleatorio.Item(InfosCamCarjacking),
                caso.CenaX, caso.CenaY, caso.CenaZ,
                Aleatorio.Real(-8f, 8f), Aleatorio.Real(-8f, 8f), 4f));

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorRouboVeiculo: '{caso.Titulo}' ({caso.Peds.Count} peds, {caso.Evidencias.Count} ev).");
            return caso;
        }
    }
}