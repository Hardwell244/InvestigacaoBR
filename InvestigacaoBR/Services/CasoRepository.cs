using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Persiste e carrega a lista mestre de casos em disco, em XML. E stateless quanto
    /// aos dados (nao guarda a lista em memoria) — apenas le e grava. Toda operacao de I/O
    /// e logada e protegida por try/catch para NUNCA derrubar o plugin.
    ///
    /// Arquivo padrao (relativo a raiz do GTA V, que e o diretorio de trabalho do RPH):
    ///   Plugins\LSPDFR\InvestigacaoBR\casos.xml
    /// </summary>
    public class CasoRepository
    {
        // O XmlSerializer e custoso de criar e gera assembly temporario; cacheamos um unico.
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(List<Caso>));

        private readonly string _caminhoArquivo;
        private readonly string _caminhoPasta;

        /// <summary>Caminho padrao do arquivo de casos, relativo a raiz do GTA V.</summary>
        public static string CaminhoPadrao()
        {
            return Path.Combine("Plugins", "LSPDFR", "InvestigacaoBR", "casos.xml");
        }

        /// <summary>Usa o caminho padrao do arquivo de casos.</summary>
        public CasoRepository() : this(CaminhoPadrao())
        {
        }

        /// <summary>Permite customizar o caminho do arquivo (util para testes).</summary>
        public CasoRepository(string caminhoArquivo)
        {
            _caminhoArquivo = caminhoArquivo;
            _caminhoPasta = Path.GetDirectoryName(_caminhoArquivo);
        }

        /// <summary>
        /// Grava a lista de casos em disco. Escreve primeiro num arquivo temporario e so
        /// entao substitui o definitivo — assim, se algo falhar no meio, o arquivo existente
        /// nao e corrompido. Retorna true em caso de sucesso.
        /// </summary>
        public bool Salvar(List<Caso> casos)
        {
            if (casos == null)
            {
                Logger.Warn("Salvar recebeu lista nula. Abortando gravacao.");
                return false;
            }

            try
            {
                GarantirPasta();

                string temporario = _caminhoArquivo + ".tmp";

                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = Encoding.UTF8
                };

                using (XmlWriter writer = XmlWriter.Create(temporario, settings))
                {
                    Serializer.Serialize(writer, casos);
                }

                // Substitui o arquivo definitivo apenas apos o temporario ter sido escrito por inteiro.
                if (File.Exists(_caminhoArquivo))
                {
                    File.Delete(_caminhoArquivo);
                }
                File.Move(temporario, _caminhoArquivo);

                Logger.Persistence("Salvar", $"{casos.Count} caso(s) gravado(s) em '{_caminhoArquivo}'.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Salvar casos em '{_caminhoArquivo}'");
                return false;
            }
        }

        /// <summary>
        /// Carrega a lista de casos do disco. Se o arquivo nao existir (primeira execucao)
        /// ou houver erro de leitura, retorna lista vazia — o plugin nunca quebra por isso.
        /// </summary>
        public List<Caso> Carregar()
        {
            try
            {
                if (!File.Exists(_caminhoArquivo))
                {
                    Logger.Persistence("Carregar", $"Arquivo '{_caminhoArquivo}' nao existe. Iniciando com lista vazia.");
                    return new List<Caso>();
                }

                using (FileStream fs = new FileStream(_caminhoArquivo, FileMode.Open, FileAccess.Read))
                {
                    object resultado = Serializer.Deserialize(fs);
                    List<Caso> casos = resultado as List<Caso> ?? new List<Caso>();
                    Logger.Persistence("Carregar", $"{casos.Count} caso(s) carregado(s) de '{_caminhoArquivo}'.");
                    return casos;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Carregar casos de '{_caminhoArquivo}'");
                Logger.Warn("Retornando lista vazia para nao bloquear o plugin. Verifique/recupere o XML manualmente.");
                return new List<Caso>();
            }
        }

        /// <summary>Garante que a pasta de destino exista antes de gravar.</summary>
        private void GarantirPasta()
        {
            if (!string.IsNullOrEmpty(_caminhoPasta) && !Directory.Exists(_caminhoPasta))
            {
                Directory.CreateDirectory(_caminhoPasta);
                Logger.Info($"Pasta de dados criada: '{_caminhoPasta}'.");
            }
        }
    }
}