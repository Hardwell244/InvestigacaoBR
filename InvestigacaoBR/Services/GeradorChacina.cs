using System;
using System.Collections.Generic;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Chacina de Gangues: multiplas vitimas, atiradores de gangue rivais,
    /// cascas de balas espalhadas, testemunhas assustadas. Cena de alta violencia.
    /// Cruzar DNA das capsulas com suspeitos e encontrar o atirador principal.
    /// </summary>
    public class GeradorChacina : GeradorBase
    {
        private static readonly string[] InfosCamChacina =
        {
            "Veiculo escuro em alta velocidade disparando em direcao as vitimas antes de fugir sentido norte.",
            "Tiroteio iniciado por ocupantes de veiculo em movimento. Fugiu sentido a rodovia.",
            "Individuo a pe disparando multiplos tiros antes de correr pela viela lateral.",
        };

        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisChacina);

            Caso caso = new Caso("Chacina", "Multiplas vitimas por disparo de arma de fogo. Indicios de conflito entre gangues.", agoraInGame);
            caso.Titulo = $"Chacina #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";
            caso.DescricaoGeral = "Cena de alta violencia. Multiplos projéteis. Possivel acerto de contas entre facoes rivais.";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            string dnaAtirador = Aleatorio.NovoDnaId();

            // ----- Vitimas (2-3 mortas, distribucao pelo centro) -----
            int qtdVitimas = Aleatorio.Inteiro(2, 3);
            for (int i = 0; i < qtdVitimas; i++)
            {
                bool eGangue = Aleatorio.Chance(60); // 60% vitimas sao membros de gangue rival
                PedDoCaso vitima = CriarPed(
                    eGangue ? PoolsCaso.ModelosGangue : PoolsCaso.ModelosVitima,
                    eGangue ? "Membro de gangue rival — vitima." : "Civil pego no tiroteio.",
                    RolePed.Indefinido, 0f, i == 0 ? 0.3f : 3f + i);
                vitima.SpawnarMorto = true;
                vitima.Heading = Aleatorio.Real(0f, 360f);
                caso.AdicionarPed(vitima);

                // Poca de sangue por vitima
                Evidencia sangueVitima = new Evidencia($"Mancha de sangue (vitima {i + 1})",
                    "Poca de sangue extensa proxima ao corpo.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsSangue),
                    ResultadoForense = "Sangue humano. Perfil unico — vitima separada."
                };
                AplicarOffsetEvidencia(sangueVitima, 0.1f, 0.4f + i * 0.3f);
                caso.AdicionarEvidencia(sangueVitima);
            }

            // ----- Culpado principal (atirador, 10-20 m da cena, ainda por perto) -----
            PedDoCaso atirador = CriarPed(PoolsCaso.ModelosGangue,
                "Individuo de gangue avistado na area imediatamente apos os disparos.",
                RolePed.Indefinido, 10f, 20f);
            atirador.EhCulpadoReal = true;
            atirador.PerfilDnaId = dnaAtirador;
            atirador.RegistroTelefonico = "Chamadas para lideres de gangue nos minutos anteriores ao incidente.";
            if (local != null)
            {
                atirador.LocalConhecidoX = local.X + Aleatorio.Real(-30f, 30f);
                atirador.LocalConhecidoY = local.Y + Aleatorio.Real(-30f, 30f);
                atirador.LocalConhecidoZ = local.Z;
            }
            caso.AdicionarPed(atirador);

            // ----- Comparsa (opcional — segundo atirador, 12-18 m) -----
            if (Aleatorio.Chance(60))
            {
                PedDoCaso comparsa = CriarPed(PoolsCaso.ModelosGangue,
                    "Associado do atirador — presente na cena.", RolePed.Indefinido, 12f, 18f);
                caso.AdicionarPed(comparsa);
            }

            // ----- Testemunha assustada (longe, 18-28 m) -----
            PedDoCaso testemunha = CriarPed(PoolsCaso.ModelosCivil,
                "Civil que presenciou o ataque de longe. Visivelmente assustado.",
                RolePed.Testemunha, 18f, 28f);
            caso.AdicionarPed(testemunha);

            // ----- Evidencias: capsulas em massa -----
            int qtdCapsulas = Aleatorio.Inteiro(3, 6);
            for (int i = 0; i < qtdCapsulas; i++)
            {
                Evidencia capsula = new Evidencia($"Capsula de projetil #{i + 1}",
                    "Capsula deflagrada no local. Calibre compativel com arma semiautomatica.")
                {
                    ModeloProp = Aleatorio.Item(PoolsCaso.PropsCapsula),
                    PerfilDnaId = i == 0 ? dnaAtirador : null, // primeira capsula tem DNA
                    ResultadoForense = i == 0
                        ? "Impressao digital parcial recuperada. Perfil de DNA isolado na culatra."
                        : "Capsula limpa. Calibre 9mm."
                };
                AplicarOffsetEvidencia(capsula, 0.5f, 6f);
                caso.AdicionarEvidencia(capsula);
            }

            // ----- Item do atirador (deixado na fuga) -----
            Evidencia itemAtirador = new Evidencia("Item perdido na fuga",
                "Objeto deixado pelo atirador durante a retirada precipitada.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                PerfilDnaId = dnaAtirador,
                ResultadoForense = "DNA na superficie compativel com perfil do suspeito principal."
            };
            AplicarOffsetEvidencia(itemAtirador, 8f, 16f);
            caso.AdicionarEvidencia(itemAtirador);

            // ----- Cameras -----
            int qtdCam = Aleatorio.Inteiro(1, 2);
            for (int i = 0; i < qtdCam; i++)
            {
                caso.AdicionarCamera(CriarCamera(
                    $"Camera de seguranca #{i + 1}",
                    Aleatorio.Item(InfosCamChacina),
                    caso.CenaX, caso.CenaY, caso.CenaZ,
                    Aleatorio.Real(-10f, 10f), Aleatorio.Real(-10f, 10f), 4f));
            }

            DistribuirAngulos(caso.Peds);

            Logger.Info($"GeradorChacina: '{caso.Titulo}' ({qtdVitimas} vitimas, {caso.Peds.Count} peds, {caso.Evidencias.Count} ev).");
            return caso;
        }
    }
}