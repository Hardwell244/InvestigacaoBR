using System;
using System.Collections.Generic;

namespace InvestigacaoBR.Core
{
    /// <summary>
    /// Helper central de sorteio, usado pelos geradores de casos. Mantem um unico Random
    /// compartilhado. Sem dependencias alem de System.
    /// </summary>
    public static class Aleatorio
    {
        private static readonly Random _rng = new Random();

        /// <summary>Inteiro entre min (inclusive) e max (inclusive).</summary>
        public static int Inteiro(int min, int max)
        {
            if (min > max)
            {
                int t = min; min = max; max = t;
            }
            return _rng.Next(min, max + 1);
        }

        /// <summary>Numero real entre min e max.</summary>
        public static float Real(float min, float max)
        {
            if (min > max)
            {
                float t = min; min = max; max = t;
            }
            return (float)(_rng.NextDouble() * (max - min) + min);
        }

        /// <summary>True com a probabilidade informada (0 a 100%).</summary>
        public static bool Chance(double percent)
        {
            return _rng.NextDouble() * 100.0 < percent;
        }

        /// <summary>Elemento aleatorio da lista; default(T) se a lista for nula/vazia.</summary>
        public static T Item<T>(IList<T> lista)
        {
            if (lista == null || lista.Count == 0)
            {
                return default(T);
            }
            return lista[_rng.Next(lista.Count)];
        }

        /// <summary>N itens DISTINTOS aleatorios da lista (limitado ao tamanho dela).</summary>
        public static List<T> Itens<T>(IList<T> lista, int quantidade)
        {
            List<T> resultado = new List<T>();
            if (lista == null || lista.Count == 0 || quantidade <= 0)
            {
                return resultado;
            }

            List<T> copia = new List<T>(lista);
            Embaralhar(copia);

            int qtd = Math.Min(quantidade, copia.Count);
            for (int i = 0; i < qtd; i++)
            {
                resultado.Add(copia[i]);
            }
            return resultado;
        }

        /// <summary>Embaralha a lista no lugar (Fisher-Yates).</summary>
        public static void Embaralhar<T>(IList<T> lista)
        {
            if (lista == null)
            {
                return;
            }

            for (int i = lista.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                T tmp = lista[i];
                lista[i] = lista[j];
                lista[j] = tmp;
            }
        }

        /// <summary>Gera um id de perfil de DNA curto e unico-ish, ex.: "DNA-7F3A".</summary>
        public static string NovoDnaId()
        {
            return "DNA-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
        }
    }
}