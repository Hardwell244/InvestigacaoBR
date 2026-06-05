using System;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Incendio Criminoso: local queimado ou em chamas. Acelerantе encontrado na cena.
    /// Incendiario pode ter ficado para "ver o resultado" ou fugiu imediatamente.
    /// Motivo: seguro, vinganca, encobrimento de outro crime, ou contrato.
    /// </summary>
    public class GeradorIncendio : GeradorBase
    {
        private static readonly string[] MotivosIncendio =
        {
            "Incendio criminoso para receber seguro. Empresario endividado sob investigacao financeira.",
            "Incendio como represalia de gangue a comerciante que nao pagou 'protecao'.",
            "Local incendiado para encobrir evidencias de crime anterior ocorrido na propriedade.",
            "Incendio por encomenda. Concorrente desleal contratou o servico."
        };

        private static readonly string[] InfosCamIncendio =
        {
            "Individuo regando o perimetro do edificio com liquido antes de riscar um fosforo e sair rapidamente.",
            "Figura encapuzada saindo do local minutos antes do incendio deflagrar. Carregava galao.",
            "Individuo observando o incendio de longe por tempo incomum antes de sair a pe."
        };

        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisIncendio);
            string motivo = Aleatorio.Item(MotivosIncendio);

            Caso caso = new Caso("Incendio Criminoso", motivo, agoraInGame);
            caso.Titulo = $"Incendio #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaIncendiario = Aleatorio.NovoDnaId();
            bool incendiarioNaCena = Aleatorio.Chance(50);

            // ----- Incendiario (culpado) -----
            PedDoCaso incendiario = CriarPed(PoolsCaso.ModelosSuspeito,
                "Suspeito de ter ateado o fogo. Possivelmente observando o resultado da acao.",
                RolePed.Indefinido,
                incendiarioNaCena ? 10f : 0.1f,
                incendiarioNaCena ? 20f : 0.1f);

            if (!incendiarioNaCena) incendiario.NaoSpawnarNaCena = true;

            incendiario.EhCulpadoReal = true;
            incendiario.PerfilDnaId = dnaIncendiario;
            incendiario.RegistroTelefonico = "Chamadas para o contratante do servico horas antes e depois do incendio.";
            if (local != null)
            {
                incendiario.LocalConhecidoX = local.X + Aleatorio.Real(-50f, 50f);
                incendiario.LocalConhecidoY = local.Y + Aleatorio.Real(-50f, 50f);
                incendiario.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(incendiario);

            // ----- Testemunha (viu o inicio do fogo, 12-22 m) -----
            PedDoCaso testemunha = CriarPed(PoolsCaso.ModelosCivil,
                "Morador ou passante que viu o incendio comecar e possivelmente o autor.", RolePed.Testemunha, 12f, 22f);
            caso.AdicionarPed(testemunha);

            // ----- Civil curioso (opcional, 20-30 m) -----
            if (Aleatorio.Chance(50))
            {
                PedDoCaso curioso = CriarPed(PoolsCaso.ModelosCivil,
                    "Pessoa atraida pela fumaca. Chegou depois do incendio.", RolePed.Indefinido, 20f, 30f);
                caso.AdicionarPed(curioso);
            }

            // ----- Evidencias -----
            Evidencia galao = new Evidencia("Galao de acelerante",
                "Recipiente com residuo de combustivel — usado para iniciar o fogo.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsAcelerante),
                PerfilDnaId = dnaIncendiario,
                ResultadoForense = "Residuo de gasolina misturada com solvente. Impressao digital do suspeito no bocal."
            };
            AplicarOffsetEvidencia(galao, 1f, 4f);
            caso.AdicionarEvidencia(galao);

            Evidencia rastro = new Evidencia("Rastro de acelerante",
                "Marcas de liquido inflamavel no chao — padrao de espalhamento intencional.")
            {
                ResultadoForense = "Analise quimica: gasolina + trementina. Padrao de aplicacao deliberada, nao acidental."
            };
            AplicarOffsetEvidencia(rastro, 0.5f, 2f);
            caso.AdicionarEvidencia(rastro);

            Evidencia itemCena = new Evidencia("Item abandonado",
                "Objeto deixado pelo incendiario antes ou durante a acao.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                PerfilDnaId = dnaIncendiario,
                ResultadoForense = "DNA compativel com o suspeito principal. Item comprado em loja proxima — rastreavel."
            };
            AplicarOffsetEvidencia(itemCena, 2f, 6f);
            caso.AdicionarEvidencia(itemCena);

            // ----- Camera -----
            if (Aleatorio.Chance(70))
            {
                caso.AdicionarCamera(CriarCamera(
                    "Camera da area — pre-incendio",
                    Aleatorio.Item(InfosCamIncendio),
                    caso.CenaX, caso.CenaY, caso.CenaZ,
                    Aleatorio.Real(-12f, 12f), Aleatorio.Real(-12f, 12f), 5f));
            }

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorIncendio: '{caso.Titulo}' (incend. {(incendiarioNaCena ? "na cena" : "fugiu")}, {caso.Peds.Count} peds, {caso.Evidencias.Count} ev).");
            return caso;
        }
    }
}