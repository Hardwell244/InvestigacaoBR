using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Latrocinio: roubo a estabelecimento ou transeunte que resultou em morte.
    /// Vitima morta no local. Assaltante fugiu. Testemunhas presentes.
    /// Camera de seguranca e elemento central — unica forma de identificar o rosto.
    /// Culpado NAO esta na cena — localizado via mandado na segunda locacao.
    /// </summary>
    public class GeradorLatrocinio : GeradorBase
    {
        private static readonly string[] TiposRoubo =
        {
            "assalto a posto de gasolina",
            "assalto a comercio local",
            "assalto a transeunte"
        };

        private static readonly string[] DescricoesAssaltante =
        {
            "Individuo de capuz escuro, armado. Atirou apos resistencia da vitima.",
            "Individuo de mascara, armado com faca. Golpeou a vitima ao tentar fugir.",
            "Dois assaltantes armados. Um deles atirou ao sentir pressao. Fugiram em veiculo."
        };

        private static readonly string[] InfosCamLatrocinio =
        {
            "Individuo encapuzado entra no estabelecimento armado. Atira na vitima e foge com o caixa.",
            "Assaltante aborda a vitima na rua. Violencia ao encontrar resistencia. Foge a pe sentido norte.",
            "Dois individuos em veiculo: um entra, o outro aguarda. Saem rapidamente apos os tiros."
        };

        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisLatrocinio);
            string tipoRoubo = Aleatorio.Item(TiposRoubo);
            string descAssalt = Aleatorio.Item(DescricoesAssaltante);

            Caso caso = new Caso("Latrocinio",
                $"Vitima fatal de {tipoRoubo}. {descAssalt}", agoraInGame);
            caso.Titulo = $"Latrocinio #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaAssaltante = Aleatorio.NovoDnaId();

            // ----- Vitima (morta, centro da cena) -----
            PedDoCaso vitima = CriarPed(PoolsCaso.ModelosVitima,
                "Vitima fatal do latrocinio.", RolePed.Indefinido, 0f, 0.3f);
            vitima.SpawnarMorto = true;
            vitima.Heading = caso.CenaHeading;
            vitima.OffsetX = 0f; vitima.OffsetY = 0f; vitima.OffsetZ = 0f;
            caso.AdicionarPed(vitima);

            // ----- Assaltante (culpado, NAO esta na cena — segunda locacao) -----
            PedDoCaso assaltante = CriarPed(PoolsCaso.ModelosSuspeito,
                "Assaltante responsavel pela morte. Fugiu do local apos o crime.", RolePed.Indefinido, 0f, 0.1f);
            // Ped nao aparece na cena — SpawnarMorto = false, mas offset zero = spawn no centro
            // Solucao: por enquanto deixamos ele como presente mas distante
            assaltante.OffsetX = 999f; assaltante.OffsetY = 999f; // fora do alcance de spawn (hack)
            assaltante.EhCulpadoReal = true;
            assaltante.PerfilDnaId = dnaAssaltante;
            assaltante.RegistroTelefonico = "Chamadas para receptador de produtos furtados. Compras suspeitas em cash horas depois.";
            if (local != null)
            {
                // Segunda locacao: apartamento ou esconderijo do assaltante
                assaltante.LocalConhecidoX = local.X + Aleatorio.Real(-60f, 60f);
                assaltante.LocalConhecidoY = local.Y + Aleatorio.Real(-60f, 60f);
                assaltante.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(assaltante);

            // ----- Testemunha 1 (presente na cena, 4-8 m) -----
            PedDoCaso testemunha1 = CriarPed(PoolsCaso.ModelosCivil,
                "Funcionario ou transeunte que presenciou o ataque.", RolePed.Testemunha, 4f, 8f);
            caso.AdicionarPed(testemunha1);

            // ----- Testemunha 2 (opcional, mais distante, 10-16 m) -----
            if (Aleatorio.Chance(70))
            {
                PedDoCaso testemunha2 = CriarPed(PoolsCaso.ModelosCivil,
                    "Passante que viu o assaltante fugindo.", RolePed.Indefinido, 10f, 16f);
                caso.AdicionarPed(testemunha2);
            }

            // ----- Sangue -----
            Evidencia sangue = new Evidencia("Mancha de sangue",
                "Poca de sangue extensa ao redor do corpo da vitima.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsSangue),
                ResultadoForense = "Sangue da vitima. Perfil DNA confirmado."
            };
            AplicarOffsetEvidencia(sangue, 0.1f, 0.5f);
            caso.AdicionarEvidencia(sangue);

            // ----- Item roubado + descartado na fuga -----
            bool armaDeFogo = Aleatorio.Chance(60);
            if (armaDeFogo)
            {
                Evidencia arma = new Evidencia("Arma do crime",
                    "Arma usada no latrocinio — descartada proximo ao local.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                    PerfilDnaId = dnaAssaltante,
                    ResultadoForense = "Impressao digital parcial e DNA do suspeito recuperados. Arma sem registro."
                };
                AplicarOffsetEvidencia(arma, 3f, 8f);
                caso.AdicionarEvidencia(arma);
            }
            else
            {
                Evidencia faca = new Evidencia("Arma branca",
                    "Faca descartada pelo assaltante durante a fuga.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsArmaBranca),
                    PerfilDnaId = dnaAssaltante,
                    ResultadoForense = "Sangue da vitima na lamina. DNA do assaltante na empunhadura."
                };
                AplicarOffsetEvidencia(faca, 3f, 8f);
                caso.AdicionarEvidencia(faca);
            }

            // ----- Objeto pessoal da vitima (espalhado no chao) -----
            Evidencia itemVitima = new Evidencia("Pertences da vitima",
                "Carteira ou celular da vitima jogado no chao durante o roubo.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                ResultadoForense = "Pertences identificados. Dinheiro e documentos ausentes — levados pelo assaltante."
            };
            AplicarOffsetEvidencia(itemVitima, 0.5f, 2f);
            caso.AdicionarEvidencia(itemVitima);

            // ----- Camera (peca central deste caso) -----
            caso.AdicionarCamera(CriarCamera(
                "Camera de seguranca — estabelecimento",
                Aleatorio.Item(InfosCamLatrocinio),
                caso.CenaX, caso.CenaY, caso.CenaZ,
                Aleatorio.Real(-8f, 8f), Aleatorio.Real(-8f, 8f), 4f));

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorLatrocinio: '{caso.Titulo}' ({tipoRoubo}, {caso.Peds.Count} peds, {caso.Evidencias.Count} ev).");
            return caso;
        }
    }
}