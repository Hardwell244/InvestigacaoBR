using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Gera um caso de homicidio aleatorio: local sorteado, vitima morta no centro, arma
    /// sorteada (fogo -> capsulas + item com DNA / branca -> faca com DNA), sangue da vitima,
    /// culpado plantado com DNA, inocentes/testemunhas e 0-2 cameras. A verdade congela aqui.
    /// </summary>
    public class GeradorHomicidio : GeradorBase
    {
        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisHomicidio);

            Caso caso = new Caso("Homicidio", "Investigacao de homicidio. Vitima encontrada na cena.", agoraInGame);
            caso.Titulo = $"Homicidio #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X;
                caso.CenaY = local.Y;
                caso.CenaZ = local.Z;
                caso.CenaHeading = local.Heading;
            }

            string dnaCulpado = Aleatorio.NovoDnaId();
            string dnaVitima = Aleatorio.NovoDnaId();

            // ----- Vitima (morta, no centro da cena) -----
            PedDoCaso vitima = CriarPed(PoolsCaso.ModelosVitima, "Vitima do homicidio.", RolePed.Indefinido, 0f, 0.5f);
            vitima.SpawnarMorto = true;
            vitima.Heading = caso.CenaHeading;
            vitima.OffsetX = 0f;
            vitima.OffsetY = 0f;
            vitima.OffsetZ = 0f;
            vitima.PerfilDnaId = dnaVitima;
            caso.AdicionarPed(vitima);

            // ----- Culpado (presente, nao classificado, DNA plantado) -----
            PedDoCaso culpado = CriarPed(PoolsCaso.ModelosSuspeito, "Pessoa avistada na area no horario do crime.", RolePed.Indefinido, 4f, 9f);
            culpado.EhCulpadoReal = true;
            culpado.PerfilDnaId = dnaCulpado;
            culpado.RegistroTelefonico = "Chamadas para numero pre-pago minutos antes e depois do horario do crime.";
            if (local != null)
            {
                culpado.LocalConhecidoX = local.X + Aleatorio.Real(-15f, 15f);
                culpado.LocalConhecidoY = local.Y + Aleatorio.Real(-15f, 15f);
                culpado.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(culpado);

            // ----- Inocentes / testemunhas (2 a 3, modelos distintos) -----
            int qtdCivis = Aleatorio.Inteiro(2, 3);
            foreach (string modelo in Aleatorio.Itens(PoolsCaso.ModelosCivil, qtdCivis))
            {
                PedDoCaso civil = CriarPed(PoolsCaso.ModelosCivil, "Pessoa nas redondezas no momento do fato.", RolePed.Indefinido, 5f, 12f);
                civil.ModeloPed = modelo; // garante distintos
                caso.AdicionarPed(civil);
            }

            // ----- Arma sorteada + evidencias -----
            bool armaDeFogo = Aleatorio.Chance(50);
            if (armaDeFogo)
            {
                caso.DescricaoGeral += " Indicios de disparo de arma de fogo.";

                int qtdCapsulas = Aleatorio.Inteiro(1, 3);
                for (int i = 0; i < qtdCapsulas; i++)
                {
                    Evidencia capsula = new Evidencia("Capsula de projetil", "Capsula deflagrada proxima ao corpo.")
                    {
                        ModeloProp = Aleatorio.Item(PoolsCaso.PropsCapsula),
                        ResultadoForense = "Calibre 9mm. Compativel com pistola semiautomatica."
                    };
                    AplicarOffsetEvidencia(capsula, 0.5f, 3f);
                    caso.AdicionarEvidencia(capsula);
                }

                // O DNA do culpado vem de um item pessoal deixado no local
                Evidencia item = new Evidencia("Item pessoal", "Objeto deixado pelo autor no local.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                    PerfilDnaId = dnaCulpado,
                    ResultadoForense = "Material genetico recuperado. Perfil de DNA isolado."
                };
                AplicarOffsetEvidencia(item, 1f, 4f);
                caso.AdicionarEvidencia(item);
            }
            else
            {
                caso.DescricaoGeral += " Ferimentos compativeis com arma branca.";

                Evidencia faca = new Evidencia("Arma branca", "Faca ensanguentada abandonada na cena.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsArmaBranca),
                    PerfilDnaId = dnaCulpado,
                    ResultadoForense = "Sangue da vitima na lamina e material genetico de terceiro na empunhadura."
                };
                AplicarOffsetEvidencia(faca, 0.5f, 3f);
                caso.AdicionarEvidencia(faca);
            }

            // ----- Sangue (DNA da vitima, sem prop visual) -----
            Evidencia sangue = new Evidencia("Mancha de sangue", "Poca de sangue ao redor do corpo.")
            {
                PerfilDnaId = dnaVitima,
                ResultadoForense = "Sangue compativel com a vitima."
            };
            AplicarOffsetEvidencia(sangue, 0.3f, 1.5f);
            caso.AdicionarEvidencia(sangue);

            // ----- Cameras (0 a 2) -----
            string[] infosCam =
            {
                "Individuo de moletom escuro deixando o local em passos apressados.",
                "Veiculo sedan escuro parado nas proximidades minutos antes.",
                "Figura encapuzada cruzando o enquadramento em direcao a saida."
            };
            int qtdCam = Aleatorio.Inteiro(0, 2);
            for (int i = 0; i < qtdCam; i++)
            {
                GravacaoCamera cam = CriarCamera(
                    $"Camera de seguranca #{i + 1}",
                    Aleatorio.Item(infosCam),
                    caso.CenaX, caso.CenaY, caso.CenaZ,
                    Aleatorio.Real(-12f, 12f), Aleatorio.Real(-12f, 12f), 4f);
                caso.AdicionarCamera(cam);
            }

            Logger.Info($"GeradorHomicidio: '{caso.Titulo}' gerado (arma {(armaDeFogo ? "de fogo" : "branca")}, {caso.Peds.Count} peds, {caso.Evidencias.Count} evidencias, {caso.Cameras.Count} cameras).");
            return caso;
        }
    }
}