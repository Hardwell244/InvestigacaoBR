using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace InvestigacaoBR.Core
{
    /// <summary>
    /// Le e aplica as configuracoes de InvestigacaoBR.ini.
    /// Se o arquivo nao existir, cria com valores padrao e comentarios explicativos.
    /// Chamar Config.Carregar() em Initialize() antes de qualquer sistema subir.
    /// </summary>
    public static class Config
    {
        private static readonly string Pasta = Path.Combine("Plugins", "LSPDFR", "InvestigacaoBR");
        private static readonly string Caminho = Path.Combine(Pasta, "InvestigacaoBR.ini");

        // ===== TECLAS =====
        public static Keys TeclaMenuCasos { get; private set; } = Keys.F6;
        public static Keys TeclaMenuDetetive { get; private set; } = Keys.I;
        public static Keys TeclaLimparCena { get; private set; } = Keys.End;
        public static Keys TeclaInterrogar { get; private set; } = Keys.OemQuestion;

        // ===== GAMEPLAY =====
        public static float RaioInterrogacao { get; private set; } = 3f;
        public static float RaioFuga { get; private set; } = 4f;
        public static float RaioBlipProximidade { get; private set; } = 10f;
        public static int MetaCasosPool { get; private set; } = 3;

        // ===== LABORATORIO =====
        public static int DelayLabMinMs { get; private set; } = 60000;
        public static int DelayLabMaxMs { get; private set; } = 150000;

        // ===== CARREGAMENTO =====

        public static void Carregar()
        {
            try
            {
                if (!File.Exists(Caminho))
                {
                    Directory.CreateDirectory(Pasta);
                    CriarArquivoPadrao();
                    Logger.Info($"Config: arquivo padrao criado em '{Caminho}'. Usando valores padrao.");
                    return; // propriedades ja tem os valores padrao definidos acima
                }

                var ini = LerIni(Caminho);

                // Teclas
                TeclaMenuCasos = LerTecla(ini, "Teclas", "MenuCasos", Keys.F6);
                TeclaMenuDetetive = LerTecla(ini, "Teclas", "MenuDetetive", Keys.I);
                TeclaLimparCena = LerTecla(ini, "Teclas", "LimparCena", Keys.End);
                TeclaInterrogar = LerTecla(ini, "Teclas", "Interrogar", Keys.OemQuestion);

                // Gameplay
                RaioInterrogacao = LerFloat(ini, "Gameplay", "RaioInterrogacao", 3f);
                RaioFuga = LerFloat(ini, "Gameplay", "RaioFuga", 4f);
                RaioBlipProximidade = LerFloat(ini, "Gameplay", "RaioBlipProximidade", 10f);
                MetaCasosPool = LerInt(ini, "Gameplay", "MetaCasosPool", 3);

                // Laboratorio
                DelayLabMinMs = LerInt(ini, "Laboratorio", "DelayMinMs", 60000);
                DelayLabMaxMs = LerInt(ini, "Laboratorio", "DelayMaxMs", 150000);

                // Sanidade: min nao pode ser maior que max
                if (DelayLabMinMs > DelayLabMaxMs) DelayLabMinMs = DelayLabMaxMs;

                Logger.Info($"Config carregado | Teclas: F6={TeclaMenuCasos} I={TeclaMenuDetetive} " +
                            $"End={TeclaLimparCena} ?={TeclaInterrogar} | " +
                            $"Pool={MetaCasosPool} | Lab={DelayLabMinMs}-{DelayLabMaxMs}ms");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Config.Carregar");
                Logger.Warn("Config: falha ao ler INI — usando todos os valores padrao.");
            }
        }

        // ===== HELPERS DE LEITURA =====

        private static Dictionary<string, Dictionary<string, string>> LerIni(string path)
        {
            var doc = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            string sec = "";

            foreach (string linha in File.ReadAllLines(path))
            {
                string l = linha.Trim();
                if (string.IsNullOrEmpty(l) || l.StartsWith(";") || l.StartsWith("#")) continue;

                if (l.StartsWith("[") && l.EndsWith("]"))
                {
                    sec = l.Substring(1, l.Length - 2).Trim();
                    if (!doc.ContainsKey(sec))
                        doc[sec] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else if (l.Contains("=") && !string.IsNullOrEmpty(sec))
                {
                    int eq = l.IndexOf('=');
                    string k = l.Substring(0, eq).Trim();
                    string v = l.Substring(eq + 1).Trim();
                    int ci = v.IndexOf(';');
                    if (ci >= 0) v = v.Substring(0, ci).Trim(); // remove comentario inline
                    doc[sec][k] = v;
                }
            }
            return doc;
        }

        private static Keys LerTecla(Dictionary<string, Dictionary<string, string>> ini,
            string sec, string key, Keys padrao)
        {
            if (!ini.TryGetValue(sec, out var s) || !s.TryGetValue(key, out string v)) return padrao;
            if (Enum.TryParse(v, true, out Keys k)) return k;
            Logger.Warn($"Config: tecla '{v}' invalida para [{sec}]{key}. Usando padrao: {padrao}.");
            return padrao;
        }

        private static float LerFloat(Dictionary<string, Dictionary<string, string>> ini,
            string sec, string key, float padrao)
        {
            if (!ini.TryGetValue(sec, out var s) || !s.TryGetValue(key, out string v)) return padrao;
            if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float r)) return r;
            Logger.Warn($"Config: valor '{v}' invalido para [{sec}]{key}. Usando padrao: {padrao}.");
            return padrao;
        }

        private static int LerInt(Dictionary<string, Dictionary<string, string>> ini,
            string sec, string key, int padrao)
        {
            if (!ini.TryGetValue(sec, out var s) || !s.TryGetValue(key, out string v)) return padrao;
            if (int.TryParse(v, out int r)) return r;
            Logger.Warn($"Config: valor '{v}' invalido para [{sec}]{key}. Usando padrao: {padrao}.");
            return padrao;
        }

        // ===== CRIACAO DO ARQUIVO PADRAO =====

        private static void CriarArquivoPadrao()
        {
            var linhas = new[]
            {
                "; ================================================================",
                ";  InvestigacaoBR — Arquivo de Configuracao",
                ";  Pasta: Plugins\\LSPDFR\\InvestigacaoBR\\InvestigacaoBR.ini",
                "; ================================================================",
                ";",
                ";  Nomes de teclas validos (System.Windows.Forms.Keys):",
                ";    Letras  : A-Z",
                ";    Numeros : D0-D9, NumPad0-NumPad9",
                ";    Funcao  : F1-F12",
                ";    Especial: End, Home, Insert, Delete, PageUp, PageDown",
                ";    ABNT    : OemQuestion (?), OemSemicolon (;), OemComma (,), OemPeriod (.)",
                "",
                "[Teclas]",
                "; Abre o menu de selecao de casos (pool F6)",
                "MenuCasos = F6",
                "",
                "; Abre a mesa de trabalho do detetive",
                "MenuDetetive = I",
                "",
                "; Limpa visuais de todas as cenas (peds liberados, dados preservados)",
                "LimparCena = End",
                "",
                "; Interroga o ped mais proximo — tecla ? no teclado ABNT2",
                "Interrogar = OemQuestion",
                "",
                "[Gameplay]",
                "; Distancia maxima em metros para abordar/interrogar um ped (padrao: 3)",
                "RaioInterrogacao = 3",
                "",
                "; Distancia em metros para suspeito com mandado comecar a fugir (padrao: 4)",
                "RaioFuga = 4",
                "",
                "; Distancia em metros para o blip da cena sumir ao chegar (padrao: 10)",
                "RaioBlipProximidade = 10",
                "",
                "; Quantos casos manter disponiveis no pool do F6 (padrao: 3, max recomendado: 6)",
                "MetaCasosPool = 3",
                "",
                "[Laboratorio]",
                "; Tempo minimo de analise laboratorial em milissegundos",
                "; 60000 = 1 minuto | 30000 = 30 segundos",
                "DelayMinMs = 60000",
                "",
                "; Tempo maximo de analise laboratorial em milissegundos",
                "; 150000 = 2 minutos e 30 segundos",
                "DelayMaxMs = 150000",
            };
            File.WriteAllLines(Caminho, linhas, System.Text.Encoding.UTF8);
        }
    }
}