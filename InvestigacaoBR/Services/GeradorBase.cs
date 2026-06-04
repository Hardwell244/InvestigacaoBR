using System;
using System.Collections.Generic;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    public abstract class GeradorBase
    {
        public abstract Caso Gerar(DateTime agoraInGame);

        /// <summary>Sorteia nome americano e devolve o genero.</summary>
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

        protected static void AplicarOffsetEvidencia(Evidencia ev, float raioMin, float raioMax)
        {
            AplicarOffsetAnel(out float x, out float y, raioMin, raioMax);
            ev.OffsetX = x;
            ev.OffsetY = y;
            ev.OffsetZ = 0f;
        }

        protected static void AplicarOffsetAnel(out float x, out float y, float raioMin, float raioMax)
        {
            double ang = Aleatorio.Real(0f, (float)(Math.PI * 2.0));
            float raio = Aleatorio.Real(raioMin, raioMax);
            x = (float)(Math.Cos(ang) * raio);
            y = (float)(Math.Sin(ang) * raio);
        }

        /// <summary>
        /// fix #5: redistribui os angulos dos peds ao redor da cena para evitar aglomeracao.
        /// Peds com raio aproximadamente zero (ex.: vitima no centro) sao ignorados.
        /// Mantem o raio de cada ped, apenas espalha os angulos uniformemente com pequeno jitter.
        /// Chame no final de cada Gerar(), depois de adicionar todos os peds ao caso.
        /// </summary>
        protected static void DistribuirAngulos(IList<PedDoCaso> peds)
        {
            if (peds == null || peds.Count == 0) return;

            // Coleta apenas os peds fora do centro
            var distribuir = new List<PedDoCaso>();
            foreach (PedDoCaso p in peds)
            {
                float raio = (float)Math.Sqrt(p.OffsetX * p.OffsetX + p.OffsetY * p.OffsetY);
                if (raio > 0.1f) distribuir.Add(p);
            }

            int n = distribuir.Count;
            if (n == 0) return;

            for (int i = 0; i < n; i++)
            {
                PedDoCaso p = distribuir[i];
                float raio = (float)Math.Sqrt(p.OffsetX * p.OffsetX + p.OffsetY * p.OffsetY);
                // Angulo base uniforme + jitter leve para nao parecer robotico
                double ang = (Math.PI * 2.0 / n) * i + Aleatorio.Real(-0.25f, 0.25f);
                p.OffsetX = (float)(Math.Cos(ang) * raio);
                p.OffsetY = (float)(Math.Sin(ang) * raio);
            }
        }

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