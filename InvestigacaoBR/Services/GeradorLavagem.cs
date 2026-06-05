using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Lavagem de Dinheiro: operacao financeira clandestina descoberta.
    /// Contabilista / lavador de dinheiro + associados contando e registrando.
    /// Crime sem violencia direta — a investigacao e toda baseada em papeis,
    /// dinheiro e registros contabeis. Camera e crucial para flagrar a operacao.
    /// </summary>
    public class GeradorLavagem : GeradorBase
    {
        private static readonly string[] EsquemasLavagem =
        {
            "Dinheiro de trafico sendo lavado via empresa de taxi ficticia. Veiculo registrado como frota.",
            "Casino clandestino operando como fachada para lavagem de receita de extorsao.",
            "Construtora de fachada emitindo notas frias para justificar entrada de dinheiro sujo.",
            "Restaurante com movimento irreal de clientes — usado para limpar dinheiro de quadrilha."
        };

        private static readonly string[] InfosCamLavagem =
        {
            "Individuos contando grandes quantidades de notas ao redor de uma mesa.",
            "Troca de maletas entre dois homens de terno num estacionamento fechado.",
            "Individuo operando maquina de contar dinheiro num espaco sem janelas."
        };

        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisLavagem);
            string esquema = Aleatorio.Item(EsquemasLavagem);

            Caso caso = new Caso("Lavagem de Dinheiro", esquema, agoraInGame);
            caso.Titulo = $"Lavagem #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaContabilista = Aleatorio.NovoDnaId();

            // ----- Contabilista / lavador (culpado, centro) -----
            PedDoCaso lavador = CriarPed(PoolsCaso.ModelosVitima, // visual de executivo
                "Responsavel pela operacao financeira ilegal. Aparencia de profissional legítimo.",
                RolePed.Indefinido, 0.5f, 2f);
            lavador.EhCulpadoReal = true;
            lavador.PerfilDnaId = dnaContabilista;
            lavador.RegistroTelefonico = "Transferencias internacionais frequentes. Comunicacao com offshores nas Ilhas Cayman.";
            if (local != null)
            {
                lavador.LocalConhecidoX = local.X + Aleatorio.Real(-30f, 30f);
                lavador.LocalConhecidoY = local.Y + Aleatorio.Real(-30f, 30f);
                lavador.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(lavador);

            // ----- Associado 1 (seguranca / runner, 4-7 m) -----
            PedDoCaso associado1 = CriarPed(PoolsCaso.ModelosSuspeito,
                "Individuo transportando ou guardando o dinheiro a lavar.", RolePed.Indefinido, 4f, 7f);
            caso.AdicionarPed(associado1);

            // ----- Associado 2 (opcional, 5-9 m) -----
            if (Aleatorio.Chance(60))
            {
                PedDoCaso associado2 = CriarPed(PoolsCaso.ModelosSuspeito,
                    "Segundo operacional. Registra entradas no sistema paralelo.", RolePed.Indefinido, 5f, 9f);
                caso.AdicionarPed(associado2);
            }

            // ----- Testemunha (funcionario inocente que nao sabia, 10-18 m) -----
            if (Aleatorio.Chance(50))
            {
                PedDoCaso funcionario = CriarPed(PoolsCaso.ModelosCivil,
                    "Funcionario do estabelecimento. Nao sabia da operacao ilegal.", RolePed.Inocente, 10f, 18f);
                caso.AdicionarPed(funcionario);
            }

            // ----- Evidencias -----
            int macos = Aleatorio.Inteiro(2, 4);
            for (int i = 0; i < macos; i++)
            {
                Evidencia dinheiro = new Evidencia($"Maco de dinheiro #{i + 1}",
                    $"${Aleatorio.Inteiro(5, 80) * 1000} em notas mistas. Sem origem declaravel.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsDinheiro),
                    PerfilDnaId = i == 0 ? dnaContabilista : null,
                    ResultadoForense = i == 0
                        ? "Impressao digital do suspeito. Notas rastreadas a transacoes suspeitas."
                        : "Notas sem identificador. Possivelmente parte de operacao de trafico."
                };
                AplicarOffsetEvidencia(dinheiro, 0.3f, 2f);
                caso.AdicionarEvidencia(dinheiro);
            }

            Evidencia ledger = new Evidencia("Registro contabil paralelo",
                "Caderno com entradas e saidas nao declaradas. Contabilidade dupla.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsFerramenta),
                PerfilDnaId = dnaContabilista,
                ResultadoForense = $"DNA do suspeito. Registros de ${Aleatorio.Inteiro(50, 500) * 10000} em operacoes nao declaradas ao fisco."
            };
            AplicarOffsetEvidencia(ledger, 0.5f, 2f);
            caso.AdicionarEvidencia(ledger);

            Evidencia notas = new Evidencia("Notas fiscais falsas",
                "Documentacao fabricada para justificar entrada de dinheiro sujo como receita legítima.")
            {
                ResultadoForense = "Impressao digital do contabilista. Notas com CNPJ inexistente — fraude documental confirmada."
            };
            AplicarOffsetEvidencia(notas, 0.5f, 2.5f);
            caso.AdicionarEvidencia(notas);

            // ----- Camera -----
            caso.AdicionarCamera(CriarCamera(
                "Camera interna — area operacional",
                Aleatorio.Item(InfosCamLavagem),
                caso.CenaX, caso.CenaY, caso.CenaZ,
                Aleatorio.Real(-8f, 8f), Aleatorio.Real(-8f, 8f), 4f));

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorLavagem: '{caso.Titulo}' ({caso.Peds.Count} peds, {caso.Evidencias.Count} ev).");
            return caso;
        }
    }
}