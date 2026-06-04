using System;
using System.Collections.Generic;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    public class GeradorRouboCarga : GeradorBase
    {
        public override Caso Gerar(DateTime agoraInGame)
        {
            PontoCena local = Aleatorio.Item(PoolsCaso.LocaisRouboCarga);

            Caso caso = new Caso("Roubo de Carga", "Investigacao de roubo de carga atribuido a uma quadrilha.", agoraInGame);
            caso.Titulo = $"Roubo de Carga #{caso.Id.ToString("N").Substring(0, 4).ToUpperInvariant()}";

            if (local != null)
            {
                caso.CenaX = local.X; caso.CenaY = local.Y;
                caso.CenaZ = local.Z; caso.CenaHeading = local.Heading;
            }

            // ----- Quadrilha (2-3 membros, 2-8 m — espalhados pela cena do roubo) -----
            int qtdMembros = Aleatorio.Inteiro(2, 3);
            List<string> dnas = new List<string>();

            for (int i = 0; i < qtdMembros; i++)
            {
                string dna = Aleatorio.NovoDnaId();
                dnas.Add(dna);

                PedDoCaso membro = CriarPed(PoolsCaso.ModelosSuspeito,
                    $"Suspeito de integrar a quadrilha (membro {i + 1}).", RolePed.Indefinido, 2f, 8f);
                membro.EhCulpadoReal = true;
                membro.PerfilDnaId = dna;
                membro.RegistroTelefonico = "Mensagens coordenando horario e ponto de interceptacao da carga com os demais.";
                if (local != null)
                {
                    membro.LocalConhecidoX = local.X + Aleatorio.Real(-25f, 25f);
                    membro.LocalConhecidoY = local.Y + Aleatorio.Real(-25f, 25f);
                    membro.LocalConhecidoZ = local.Z;
                }
                caso.AdicionarPed(membro);
            }

            // ----- Testemunha (10-16 m — trabalhador que viu de longe, longe da quadrilha) -----
            if (Aleatorio.Chance(60))
            {
                PedDoCaso testemunha = CriarPed(PoolsCaso.ModelosCivil,
                    "Trabalhador local que presenciou parte da acao de longe.", RolePed.Testemunha, 10f, 16f);
                caso.AdicionarPed(testemunha);
            }

            // ----- Evidencias: DNA de membros distintos (cruzamento) -----
            Evidencia carga = new Evidencia("Carga abandonada", "Parte da carga deixada para tras na fuga.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsCarga),
                PerfilDnaId = dnas[0],
                ResultadoForense = "Material genetico recuperado das embalagens manuseadas."
            };
            AplicarOffsetEvidencia(carga, 1f, 4f);
            caso.AdicionarEvidencia(carga);

            Evidencia ferramenta = new Evidencia("Ferramenta de arrombamento", "Instrumento usado para forcar o container.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsFerramenta),
                PerfilDnaId = dnas.Count > 1 ? dnas[1] : dnas[0],
                ResultadoForense = "DNA na empunhadura. Marcas compativeis com o arrombamento do lacre."
            };
            AplicarOffsetEvidencia(ferramenta, 1f, 4f);
            caso.AdicionarEvidencia(ferramenta);

            Evidencia celular = new Evidencia("Celular derrubado", "Aparelho perdido por um dos envolvidos na fuga.")
            {
                ModeloProp = Aleatorio.Item(PoolsCaso.PropsItemPessoal),
                PerfilDnaId = dnas[dnas.Count - 1],
                ResultadoForense = "Mensagens coordenando o roubo. DNA confirmado no aparelho."
            };
            AplicarOffsetEvidencia(celular, 1f, 4f);
            caso.AdicionarEvidencia(celular);

            // ----- Cameras -----
            string[] infos =
            {
                "Caminhao parado e varios individuos transferindo caixas para outro veiculo.",
                "Grupo forcando a abertura de um container nas docas.",
                "Veiculo de fuga carregado deixando o terminal em alta velocidade."
            };
            int qtdCam = Aleatorio.Inteiro(1, 2);
            for (int i = 0; i < qtdCam; i++)
            {
                caso.AdicionarCamera(CriarCamera(
                    $"Camera do terminal #{i + 1}", Aleatorio.Item(infos),
                    caso.CenaX, caso.CenaY, caso.CenaZ,
                    Aleatorio.Real(-15f, 15f), Aleatorio.Real(-15f, 15f), 5f));
            }

            DistribuirAngulos(caso.Peds); // fix #5

            Logger.Info($"GeradorRouboCarga: '{caso.Titulo}' ({qtdMembros} membros, {caso.Peds.Count} peds, {caso.Evidencias.Count} ev, {caso.Cameras.Count} cam).");
            return caso;
        }
    }
}