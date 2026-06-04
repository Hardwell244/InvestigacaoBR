using System;
using System.Collections.Generic;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Base dos geradores de casos. Reune os helpers compartilhados de sorteio (nome/genero,
    /// ped autorado, offset em anel, camera) para os geradores concretos — homicidio, trafico,
    /// roubo de carga — nao duplicarem a mesma logica. O seeder pode trata-los polimorficamente.
    /// </summary>
    public abstract class GeradorBase
    {
        /// <summary>Gera um caso completo, ja com o tempo IN-GAME de abertura.</summary>
        public abstract Caso Gerar(DateTime agoraInGame);

        /// <summary>Sorteia um nome completo (PT) e devolve o genero ("Masculino"/"Feminino").</summary>
        protected static string GerarNome(out string genero)
        {
            bool masculino = Aleatorio.Chance(50);
            genero = masculino ? "Masculino" : "Feminino";

            string primeiro = masculino
                ? Aleatorio.Item(PoolsCaso.NomesMasculinos)
                : Aleatorio.Item(PoolsCaso.NomesFemininos);
            string sobrenome = Aleatorio.Item(PoolsCaso.Sobrenomes);

            return $"{primeiro} {sobrenome}";
        }

        /// <summary>
        /// Cria um PedDoCaso autorado: modelo sorteado do pool, nome/genero sorteados, heading
        /// aleatorio e offset em anel entre raioMin e raioMax a partir da origem da cena.
        /// </summary>
        protected static PedDoCaso CriarPed(IList<string> poolModelos, string descricao, RolePed role,
                                            float raioMin, float raioMax)
        {
            string genero;
            string nome = GerarNome(out genero);

            PedDoCaso ped = new PedDoCaso(nome, descricao, role)
            {
                ModeloPed = Aleatorio.Item(poolModelos),
                Genero = genero,
                Heading = Aleatorio.Real(0f, 360f)
            };

            AplicarOffsetAnel(out float x, out float y, raioMin, raioMax);
            ped.OffsetX = x;
            ped.OffsetY = y;
            ped.OffsetZ = 0f;
            return ped;
        }

        /// <summary>Aplica um offset em anel (raioMin..raioMax) a uma evidencia. Z = 0.</summary>
        protected static void AplicarOffsetEvidencia(Evidencia ev, float raioMin, float raioMax)
        {
            AplicarOffsetAnel(out float x, out float y, raioMin, raioMax);
            ev.OffsetX = x;
            ev.OffsetY = y;
            ev.OffsetZ = 0f;
        }

        /// <summary>Calcula um offset (x,y) num anel entre raioMin e raioMax ao redor da origem.</summary>
        protected static void AplicarOffsetAnel(out float x, out float y, float raioMin, float raioMax)
        {
            double ang = Aleatorio.Real(0f, (float)(Math.PI * 2.0));
            float raio = Aleatorio.Real(raioMin, raioMax);
            x = (float)(Math.Cos(ang) * raio);
            y = (float)(Math.Sin(ang) * raio);
        }

        /// <summary>
        /// Cria uma camera posicionada em (origem + offset) olhando para a origem da cena.
        /// </summary>
        protected static GravacaoCamera CriarCamera(string local, string info,
            float origemX, float origemY, float origemZ,
            float offX, float offY, float offZ)
        {
            return new GravacaoCamera(local)
            {
                PosX = origemX + offX,
                PosY = origemY + offY,
                PosZ = origemZ + offZ,
                AlvoX = origemX,
                AlvoY = origemY,
                AlvoZ = origemZ,
                Fov = 50f,
                InfoRevelada = info
            };
        }
    }
}