using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Tráfico de Armas: reuniao de negociacao de armas interceptada.
    /// Vendedor + compradores de gangue + vigia. Evidencias: caixas de armas,
    /// mala de dinheiro, documentacao de serial numbers. Segunda locacao:
    /// deposito com mais armamento (via mandado).
    /// </summary>
    public class GeradorTraficoArmas : GeradorBase
    {
        private static readonly string[] InfosCamArmas =
        {
            "Reuniao de individuos ao redor de maletas abertas. Troca de objetos e dinheiro.",
            "Veiculo fechado descarregando caixas no local. Individuos armados fazendo seguranca.",
            "Troca de maletas entre dois individuos. Terceiro faz observacao do perimetro."
        };

        private static readonly string[] RegistrosTelefonicos =
        {
            "Comunicacao criptografada com fornecedor. Mencao a 'carregamento' e 'entrega urgente'.",
            "Contatos com numeros internacionais. Mensagens deletadas — recuperadas parcialmente.",
            "Chamadas para lideres de organizacao criminosa. Horarios coincidem com o crime."
        };

        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisTraficoArmas);

            Caso caso = new Caso("Trafico de Armas",
                "Negociacao de armamento ilegal interceptada. Multiplos suspeitos envolvidos.",
                agoraInGame);
            caso.Titulo = $"Trafico Armas #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaVendedor = Aleatorio.NovoDnaId();

            // ----- Vendedor (culpado principal, centro da negociacao) -----
            PedDoCaso vendedor = CriarPed(PoolsCaso.ModelosSuspeito,
                "Individuo conduzindo a venda do armamento.", RolePed.Indefinido, 0.5f, 2f);
            vendedor.EhCulpadoReal = true;
            vendedor.PerfilDnaId = dnaVendedor;
            vendedor.RegistroTelefonico = Aleatorio.Item(RegistrosTelefonicos);
            if (local != null)
            {
                // Segunda locacao: deposito com mais armas
                vendedor.LocalConhecidoX = local.X + Aleatorio.Real(-40f, 40f);
                vendedor.LocalConhecidoY = local.Y + Aleatorio.Real(-40f, 40f);
                vendedor.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(vendedor);

            // ----- Compradores (gangue, 3-6 m) -----
            int qtdCompradores = Aleatorio.Inteiro(1, 2);
            for (int i = 0; i < qtdCompradores; i++)
            {
                PedDoCaso comprador = CriarPed(PoolsCaso.ModelosGangue,
                    "Membro de gangue adquirindo armamento.", RolePed.Indefinido, 3f, 6f);
                caso.AdicionarPed(comprador);
            }

            // ----- Vigia (lookout, 10-16 m) -----
            PedDoCaso vigia = CriarPed(PoolsCaso.ModelosGangue,
                "Individuo fazendo seguranca do perimetro da reuniao.", RolePed.Indefinido, 10f, 16f);
            caso.AdicionarPed(vigia);

            // ----- Evidencias: caixas de armas -----
            int qtdCaixas = Aleatorio.Inteiro(1, 3);
            for (int i = 0; i < qtdCaixas; i++)
            {
                Evidencia caixa = new Evidencia($"Maleta de armamento #{i + 1}",
                    "Caixa contendo armamento ilegal sem numeracao de serie.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsCaixaArma),
                    PerfilDnaId = i == 0 ? dnaVendedor : null,
                    ResultadoForense = i == 0
                        ? $"Impressao digital do suspeito identificada. {Aleatorio.Inteiro(3, 8)} armas sem registro."
                        : $"{Aleatorio.Inteiro(2, 6)} armas com numeracao adulterada."
                };
                AplicarOffsetEvidencia(caixa, 0.5f, 3f);
                caso.AdicionarEvidencia(caixa);
            }

            // ----- Dinheiro do pagamento -----
            Evidencia dinheiro = new Evidencia("Pagamento em especie",
                $"Mala com ${Aleatorio.Inteiro(5, 30) * 1000} em notas nao rastreadas.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsDinheiro),
                ResultadoForense = "Notas falsas misturadas. Impressoes digitais multiplas."
            };
            AplicarOffsetEvidencia(dinheiro, 0.5f, 2.5f);
            caso.AdicionarEvidencia(dinheiro);

            // ----- Documentacao: lista de serial numbers -----
            Evidencia docs = new Evidencia("Documentacao suspeita",
                "Papel com codigos e numeros — possivelmente serial numbers adulterados.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsFerramenta),
                PerfilDnaId = dnaVendedor,
                ResultadoForense = "Lista de 14 armas com serials raspados. DNA do suspeito no papel."
            };
            AplicarOffsetEvidencia(docs, 0.5f, 2f);
            caso.AdicionarEvidencia(docs);

            // ----- Camera -----
            caso.AdicionarCamera(CriarCamera(
                "Camera de seguranca — area industrial",
                Aleatorio.Item(InfosCamArmas),
                caso.CenaX, caso.CenaY, caso.CenaZ,
                Aleatorio.Real(-12f, 12f), Aleatorio.Real(-12f, 12f), 5f));

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorTraficoArmas: '{caso.Titulo}' ({caso.Peds.Count} peds, {caso.Evidencias.Count} ev).");
            return caso;
        }
    }
}