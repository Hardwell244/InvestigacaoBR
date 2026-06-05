using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Invasao / Arrombamento: residencia ou comercio invadido.
    /// Ladroes (1-2) ainda na cena OU fugiram. Vitima pode estar presente (sobrevivente)
    /// ou o local estava vazio. Vizinho como testemunha frequente.
    /// </summary>
    public class GeradorInvasao : GeradorBase
    {
        private static readonly string[] TiposInvasao =
        {
            "Residencia de alto padrao invadida. Vitima encontrada amarrada.",
            "Comercio local arrombado na madrugada. Cofre forcado.",
            "Apartamento violado durante ausencia do morador. Joias e eletronicos furtados.",
            "Casa invadida. Morador surpreendeu os ladroes — confronto fisico."
        };

        private static readonly string[] InfosCamInvasao =
        {
            "Individuo forcando a fechadura da entrada. Segundo aguarda do lado de fora.",
            "Figura encapuzada saindo pela janela carregando objeto volumoso.",
            "Van estacionada em frente ao imovel. Dois individuos carregam itens para dentro."
        };

        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisInvasao);
            string tipo = Aleatorio.Item(TiposInvasao);
            bool ladraoNaCena = Aleatorio.Chance(55);
            bool vitimaNaCena = Aleatorio.Chance(65);

            Caso caso = new Caso("Invasao / Arrombamento", tipo, agoraInGame);
            caso.Titulo = $"Invasao #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaLadrao = Aleatorio.NovoDnaId();

            // ----- Ladrao principal (culpado) -----
            PedDoCaso ladrao = CriarPed(PoolsCaso.ModelosSuspeito,
                "Suspeito do arrombamento. " + (ladraoNaCena ? "Ainda na cena." : "Fugiu com os itens."),
                RolePed.Indefinido,
                ladraoNaCena ? 2f : 0.1f,
                ladraoNaCena ? 8f : 0.1f);

            if (!ladraoNaCena) ladrao.NaoSpawnarNaCena = true;

            ladrao.EhCulpadoReal = true;
            ladrao.PerfilDnaId = dnaLadrao;
            ladrao.RegistroTelefonico = "Mensagens combinando o horario da acao com comparsa. Receptador contatado para venda dos itens.";
            if (local != null)
            {
                ladrao.LocalConhecidoX = local.X + Aleatorio.Real(-40f, 40f);
                ladrao.LocalConhecidoY = local.Y + Aleatorio.Real(-40f, 40f);
                ladrao.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(ladrao);

            // ----- Comparsa (opcional, 4-10 m) -----
            if (Aleatorio.Chance(50))
            {
                PedDoCaso comparsa = CriarPed(PoolsCaso.ModelosSuspeito,
                    "Segundo individuo envolvido no arrombamento.", RolePed.Indefinido,
                    ladraoNaCena ? 4f : 0.1f, ladraoNaCena ? 10f : 0.1f);
                if (!ladraoNaCena) comparsa.NaoSpawnarNaCena = true;
                caso.AdicionarPed(comparsa);
            }

            // ----- Vitima / morador (opcional, 6-12 m) -----
            if (vitimaNaCena)
            {
                PedDoCaso vitima = CriarPed(PoolsCaso.ModelosVitima,
                    "Vitima do arrombamento. Encontrada no local — pode relatar o que viu.",
                    RolePed.Testemunha, 6f, 12f);
                caso.AdicionarPed(vitima);
            }

            // ----- Vizinho (testemunha, 15-25 m) -----
            PedDoCaso vizinho = CriarPed(PoolsCaso.ModelosCivil,
                "Vizinho que ouviu barulho e chamou a policia. Viu o veiculo dos invasores.",
                RolePed.Testemunha, 15f, 25f);
            caso.AdicionarPed(vizinho);

            // ----- Evidencias -----
            Evidencia ferramenta = new Evidencia("Ferramenta de arrombamento",
                "Pe-de-cabra ou corta-cadeado deixado pelos invasores na fuga.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsFerramenta),
                PerfilDnaId = dnaLadrao,
                ResultadoForense = "Impressao digital e DNA do suspeito. Marcas na ferramenta coincidem com o arrombamento."
            };
            AplicarOffsetEvidencia(ferramenta, 0.5f, 2.5f);
            caso.AdicionarEvidencia(ferramenta);

            Evidencia eletronico = new Evidencia("Eletronico abandonado",
                "Aparelho derrubado na fuga — parte dos bens furtados.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsEletronico),
                ResultadoForense = "Numero de serie rastreado ao proprietario. Impressao digital do suspeito na carcaca."
            };
            AplicarOffsetEvidencia(eletronico, 1f, 4f);
            caso.AdicionarEvidencia(eletronico);

            Evidencia luva = new Evidencia("Luva descartada",
                "Luva cirurgica ou de procedimento descartada proximo ao ponto de entrada.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                PerfilDnaId = dnaLadrao,
                ResultadoForense = "DNA interno da luva — perfil do suspeito isolado com exito."
            };
            AplicarOffsetEvidencia(luva, 1f, 3f);
            caso.AdicionarEvidencia(luva);

            // ----- Camera (vizinho ou condominio) -----
            if (Aleatorio.Chance(75))
            {
                caso.AdicionarCamera(CriarCamera(
                    "Camera do condominio / vizinho",
                    Aleatorio.Item(InfosCamInvasao),
                    caso.CenaX, caso.CenaY, caso.CenaZ,
                    Aleatorio.Real(-10f, 10f), Aleatorio.Real(-10f, 10f), 4f));
            }

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorInvasao: '{caso.Titulo}' (ladrao {(ladraoNaCena ? "na cena" : "fugiu")}, vitima {(vitimaNaCena ? "presente" : "ausente")}, {caso.Peds.Count} peds).");
            return caso;
        }
    }
}