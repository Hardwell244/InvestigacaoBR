using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Assassinato de Policial: oficial abatido em servico. Maximo dramatismo.
    /// Parceiro (testemunha) presente e chave para a investigacao.
    /// Evidencias militares: capsulas, radio, distintivo. Assassino identificado
    /// por camera ou relato do parceiro. Alta prioridade — mandado imediato.
    /// </summary>
    public class GeradorAssassinatoPolicial : GeradorBase
    {
        private static readonly string[] CenasAssassinato =
        {
            "Oficial abordou veiculo suspeito numa blitz. Ocupante atirou e fugiu a pe.",
            "Patrulha foi emboscada. Oficial desembarcou para verificar relato. Ataque surpresa.",
            "Resposta a chamado falso atraiu o oficial a local isolado. Assassinato planejado.",
            "Tentativa de prisao de suspeito foragido. Oficial desarmado e executado."
        };

        private static readonly string[] InfosCamAssassinato =
        {
            "Oficial aborda veiculo. Ocupante dispara por janela aberta e foge correndo.",
            "Suspeito se aproxima pela retaguarda do oficial desprevenido. Varios disparos. Fuga a pe.",
            "Veiculo para, dois individuos descem, atiram no oficial e reentram. Fuga em alta velocidade."
        };

        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisHomicidio); // cenas urbanas abertas
            string cena = Aleatorio.Item(CenasAssassinato);

            Caso caso = new Caso("Assassinato de Policial", cena, agoraInGame);
            caso.Titulo = $"Oficiais Caidos #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";
            caso.DescricaoGeral = $"PRIORIDADE MAXIMA. {cena} Assassino identificado — mandado pendente.";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaAssassino = Aleatorio.NovoDnaId();

            // ----- Oficial vitima (morto, centro) -----
            PedDoCaso oficial = CriarPed(PoolsCaso.ModelosPolicia,
                "Oficial de policia vitimado em servico.", RolePed.Indefinido, 0f, 0.3f);
            oficial.SpawnarMorto = true;
            oficial.Heading = caso.CenaHeading;
            oficial.OffsetX = 0f; oficial.OffsetY = 0f; oficial.OffsetZ = 0f;
            caso.AdicionarPed(oficial);

            // ----- Assassino (culpado — fugiu) -----
            PedDoCaso assassino = CriarPed(PoolsCaso.ModelosSuspeito,
                "Autor do homicidio. Fugiu imediatamente apos os tiros.", RolePed.Indefinido, 0.1f, 0.1f);
            assassino.NaoSpawnarNaCena = true;
            assassino.EhCulpadoReal = true;
            assassino.PerfilDnaId = dnaAssassino;
            assassino.Procurado = true;
            assassino.RegistroTelefonico = "Historico criminal extenso. Mandado ativo de prisao anterior. Comunicacao com organizacao criminosa.";
            if (local != null)
            {
                assassino.LocalConhecidoX = local.X + Aleatorio.Real(-60f, 60f);
                assassino.LocalConhecidoY = local.Y + Aleatorio.Real(-60f, 60f);
                assassino.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(assassino);

            // ----- Parceiro (testemunha principal, 4-8 m) -----
            PedDoCaso parceiro = CriarPed(PoolsCaso.ModelosPolicia,
                "Parceiro do oficial vitimado. Presente na cena. Pode descrever o autor em detalhe.",
                RolePed.Testemunha, 4f, 8f);
            caso.AdicionarPed(parceiro);

            // ----- Civil testemunha (opcional, 15-25 m) -----
            if (Aleatorio.Chance(70))
            {
                PedDoCaso civil = CriarPed(PoolsCaso.ModelosCivil,
                    "Civil que presenciou o ataque de longe. Em estado de choque.", RolePed.Indefinido, 15f, 25f);
                caso.AdicionarPed(civil);
            }

            // ----- Sangue do oficial -----
            Evidencia sangue = new Evidencia("Local do abatimento",
                "Mancha de sangue do oficial. Posicao indica direcao de onde veio o disparo.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsSangue),
                ResultadoForense = "Sangue do oficial confirmado. Angulo de entrada do projetil: costas / lateral."
            };
            AplicarOffsetEvidencia(sangue, 0.1f, 0.5f);
            caso.AdicionarEvidencia(sangue);

            // ----- Capsulas (multiplas, alto calibre) -----
            int qtdCapsulas = Aleatorio.Inteiro(3, 6);
            for (int i = 0; i < qtdCapsulas; i++)
            {
                Evidencia capsula = new Evidencia($"Capsula #{i + 1} — alto calibre",
                    ".357 ou .45. Compativel com revolver ou pistola de grande porte.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsCapsula),
                    PerfilDnaId = i == 0 ? dnaAssassino : null,
                    ResultadoForense = i == 0
                        ? "Impressao digital parcial. DNA isolado — assassino identificado."
                        : "Sem identificador. Mesmo calibre dos demais."
                };
                AplicarOffsetEvidencia(capsula, 1f, 8f);
                caso.AdicionarEvidencia(capsula);
            }

            // ----- Distintivo / equipamento do oficial -----
            Evidencia distintivo = new Evidencia("Distintivo do oficial",
                "Badge derrubado durante o ataque. Marca de impacto visivel.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsFerramenta),
                ResultadoForense = "Impressao digital do assassino no verso do distintivo — tocou para confirmar o abatimento."
            };
            AplicarOffsetEvidencia(distintivo, 0.5f, 2f);
            caso.AdicionarEvidencia(distintivo);

            // ----- Camera (alta chance — area policiada) -----
            caso.AdicionarCamera(CriarCamera(
                "Camera do sistema de monitoramento LSPD",
                Aleatorio.Item(InfosCamAssassinato),
                caso.CenaX, caso.CenaY, caso.CenaZ,
                Aleatorio.Real(-12f, 12f), Aleatorio.Real(-12f, 12f), 5f));

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorAssassinatoPolicial: '{caso.Titulo}' ({caso.Peds.Count} peds, {caso.Evidencias.Count} ev).");
            return caso;
        }
    }
}