using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Laboratorio Clandestino: operacao de producao de metanfetamina descoberta.
    /// Cozinheiro (culpado) + guardas. Cena rica em evidencias quimicas.
    /// Sem vitimas mortas — o crime e a operacao em si, nao violencia.
    /// Segunda locacao via mandado: ponto de distribuicao da droga produzida.
    /// </summary>
    public class GeradorLaboratorio : GeradorBase
    {
        private static readonly string[] TiposDroga =
        {
            "Metanfetamina", "Cocaina refinada", "MDMA em po"
        };

        private static readonly string[] RegistrosCozinheiro =
        {
            "Pedidos recorrentes de precursores quimicos por numero falso. Fornecedores rastreados.",
            "Comunicacao com distribuidor — combinando lotes e precos. Mencao a 'laboratorio 3'.",
            "Contatos com advogado suspeito. Transferencias bancarias de valores suspeitos."
        };

        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisLaboratorio);
            string tipoDroga = Aleatorio.Item(TiposDroga);

            Caso caso = new Caso("Laboratorio Clandestino",
                $"Producao ilegal de {tipoDroga} identificada. Operacao em andamento no momento da chegada.",
                agoraInGame);
            caso.Titulo = $"Lab Clandestino #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaCozinheiro = Aleatorio.NovoDnaId();

            // ----- Cozinheiro (culpado principal, no laboratorio) -----
            PedDoCaso cozinheiro = CriarPed(PoolsCaso.ModelosSuspeito,
                $"Individuo responsavel pela producao de {tipoDroga}. Especializado em quimica.",
                RolePed.Indefinido, 0.5f, 2f);
            cozinheiro.EhCulpadoReal = true;
            cozinheiro.PerfilDnaId = dnaCozinheiro;
            cozinheiro.RegistroTelefonico = Aleatorio.Item(RegistrosCozinheiro);
            if (local != null)
            {
                // Segunda locacao: ponto de distribuicao
                cozinheiro.LocalConhecidoX = local.X + Aleatorio.Real(-35f, 35f);
                cozinheiro.LocalConhecidoY = local.Y + Aleatorio.Real(-35f, 35f);
                cozinheiro.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(cozinheiro);

            // ----- Guarda 1 (seguranca do lab, 8-14 m) -----
            PedDoCaso guarda1 = CriarPed(PoolsCaso.ModelosSuspeito,
                "Individuo fazendo seguranca do laboratorio.", RolePed.Indefinido, 8f, 14f);
            caso.AdicionarPed(guarda1);

            // ----- Guarda 2 (opcional, 10-18 m) -----
            if (Aleatorio.Chance(60))
            {
                PedDoCaso guarda2 = CriarPed(PoolsCaso.ModelosGangue,
                    "Segundo guarda do perimetro externo.", RolePed.Indefinido, 10f, 18f);
                caso.AdicionarPed(guarda2);
            }

            // ----- Usuário/vitima de overdose (opcional — moral do caso) -----
            if (Aleatorio.Chance(30))
            {
                PedDoCaso vitima = CriarPed(PoolsCaso.ModelosVitima,
                    "Individuo encontrado inconsciente proximo ao laboratorio. Overdose suspeita.",
                    RolePed.Inocente, 4f, 7f);
                vitima.SpawnarMorto = true;
                caso.AdicionarPed(vitima);
            }

            // ----- Evidencias: producao -----
            int qtdBags = Aleatorio.Inteiro(2, 4);
            for (int i = 0; i < qtdBags; i++)
            {
                Evidencia lote = new Evidencia($"Lote de droga #{i + 1}",
                    $"Porcoes de {tipoDroga} embaladas para distribuicao. Alta pureza.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsLaboratorio),
                    PerfilDnaId = i == 0 ? dnaCozinheiro : null,
                    ResultadoForense = i == 0
                        ? $"{tipoDroga} — pureza de {Aleatorio.Inteiro(75, 95)}%. DNA do cozinheiro na embalagem."
                        : $"{tipoDroga} — lote pronto para distribuicao. Sem identificador."
                };
                AplicarOffsetEvidencia(lote, 0.3f, 2f);
                caso.AdicionarEvidencia(lote);
            }

            // ----- Equipamento de laboratorio -----
            Evidencia equipamento = new Evidencia("Equipamento quimico",
                "Aparatos de destilacao e processamento adaptados para producao ilegal.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsLaboratorio),
                ResultadoForense = $"Residuos de precursores de {tipoDroga}. Operacao ha pelo menos 6 meses."
            };
            AplicarOffsetEvidencia(equipamento, 0.5f, 2.5f);
            caso.AdicionarEvidencia(equipamento);

            // ----- Contabilidade / caderno de registros -----
            Evidencia caderno = new Evidencia("Caderno de contabilidade",
                "Registro manuscrito de lotes, valores e contatos de distribuicao.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsFerramenta),
                PerfilDnaId = dnaCozinheiro,
                ResultadoForense = $"DNA identificado. Registros de {Aleatorio.Inteiro(20, 80)} lotes ao longo de meses. Nomes de distribuidores anotados."
            };
            AplicarOffsetEvidencia(caderno, 0.5f, 2f);
            caso.AdicionarEvidencia(caderno);

            // ----- Dinheiro (lucro da operacao) -----
            Evidencia dinheiro = new Evidencia("Dinheiro do trafico",
                $"${Aleatorio.Inteiro(10, 80) * 1000} em notas misturadas. Lucro estimado da operacao.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsDinheiro),
                ResultadoForense = "Notas sem rastreamento. Impressoes digitais multiplas."
            };
            AplicarOffsetEvidencia(dinheiro, 1f, 3f);
            caso.AdicionarEvidencia(dinheiro);

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorLaboratorio: '{caso.Titulo}' ({tipoDroga}, {caso.Peds.Count} peds, {caso.Evidencias.Count} ev).");
            return caso;
        }
    }
}