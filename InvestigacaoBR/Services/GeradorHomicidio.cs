using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    public class GeradorHomicidio : GeradorBase
    {
        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisHomicidio);

            Caso caso = new Caso("Homicidio", "Investigacao de homicidio. Vitima encontrada na cena.", agoraInGame);
            caso.Titulo = $"Homicidio #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaCulpado = Aleatorio.NovoDnaId();
            string dnaVitima = Aleatorio.NovoDnaId();

            // ----- Vitima (morta, centro da cena) -----
            PedDoCaso vitima = CriarPed(PoolsCaso.ModelosVitima, "Vitima do homicidio.", RolePed.Indefinido, 0f, 0.3f);
            vitima.SpawnarMorto = true;
            vitima.Heading = caso.CenaHeading;
            vitima.OffsetX = 0f; vitima.OffsetY = 0f; vitima.OffsetZ = 0f;
            vitima.PerfilDnaId = dnaVitima;
            caso.AdicionarPed(vitima);

            // ----- Culpado (longe da vitima) -----
            PedDoCaso culpado = CriarPed(PoolsCaso.ModelosSuspeito,
                "Pessoa avistada na area no horario do crime.", RolePed.Indefinido, 8f, 14f);
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

            // ----- Civis / testemunhas (perto da cena) -----
            int qtdCivis = Aleatorio.Inteiro(2, 3);
            foreach (string modelo in Aleatorio.Itens(PoolsCaso.ModelosCivil, qtdCivis))
            {
                PedDoCaso civil = CriarPed(PoolsCaso.ModelosCivil,
                    "Pessoa nas redondezas no momento do fato.", RolePed.Indefinido, 3f, 6f);
                civil.ModeloPed = modelo;
                caso.AdicionarPed(civil);
            }

            // ----- Arma + evidencias -----
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
                    ResultadoForense = "Sangue da vitima na lamina e DNA de terceiro na empunhadura."
                };
                AplicarOffsetEvidencia(faca, 0.5f, 3f);
                caso.AdicionarEvidencia(faca);
            }

            // ----- B5: poca de sangue com prop visivel + decal como backup -----
            Evidencia sangue = new Evidencia("Mancha de sangue", "Poca de sangue ao redor do corpo.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsSangue),  // prop visivel no chao
                PerfilDnaId = dnaVitima,
                ResultadoForense = "Sangue compativel com a vitima. DNA confirmado."
            };
            AplicarOffsetEvidencia(sangue, 0.1f, 0.5f);  // perto do corpo
            caso.AdicionarEvidencia(sangue);

            // ----- Cameras -----
            string[] infosCam =
            {
                "Individuo de moletom escuro deixando o local em passos apressados.",
                "Veiculo sedan escuro parado nas proximidades minutos antes.",
                "Figura encapuzada cruzando o enquadramento em direcao a saida."
            };
            int qtdCam = Aleatorio.Inteiro(0, 2);
            for (int i = 0; i < qtdCam; i++)
            {
                caso.AdicionarCamera(CriarCamera(
                    $"Camera de seguranca #{i + 1}", Aleatorio.Item(infosCam),
                    caso.CenaX, caso.CenaY, caso.CenaZ,
                    Aleatorio.Real(-12f, 12f), Aleatorio.Real(-12f, 12f), 4f));
            }

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorHomicidio: '{caso.Titulo}' ({(armaDeFogo ? "fogo" : "branca")}, {caso.Peds.Count} peds, {caso.Evidencias.Count} ev, {caso.Cameras.Count} cam).");
            return caso;
        }
    }
}