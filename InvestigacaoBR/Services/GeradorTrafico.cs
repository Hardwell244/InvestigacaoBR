using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Gera um caso de trafico de drogas aleatorio: ponto sorteado, traficante vivo (culpado,
    /// com DNA na droga/celular), comprador opcional, transeuntes, evidencias (drogas, dinheiro,
    /// balanca, celular), telefone revelando a rede e cameras da transacao. Verdade congela aqui.
    /// </summary>
    public class GeradorTrafico : GeradorBase
    {
        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisTrafico);

            Caso caso = new Caso("Trafico", "Investigacao de trafico de drogas. Ponto de venda monitorado.", agoraInGame);
            caso.Titulo = $"Trafico #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X;
                caso.CenaY = local.Y;
                caso.CenaZ = local.Z;
                caso.CenaHeading = local.Heading;
            }

            string dnaTraficante = Aleatorio.NovoDnaId();

            // ----- Traficante (culpado, vivo, no ponto) -----
            PedDoCaso traficante = CriarPed(PoolsCaso.ModelosSuspeito, "Individuo realizando a venda.", RolePed.Indefinido, 0.5f, 2f);
            traficante.Heading = caso.CenaHeading;
            traficante.EhCulpadoReal = true;
            traficante.PerfilDnaId = dnaTraficante;
            traficante.RegistroTelefonico = "Mensagens e chamadas frequentes para fornecedor e lista de compradores recorrentes.";
            if (local != null)
            {
                traficante.LocalConhecidoX = local.X + Aleatorio.Real(-20f, 20f);
                traficante.LocalConhecidoY = local.Y + Aleatorio.Real(-20f, 20f);
                traficante.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(traficante);

            // ----- Comprador (opcional, pessoa de interesse) -----
            if (Aleatorio.Chance(70))
            {
                PedDoCaso comprador = CriarPed(PoolsCaso.ModelosCivil, "Pessoa em contato com o alvo (possivel comprador).", RolePed.Indefinido, 2f, 4f);
                caso.AdicionarPed(comprador);
            }

            // ----- Transeuntes (1 a 2, modelos distintos) -----
            int qtdCivis = Aleatorio.Inteiro(1, 2);
            foreach (string modelo in Aleatorio.Itens(PoolsCaso.ModelosCivil, qtdCivis))
            {
                PedDoCaso civil = CriarPed(PoolsCaso.ModelosCivil, "Transeunte nas proximidades.", RolePed.Indefinido, 6f, 12f);
                civil.ModeloPed = modelo;
                caso.AdicionarPed(civil);
            }

            // ----- Drogas (DNA do traficante na embalagem) -----
            Evidencia drogas = new Evidencia("Entorpecentes", "Porcoes de droga embaladas para venda.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsDroga),
                PerfilDnaId = dnaTraficante,
                ResultadoForense = "Substancia confirmada como entorpecente. DNA recuperado da embalagem."
            };
            AplicarOffsetEvidencia(drogas, 0.5f, 2.5f);
            caso.AdicionarEvidencia(drogas);

            // ----- Dinheiro -----
            Evidencia dinheiro = new Evidencia("Dinheiro em especie", "Maco de notas oriundo da venda.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsDinheiro),
                ResultadoForense = $"Total aproximado de R$ {Aleatorio.Inteiro(2, 30) * 100} em notas."
            };
            AplicarOffsetEvidencia(dinheiro, 0.5f, 2.5f);
            caso.AdicionarEvidencia(dinheiro);

            // ----- Balanca (registrada, sem prop) -----
            Evidencia balanca = new Evidencia("Balanca de precisao", "Balanca usada para pesar as porcoes.")
            {
                ResultadoForense = "Residuos de entorpecente na superficie."
            };
            AplicarOffsetEvidencia(balanca, 0.5f, 2f);
            caso.AdicionarEvidencia(balanca);

            // ----- Celular (DNA + rede telefonica) -----
            Evidencia celular = new Evidencia("Celular do alvo", "Aparelho com mensagens da rede de venda.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                PerfilDnaId = dnaTraficante,
                ResultadoForense = "Mensagens com fornecedor e clientes. DNA confirmado no aparelho."
            };
            AplicarOffsetEvidencia(celular, 0.5f, 2f);
            caso.AdicionarEvidencia(celular);

            // ----- Cameras (1 a 2) -----
            string[] infos =
            {
                "Troca rapida de objeto por dinheiro entre o alvo e um pedestre.",
                "Alvo guardando pacote em mochila apos abordagem de um veiculo.",
                "Movimentacao constante de pessoas se aproximando do mesmo individuo."
            };
            int qtdCam = Aleatorio.Inteiro(1, 2);
            for (int i = 0; i < qtdCam; i++)
            {
                GravacaoCamera cam = CriarCamera(
                    $"Camera do ponto #{i + 1}",
                    Aleatorio.Item(infos),
                    caso.CenaX, caso.CenaY, caso.CenaZ,
                    Aleatorio.Real(-12f, 12f), Aleatorio.Real(-12f, 12f), 4f);
                caso.AdicionarCamera(cam);
            }

            Logger.Info($"GeradorTrafico: '{caso.Titulo}' gerado ({caso.Peds.Count} peds, {caso.Evidencias.Count} evidencias, {caso.Cameras.Count} cameras).");
            return caso;
        }
    }
}