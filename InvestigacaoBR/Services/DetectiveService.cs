using System;
using System.IO;
using System.Xml.Serialization;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    public class DetectiveService
    {
        private static readonly string Pasta = Path.Combine("Plugins", "LSPDFR", "InvestigacaoBR");
        private static readonly string Caminho = Path.Combine(Pasta, "perfil.xml");

        private static readonly string[] NomesPatente =
            { "Agente", "Detetive III", "Detetive II", "Detetive I", "Inspetor", "Delegado" };

        private DetectiveProfile _perfil;
        public DetectiveProfile Perfil => _perfil;

        // ===== INICIALIZACAO =====

        public void Inicializar(string nomeJogador = null)
        {
            try
            {
                _perfil = File.Exists(Caminho) ? Carregar() : new DetectiveProfile
                {
                    Nome = string.IsNullOrEmpty(nomeJogador) ? "Detetive" : nomeJogador,
                    Matricula = $"{Aleatorio.Inteiro(1000, 9999)}"
                };
                if (!string.IsNullOrEmpty(nomeJogador) && _perfil.Nome == "Detetive")
                    _perfil.Nome = nomeJogador;

                Salvar();
                Logger.Info($"DetectiveService: '{_perfil.Nome}' | {NomePatente(_perfil.Patente)} | XP {_perfil.XP} | Rep {_perfil.Reputacao} | Int {_perfil.Integridade}.");
            }
            catch (Exception ex) { Logger.Exception(ex, "DetectiveService.Inicializar"); _perfil = new DetectiveProfile(); }
        }

        // ===== PROGRESSAO — CASOS =====

        public int RegistrarResolucao(Caso caso)
        {
            if (_perfil == null) return 0;

            int xp = XpBasePorTipo(caso.Titulo);
            int rep = 8;

            bool culpadoId = false, evAnalisada = false;
            foreach (PedDoCaso p in caso.Peds) if (p.Role == RolePed.Culpado) culpadoId = true;
            foreach (Evidencia e in caso.Evidencias) if (e.Estado == EstadoEvidencia.Analisada) evAnalisada = true;

            if (culpadoId) { xp += 50; rep += 5; _perfil.PrisoesCertas++; }
            if (evAnalisada) { xp += 30; }
            if (caso.Titulo.StartsWith("Assassinato") || caso.Titulo.StartsWith("Sequestro")) rep += 7;

            // 5D: bonus por integridade alta
            if (_perfil.Integridade >= 80) { xp += 20; rep += 2; }

            _perfil.XP += xp;
            _perfil.Reputacao = Math.Min(100, _perfil.Reputacao + rep);
            _perfil.CasosResolvidos++;

            VerificarPromocao();
            Salvar();
            Logger.Info($"Resolucao: +{xp} XP ({_perfil.XP}), +{rep} rep ({_perfil.Reputacao}/100).");
            return xp;
        }

        public void RegistrarArquivamento()
        {
            if (_perfil == null) return;
            _perfil.Reputacao = Math.Max(0, _perfil.Reputacao - 5);
            _perfil.XP = Math.Max(0, _perfil.XP - 20);
            _perfil.CasosArquivados++;
            Salvar();
        }

        public void RegistrarEvidenciaColetada()
        {
            if (_perfil == null) return;
            _perfil.EvidenciasColetadas++;
            _perfil.XP += 5;
            Salvar();
        }

        public void RegistrarMandadoEmitido()
        {
            if (_perfil == null) return;
            _perfil.MandadosEmitidos++;
            _perfil.XP += 10;
            Salvar();
        }

        // ===== 5D: CORRUPCAO =====

        /// <summary>Jogador aceitou propina. Reduz integridade e reputacao.</summary>
        public void RegistrarPropina(int valorEmMilhares)
        {
            if (_perfil == null) return;

            _perfil.Integridade = Math.Max(0, _perfil.Integridade - 15);
            _perfil.Reputacao = Math.Max(0, _perfil.Reputacao - 12);
            _perfil.DinheiroPropinas += valorEmMilhares;
            _perfil.XP = Math.Max(0, _perfil.XP - 30);

            Salvar();
            Logger.Info($"Propina aceita: ${valorEmMilhares}k. Int: {_perfil.Integridade}/100. Rep: {_perfil.Reputacao}/100.");

            // Aviso de corregedoria quando integridade fica critica
            if (_perfil.Integridade <= 30)
                Notificacao.Alerta("Corregedoria: Investigacao interna em andamento. Cuidado com os proximos passos.");
            else if (_perfil.Integridade <= 50)
                Notificacao.Aviso("Sua reputacao no departamento esta comprometida. Agentes de corregedoria atentos.");
        }

        /// <summary>Jogador recusou propina. Bonus pequeno de integridade.</summary>
        public void RegistrarPropinarRecusada()
        {
            if (_perfil == null) return;
            _perfil.Integridade = Math.Min(100, _perfil.Integridade + 3);
            _perfil.Reputacao = Math.Min(100, _perfil.Reputacao + 2);
            _perfil.PropinaRecusadas++;
            Salvar();
            Logger.Info($"Propina recusada. Int: {_perfil.Integridade}/100.");
        }

        public void Salvar()
        {
            try
            {
                Directory.CreateDirectory(Pasta);
                var s = new XmlSerializer(typeof(DetectiveProfile));
                using (var sw = new StreamWriter(Caminho, false, System.Text.Encoding.UTF8))
                    s.Serialize(sw, _perfil);
            }
            catch (Exception ex) { Logger.Exception(ex, "DetectiveService.Salvar"); }
        }

        // ===== HELPERS PUBLICOS =====

        public static string NomePatente(Rank rank) =>
            (int)rank < NomesPatente.Length ? NomesPatente[(int)rank] : rank.ToString();

        public string ResumoXP() => _perfil == null ? "—" :
            _perfil.PatenteMaxima ? $"XP: {_perfil.XP} (patente maxima)" :
            $"XP: {_perfil.XP} / {_perfil.XpParaProximaPatente}";

        public string BarraReputacao()
        {
            if (_perfil == null) return "";
            int segs = _perfil.Reputacao / 10;
            string cor = _perfil.Reputacao >= 70 ? "~g~" : _perfil.Reputacao >= 40 ? "~y~" : "~r~";
            return $"{cor}{"||||||||||".Substring(0, segs)}~s~{"..........".Substring(0, 10 - segs)}";
        }

        public string BarraIntegridade()
        {
            if (_perfil == null) return "";
            int segs = _perfil.Integridade / 10;
            string cor = _perfil.Integridade >= 70 ? "~b~" : _perfil.Integridade >= 40 ? "~y~" : "~r~";
            return $"{cor}{"||||||||||".Substring(0, segs)}~s~{"..........".Substring(0, 10 - segs)}";
        }

        // ===== PRIVADOS =====

        private void VerificarPromocao()
        {
            if (_perfil == null || _perfil.PatenteMaxima) return;
            Rank atual = _perfil.Patente;
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                if ((int)rank <= (int)atual) continue;
                if (_perfil.XP >= DetectiveProfile.XpThresholds[(int)rank])
                {
                    _perfil.Patente = rank;
                    Notificacao.Sucesso($"PROMOCAO! Nova patente: {NomePatente(rank)}!");
                    Logger.Info($"Promocao: {NomePatente(atual)} -> {NomePatente(rank)}.");
                    atual = rank;
                }
            }
        }

        private static DetectiveProfile Carregar()
        {
            var s = new XmlSerializer(typeof(DetectiveProfile));
            using (var sr = new StreamReader(Caminho, System.Text.Encoding.UTF8))
                return (DetectiveProfile)s.Deserialize(sr);
        }

        private static int XpBasePorTipo(string titulo)
        {
            if (titulo.StartsWith("Assassinato")) return 200;
            if (titulo.StartsWith("Sequestro")) return 180;
            if (titulo.StartsWith("Chacina")) return 160;
            if (titulo.StartsWith("Homicidio") || titulo.StartsWith("Latrocinio")) return 120;
            if (titulo.StartsWith("Trafico Armas") || titulo.StartsWith("Lavagem")) return 100;
            if (titulo.StartsWith("Trafico") || titulo.StartsWith("Lab")) return 80;
            return 60;
        }
    }
}