using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Sequestro: vitima foi abduzida. A CENA mostra ONDE aconteceu o rapto —
    /// pertences espalhados, sinais de luta. O culpado NAO esta na cena.
    /// A vitima esta sendo mantida na segunda locacao (revelada via mandado).
    /// Camera de rua e a chave para identificar o veiculo e o rosto do raptor.
    /// </summary>
    public class GeradorSequestro : GeradorBase
    {
        private static readonly string[] MotivoSequestro =
        {
            "Sequestro para resgate. Vitima e parente de empresario local.",
            "Abduzida por divida de jogo nao paga. Retida como pressao.",
            "Desaparecimento forcado ligado a testemunho em processo criminal em andamento.",
            "Raptor com historico de perseguicao. Vitima relatou ameacas a policia semanas antes."
        };

        private static readonly string[] InfosCamSequestro =
        {
            "Van escura para ao lado da vitima. Dois individuos a forçam para dentro. Placa parcialmente visivel.",
            "Vitima abordada por individuo a pe. Forcada para veiculo estacionado. Fuga em alta velocidade.",
            "SUV escuro, janelas fumê. Para, abre porta. Vitima arrastada sem resistencia visivel na gravacao."
        };

        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisSequestro);
            string motivo = Aleatorio.Item(MotivoSequestro);

            Caso caso = new Caso("Sequestro", motivo, agoraInGame);
            caso.Titulo = $"Sequestro #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaRaptor = Aleatorio.NovoDnaId();

            // ----- Raptor principal (NAO esta na cena — esta no cativeiro) -----
            PedDoCaso raptor = CriarPed(PoolsCaso.ModelosSuspeito,
                "Suspeito do rapto. Localizado no cativeiro via mandado.", RolePed.Indefinido, 0f, 0.1f);
            raptor.NaoSpawnarNaCena = true; // fugiu com a vitima — nao aparece aqui
            raptor.EhCulpadoReal = true;
            raptor.PerfilDnaId = dnaRaptor;
            raptor.RegistroTelefonico = "Chamadas anonimas exigindo resgate. Numero rastreado a celular pre-pago comprado em cash.";
            if (local != null)
            {
                raptor.LocalConhecidoX = local.X + Aleatorio.Real(-80f, 80f);
                raptor.LocalConhecidoY = local.Y + Aleatorio.Real(-80f, 80f);
                raptor.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(raptor);

            // ----- Testemunha 1 (viu o rapto, 5-10 m) -----
            PedDoCaso testemunha1 = CriarPed(PoolsCaso.ModelosCivil,
                "Transeunte que presenciou a abducao. Visivelmente abalado.", RolePed.Testemunha, 5f, 10f);
            caso.AdicionarPed(testemunha1);

            // ----- Testemunha 2 (opcional, mais distante, 12-20 m) -----
            if (Aleatorio.Chance(60))
            {
                PedDoCaso testemunha2 = CriarPed(PoolsCaso.ModelosCivil,
                    "Morador local que ouviu gritos e viu o veiculo partir.", RolePed.Indefinido, 12f, 20f);
                caso.AdicionarPed(testemunha2);
            }

            // ----- Evidencias: pertences da vitima -----
            Evidencia bolsa = new Evidencia("Bolsa da vitima",
                "Bolsa abandonada no local — pertences espalhados. Sinal de resistencia.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                ResultadoForense = "Pertences identificados como da vitima. Impressao digital de terceiro na alcа."
            };
            AplicarOffsetEvidencia(bolsa, 0.5f, 2f);
            caso.AdicionarEvidencia(bolsa);

            Evidencia celular = new Evidencia("Celular da vitima",
                "Aparelho danificado encontrado no chao — indica queda forcada.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                ResultadoForense = "Ultima localizacao GPS registrada coincide com esta cena. Chamada abortada para emergencia."
            };
            AplicarOffsetEvidencia(celular, 0.3f, 1.5f);
            caso.AdicionarEvidencia(celular);

            // ----- Item do raptor (DNA na cena) -----
            Evidencia itemRaptor = new Evidencia("Item do raptor",
                "Objeto deixado pelo autor durante o rapto precipitado.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                PerfilDnaId = dnaRaptor,
                ResultadoForense = "DNA na superficie — perfil isolado. Nao consta em registros anteriores."
            };
            AplicarOffsetEvidencia(itemRaptor, 1f, 3f);
            caso.AdicionarEvidencia(itemRaptor);

            // ----- Marcas do veiculo (evidencia textual) -----
            Evidencia marcas = new Evidencia("Marcas de frenagem",
                $"Rastros de pneu no asfalto — fuga precipitada. Largura compativel com van ou SUV.")
            {
                ResultadoForense = $"Bitola de {Aleatorio.Real(1.5f, 1.9f):F2} m. Pneu de aro {Aleatorio.Inteiro(17, 20)} pol. Possivelmente van de carga ou SUV grande."
            };
            AplicarOffsetEvidencia(marcas, 2f, 5f);
            caso.AdicionarEvidencia(marcas);

            // ----- Camera (essencial — identifica veiculo) -----
            caso.AdicionarCamera(CriarCamera(
                "Camera de vigilancia — via publica",
                Aleatorio.Item(InfosCamSequestro),
                caso.CenaX, caso.CenaY, caso.CenaZ,
                Aleatorio.Real(-10f, 10f), Aleatorio.Real(-10f, 10f), 5f));

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorSequestro: '{caso.Titulo}' ({caso.Peds.Count} peds, {caso.Evidencias.Count} ev).");
            return caso;
        }
    }
}