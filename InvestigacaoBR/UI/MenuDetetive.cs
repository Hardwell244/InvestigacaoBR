using System;
using System.Collections.Generic;
using LemonUI;
using LemonUI.Menus;
using Rage;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;
using InvestigacaoBR.Services;

namespace InvestigacaoBR.UI
{
    /// <summary>
    /// Mesa de trabalho do detetive. Lista os casos aceitos e, por caso, abre as secoes:
    /// Jurisdicao/Cena, Evidencias & Lab, DNA & Suspeitos, Cameras, Telefone & Mandados, Status.
    /// Navegacao manual (item "Voltar") para previsibilidade. Reconstroi cada secao no Shown.
    /// </summary>
    public class MenuDetetive
    {
        private static readonly RolePed[] RolesOrdem =
        {
            RolePed.Indefinido, RolePed.Testemunha, RolePed.PessoaDeInteresse, RolePed.Inocente, RolePed.Culpado
        };
        private static readonly string[] RolesNomes =
        {
            "Indefinido", "Testemunha", "Pessoa de Interesse", "Inocente", "Culpado"
        };
        private static readonly StatusCaso[] StatusOrdem =
        {
            StatusCaso.Aberto, StatusCaso.Arquivado, StatusCaso.Resolvido
        };
        private static readonly string[] StatusNomes = { "Aberto", "Arquivado", "Resolvido" };

        private readonly CasoService _casoService;
        private readonly CenaService _cenaService;
        private readonly LaboratorioService _laboratorioService;
        private readonly CameraService _cameraService;
        private readonly MandadoService _mandadoService;

        private readonly NativeMenu _menuPrincipal;
        private readonly NativeMenu _menuCaso;
        private readonly NativeMenu _menuCena;
        private readonly NativeMenu _menuEvidencias;
        private readonly NativeMenu _menuDna;
        private readonly NativeMenu _menuCameras;
        private readonly NativeMenu _menuTelefone;
        private readonly NativeMenu _menuStatus;

        private Caso _casoAtual;

        public MenuDetetive(ObjectPool pool, CasoService casoService, CenaService cenaService,
            LaboratorioService laboratorioService, CameraService cameraService, MandadoService mandadoService)
        {
            _casoService = casoService;
            _cenaService = cenaService;
            _laboratorioService = laboratorioService;
            _cameraService = cameraService;
            _mandadoService = mandadoService;

            _menuPrincipal = new NativeMenu("DETETIVE", "Meus Casos");
            _menuCaso = new NativeMenu("DETETIVE", "Caso");
            _menuCena = new NativeMenu("DETETIVE", "Jurisdicao & Cena");
            _menuEvidencias = new NativeMenu("DETETIVE", "Evidencias & Lab");
            _menuDna = new NativeMenu("DETETIVE", "DNA & Suspeitos");
            _menuCameras = new NativeMenu("DETETIVE", "Cameras");
            _menuTelefone = new NativeMenu("DETETIVE", "Telefone & Mandados");
            _menuStatus = new NativeMenu("DETETIVE", "Status do Caso");

            pool.Add(_menuPrincipal);
            pool.Add(_menuCaso);
            pool.Add(_menuCena);
            pool.Add(_menuEvidencias);
            pool.Add(_menuDna);
            pool.Add(_menuCameras);
            pool.Add(_menuTelefone);
            pool.Add(_menuStatus);

            ConstruirMenuCaso();

            _menuCena.Shown += (s, e) => RebuildCena();
            _menuEvidencias.Shown += (s, e) => RebuildEvidencias();
            _menuDna.Shown += (s, e) => RebuildDna();
            _menuCameras.Shown += (s, e) => RebuildCameras();
            _menuTelefone.Shown += (s, e) => RebuildTelefone();
            _menuStatus.Shown += (s, e) => RebuildStatus();
        }

        // ===================== Abertura / navegacao =====================

        public void Abrir()
        {
            RebuildPrincipal();
            _menuPrincipal.Visible = true;
            Logger.Menu("Detetive", "aberto");
        }

        private static void Navegar(NativeMenu de, NativeMenu para)
        {
            de.Visible = false;
            para.Visible = true;
        }

        private void RebuildPrincipal()
        {
            _menuPrincipal.Clear();

            List<Caso> casos = new List<Caso>(_casoService.ObterDoDetetive());
            if (casos.Count == 0)
            {
                _menuPrincipal.Add(new NativeItem("Nenhum caso aceito", "Pegue casos no menu de selecao (F6).") { Enabled = false });
                return;
            }

            foreach (Caso caso in casos)
            {
                Caso c = caso;
                string desc = $"Status: {StatusTexto(c.Status)} | Peds: {c.Peds.Count} | Evidencias: {c.Evidencias.Count}";
                NativeItem item = new NativeItem(c.Titulo, desc);
                item.Activated += (s, e) =>
                {
                    _casoAtual = c;
                    _menuCaso.Name = c.Titulo;
                    Navegar(_menuPrincipal, _menuCaso);
                    Logger.Menu("Detetive", $"abriu caso '{c.Titulo}'");
                };
                _menuPrincipal.Add(item);
            }
        }

        private void ConstruirMenuCaso()
        {
            NativeItem iCena = new NativeItem("Jurisdicao & Cena", "Assumir, isolar e processar a cena.");
            iCena.Activated += (s, e) => Navegar(_menuCaso, _menuCena);

            NativeItem iEvid = new NativeItem("Evidencias & Lab", "Coletar e enviar para analise.");
            iEvid.Activated += (s, e) => Navegar(_menuCaso, _menuEvidencias);

            NativeItem iDna = new NativeItem("DNA & Suspeitos", "Cruzar DNA e classificar os peds.");
            iDna.Activated += (s, e) => Navegar(_menuCaso, _menuDna);

            NativeItem iCam = new NativeItem("Cameras", "Revisar gravacoes da area.");
            iCam.Activated += (s, e) => Navegar(_menuCaso, _menuCameras);

            NativeItem iTel = new NativeItem("Telefone & Mandados", "Emitir mandados e rastrear.");
            iTel.Activated += (s, e) => Navegar(_menuCaso, _menuTelefone);

            NativeItem iStatus = new NativeItem("Status do Caso", "Atualizar o status do caso.");
            iStatus.Activated += (s, e) => Navegar(_menuCaso, _menuStatus);

            NativeItem voltar = new NativeItem("< Voltar aos casos");
            voltar.Activated += (s, e) => Navegar(_menuCaso, _menuPrincipal);

            _menuCaso.Add(iCena);
            _menuCaso.Add(iEvid);
            _menuCaso.Add(iDna);
            _menuCaso.Add(iCam);
            _menuCaso.Add(iTel);
            _menuCaso.Add(iStatus);
            _menuCaso.Add(voltar);
        }

        private void AdicionarVoltar(NativeMenu secao)
        {
            NativeItem voltar = new NativeItem("< Voltar");
            voltar.Activated += (s, e) => Navegar(secao, _menuCaso);
            secao.Add(voltar);
        }

        // ===================== Jurisdicao & Cena =====================

        private void RebuildCena()
        {
            _menuCena.Clear();
            if (_casoAtual == null) { return; }
            Caso caso = _casoAtual;

            NativeItem jurisdicao = new NativeItem("Assumir jurisdicao",
                caso.JurisdicaoAssumida ? "Ja assumida." : "Assume a responsabilidade pela cena.")
            { Enabled = !caso.JurisdicaoAssumida };
            jurisdicao.Activated += (s, e) =>
            {
                if (caso.AssumirJurisdicao()) { _casoService.Salvar(); Game.DisplayNotification("~b~Jurisdicao assumida."); }
                RebuildCena();
            };

            NativeItem isolar = new NativeItem("Isolar area",
                !caso.JurisdicaoAssumida ? "Assuma a jurisdicao primeiro." : caso.CenaIsolada ? "Ja isolada." : "Cerca a cena com a fita.")
            { Enabled = caso.JurisdicaoAssumida && !caso.CenaIsolada };
            isolar.Activated += (s, e) =>
            {
                if (caso.IsolarCena()) { _cenaService.SpawnarFitaIsolamento(caso); _casoService.Salvar(); Game.DisplayNotification("~b~Area isolada."); }
                RebuildCena();
            };

            NativeItem processar = new NativeItem("Processar cena",
                !caso.CenaIsolada ? "Isole a cena primeiro." : caso.CenaProcessada ? "Ja processada." : "Libera a coleta de evidencias.")
            { Enabled = caso.CenaIsolada && !caso.CenaProcessada };
            processar.Activated += (s, e) =>
            {
                if (caso.ProcessarCena()) { _casoService.Salvar(); Game.DisplayNotification("~b~Cena processada. Pode coletar evidencias."); }
                RebuildCena();
            };

            _menuCena.Add(jurisdicao);
            _menuCena.Add(isolar);
            _menuCena.Add(processar);
            AdicionarVoltar(_menuCena);
        }

        // ===================== Evidencias & Lab =====================

        private void RebuildEvidencias()
        {
            _menuEvidencias.Clear();
            if (_casoAtual == null) { return; }
            Caso caso = _casoAtual;

            if (caso.Evidencias.Count == 0)
            {
                _menuEvidencias.Add(new NativeItem("Sem evidencias", "Nada registrado neste caso.") { Enabled = false });
                AdicionarVoltar(_menuEvidencias);
                return;
            }

            foreach (Evidencia ev in caso.Evidencias)
            {
                Evidencia evidencia = ev;
                NativeItem item = new NativeItem(evidencia.Titulo, DescricaoEvidencia(evidencia));

                switch (evidencia.Estado)
                {
                    case EstadoEvidencia.NaCena:
                        item.Enabled = caso.CenaProcessada;
                        item.Activated += (s, e) =>
                        {
                            if (!caso.CenaProcessada) { Game.DisplayNotification("~r~Processe a cena antes de coletar."); return; }
                            if (evidencia.Coletar(TempoJogo.Agora()))
                            {
                                if (evidencia.PropVivo != null && evidencia.PropVivo.Exists())
                                {
                                    try { evidencia.PropVivo.Delete(); } catch { }
                                    evidencia.PropVivo = null;
                                }
                                _casoService.Salvar();
                                Game.DisplayNotification($"~g~Coletado:~s~ {evidencia.Titulo}");
                            }
                            RebuildEvidencias();
                        };
                        break;

                    case EstadoEvidencia.Coletada:
                        item.Activated += (s, e) =>
                        {
                            if (_laboratorioService.EnviarParaAnalise(evidencia)) { RebuildEvidencias(); }
                        };
                        break;

                    case EstadoEvidencia.NoLab:
                        item.Enabled = false;
                        break;

                    case EstadoEvidencia.Analisada:
                        item.Enabled = false;
                        break;
                }

                _menuEvidencias.Add(item);
            }

            AdicionarVoltar(_menuEvidencias);
        }

        private static string DescricaoEvidencia(Evidencia ev)
        {
            switch (ev.Estado)
            {
                case EstadoEvidencia.NaCena: return "Na cena. Selecione para COLETAR.";
                case EstadoEvidencia.Coletada: return "Coletada. Selecione para ENVIAR AO LAB.";
                case EstadoEvidencia.NoLab: return "No laboratorio. Aguardando laudo...";
                case EstadoEvidencia.Analisada:
                    string dna = ev.PossuiDna ? $" DNA: {ev.PerfilDnaId}." : "";
                    return $"LAUDO: {ev.ResultadoForense}{dna}";
                default: return ev.Descricao;
            }
        }

        // ===================== DNA & Suspeitos =====================

        private void RebuildDna()
        {
            _menuDna.Clear();
            if (_casoAtual == null) { return; }
            Caso caso = _casoAtual;

            foreach (PedDoCaso ped in caso.Peds)
            {
                PedDoCaso p = ped;
                NativeListItem<string> item = new NativeListItem<string>(p.Nome, DescricaoPed(caso, p), RolesNomes);
                item.SelectedIndex = IndiceRole(p.Role);
                item.Activated += (s, e) =>
                {
                    RolePed novo = RolesOrdem[item.SelectedIndex];
                    p.AlterarRole(novo);
                    _casoService.Salvar();
                    item.Description = DescricaoPed(caso, p);
                    Game.DisplayNotification($"~b~{p.Nome}:~s~ {RolesNomes[item.SelectedIndex]}");
                };
                _menuDna.Add(item);
            }

            AdicionarVoltar(_menuDna);
        }

        private static string DescricaoPed(Caso caso, PedDoCaso ped)
        {
            string ident = ped.DataNascimento == DateTime.MinValue
                ? "Nao identificado."
                : $"Nasc.: {ped.DataNascimento:dd/MM/yyyy} | {ped.Genero}{(ped.Procurado ? " | PROCURADO" : "")}.";

            string dna = "";
            if (ped.PossuiDna)
            {
                bool bateLaudo = false;
                foreach (Evidencia ev in caso.Evidencias)
                {
                    if (ev.Estado == EstadoEvidencia.Analisada && ev.PossuiDna && ev.PerfilDnaId == ped.PerfilDnaId)
                    {
                        bateLaudo = true;
                        break;
                    }
                }
                dna = bateLaudo ? $" DNA BATE com evidencia ({ped.PerfilDnaId})!" : "";
            }

            return $"Papel atual: {RolesNomes[IndiceRole(ped.Role)]}. {ident}{dna} (Enter aplica o papel selecionado)";
        }

        private static int IndiceRole(RolePed role)
        {
            for (int i = 0; i < RolesOrdem.Length; i++)
            {
                if (RolesOrdem[i] == role) { return i; }
            }
            return 0;
        }

        // ===================== Cameras =====================

        private void RebuildCameras()
        {
            _menuCameras.Clear();
            if (_casoAtual == null) { return; }
            Caso caso = _casoAtual;

            if (caso.Cameras.Count == 0)
            {
                _menuCameras.Add(new NativeItem("Sem cameras", "Nenhuma camera na area deste caso.") { Enabled = false });
                AdicionarVoltar(_menuCameras);
                return;
            }

            foreach (GravacaoCamera cam in caso.Cameras)
            {
                GravacaoCamera gravacao = cam;
                string desc = gravacao.Revisada ? $"Revisada: {gravacao.InfoRevelada}" : "Selecione para ASSISTIR (BACKSPACE sai).";
                NativeItem item = new NativeItem(gravacao.Local, desc);
                item.Activated += (s, e) =>
                {
                    _menuCameras.Visible = false; // fecha o menu antes de renderizar a camera
                    _cameraService.Visualizar(gravacao);
                };
                _menuCameras.Add(item);
            }

            AdicionarVoltar(_menuCameras);
        }

        // ===================== Telefone & Mandados =====================

        private void RebuildTelefone()
        {
            _menuTelefone.Clear();
            if (_casoAtual == null) { return; }
            Caso caso = _casoAtual;

            foreach (PedDoCaso ped in caso.Peds)
            {
                PedDoCaso p = ped;
                string desc = p.MandadoEmitido
                    ? $"Mandado emitido. {p.RegistroTelefonico}"
                    : "Selecione para EMITIR MANDADO (revela telefone + rastreio).";
                NativeItem item = new NativeItem(p.Nome, desc);
                item.Activated += (s, e) =>
                {
                    _mandadoService.Emitir(p);
                    item.Description = $"Mandado emitido. {p.RegistroTelefonico}";
                    RebuildTelefone();
                };
                _menuTelefone.Add(item);
            }

            AdicionarVoltar(_menuTelefone);
        }

        // ===================== Status =====================

        private void RebuildStatus()
        {
            _menuStatus.Clear();
            if (_casoAtual == null) { return; }
            Caso caso = _casoAtual;

            NativeListItem<string> item = new NativeListItem<string>("Status do caso", $"Atual: {StatusTexto(caso.Status)}", StatusNomes);
            item.SelectedIndex = IndiceStatus(caso.Status);
            item.Activated += (s, e) =>
            {
                StatusCaso novo = StatusOrdem[item.SelectedIndex];
                if (_casoService.AtualizarStatus(caso.Id, novo))
                {
                    if (novo == StatusCaso.Resolvido || novo == StatusCaso.Arquivado)
                    {
                        _cenaService.RemoverCenaCompleta(caso);
                    }
                    Game.DisplayNotification($"~b~Caso {StatusTexto(novo)}.");
                }
                item.Description = $"Atual: {StatusTexto(caso.Status)}";
            };

            _menuStatus.Add(item);
            AdicionarVoltar(_menuStatus);
        }

        // ===================== Helpers =====================

        private static int IndiceStatus(StatusCaso status)
        {
            for (int i = 0; i < StatusOrdem.Length; i++)
            {
                if (StatusOrdem[i] == status) { return i; }
            }
            return 0;
        }

        private static string StatusTexto(StatusCaso status)
        {
            switch (status)
            {
                case StatusCaso.Disponivel: return "Disponivel";
                case StatusCaso.Aberto: return "Aberto";
                case StatusCaso.Arquivado: return "Arquivado";
                case StatusCaso.Resolvido: return "Resolvido";
                default: return status.ToString();
            }
        }
    }
}