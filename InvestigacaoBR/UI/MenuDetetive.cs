using System;
using LemonUI;
using LemonUI.Menus;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;
using InvestigacaoBR.Services;

namespace InvestigacaoBR.UI
{
    /// <summary>
    /// Mesa de trabalho do detetive. fix #8: reestruturado com opcoes globais (sem caso ativo)
    /// e listas de casos divididas por status (Em Andamento / Historico).
    /// fix #3: blips de mandado removidos ao encerrar caso.
    /// fix #9: Notificacao LSPD-style em todas as acoes.
    /// </summary>
    public class MenuDetetive
    {
        private static readonly RolePed[] RolesOrdem = { RolePed.Indefinido, RolePed.Testemunha, RolePed.PessoaDeInteresse, RolePed.Inocente, RolePed.Culpado };
        private static readonly string[] RolesNomes = { "Indefinido", "Testemunha", "Pessoa de Interesse", "Inocente", "Culpado" };
        private static readonly StatusCaso[] StatusOrdem = { StatusCaso.Aberto, StatusCaso.Arquivado, StatusCaso.Resolvido };
        private static readonly string[] StatusNomes = { "Aberto", "Arquivado", "Resolvido" };

        private readonly CasoService _casoService;
        private readonly CenaService _cenaService;
        private readonly LaboratorioService _laboratorioService;
        private readonly CameraService _cameraService;
        private readonly MandadoService _mandadoService;
        private readonly GeradorCasos _geradorCasos;

        private readonly NativeMenu _menuPrincipal;   // mesa global
        private readonly NativeMenu _menuAbertos;     // casos em andamento
        private readonly NativeMenu _menuHistorico;   // casos encerrados
        private readonly NativeMenu _menuCaso;        // tabs por caso
        private readonly NativeMenu _menuCena;
        private readonly NativeMenu _menuEvidencias;
        private readonly NativeMenu _menuDna;
        private readonly NativeMenu _menuCameras;
        private readonly NativeMenu _menuTelefone;
        private readonly NativeMenu _menuStatus;

        private Caso _casoAtual;

        public MenuDetetive(ObjectPool pool,
            CasoService casoService, CenaService cenaService,
            LaboratorioService laboratorioService, CameraService cameraService,
            MandadoService mandadoService, GeradorCasos geradorCasos)
        {
            _casoService = casoService;
            _cenaService = cenaService;
            _laboratorioService = laboratorioService;
            _cameraService = cameraService;
            _mandadoService = mandadoService;
            _geradorCasos = geradorCasos;

            _menuPrincipal = new NativeMenu("DETETIVE", "Mesa de Trabalho");
            _menuAbertos = new NativeMenu("DETETIVE", "Em Andamento");
            _menuHistorico = new NativeMenu("DETETIVE", "Historico");
            _menuCaso = new NativeMenu("DETETIVE", "Caso");
            _menuCena = new NativeMenu("DETETIVE", "Jurisdicao e Cena");
            _menuEvidencias = new NativeMenu("DETETIVE", "Evidencias e Lab");
            _menuDna = new NativeMenu("DETETIVE", "DNA e Suspeitos");
            _menuCameras = new NativeMenu("DETETIVE", "Cameras");
            _menuTelefone = new NativeMenu("DETETIVE", "Telefone e Mandados");
            _menuStatus = new NativeMenu("DETETIVE", "Status do Caso");

            pool.Add(_menuPrincipal);
            pool.Add(_menuAbertos);
            pool.Add(_menuHistorico);
            pool.Add(_menuCaso);
            pool.Add(_menuCena);
            pool.Add(_menuEvidencias);
            pool.Add(_menuDna);
            pool.Add(_menuCameras);
            pool.Add(_menuTelefone);
            pool.Add(_menuStatus);

            _menuPrincipal.Shown += (s, e) => RebuildPrincipal();
            _menuAbertos.Shown += (s, e) => RebuildAbertos();
            _menuHistorico.Shown += (s, e) => RebuildHistorico();
            _menuCena.Shown += (s, e) => RebuildCena();
            _menuEvidencias.Shown += (s, e) => RebuildEvidencias();
            _menuDna.Shown += (s, e) => RebuildDna();
            _menuCameras.Shown += (s, e) => RebuildCameras();
            _menuTelefone.Shown += (s, e) => RebuildTelefone();
            _menuStatus.Shown += (s, e) => RebuildStatus();

            ConstruirMenuCaso();
        }

        // ===== ABERTURA / FECHAMENTO =====

        public void Abrir()
        {
            _menuPrincipal.Visible = true;
            Logger.Menu("Detetive", "aberto");
        }

        public void Fechar()
        {
            _menuPrincipal.Visible = false;
            _menuAbertos.Visible = false;
            _menuHistorico.Visible = false;
            _menuCaso.Visible = false;
            _menuCena.Visible = false;
            _menuEvidencias.Visible = false;
            _menuDna.Visible = false;
            _menuCameras.Visible = false;
            _menuTelefone.Visible = false;
            _menuStatus.Visible = false;
        }

        private static void Navegar(NativeMenu de, NativeMenu para)
        {
            de.Visible = false;
            para.Visible = true;
        }

        // ===== MENU PRINCIPAL =====

        private void RebuildPrincipal()
        {
            _menuPrincipal.Clear();

            int qtdAbertos = 0, qtdHistorico = 0;
            foreach (Caso c in _casoService.ObterDoDetetive())
            {
                if (c.Status == StatusCaso.Aberto) qtdAbertos++;
                else qtdHistorico++;
            }

            NativeItem iAbertos = new NativeItem(
                $"Investigacoes em Andamento  [{qtdAbertos}]",
                qtdAbertos > 0 ? "Acessar e gerir casos abertos." : "Nenhum caso aberto. Use F6 para aceitar um.");
            iAbertos.Enabled = qtdAbertos > 0;
            iAbertos.Activated += (s, e) => Navegar(_menuPrincipal, _menuAbertos);

            NativeItem iHistorico = new NativeItem(
                $"Historico  [{qtdHistorico}]",
                qtdHistorico > 0 ? "Casos resolvidos e arquivados." : "Nenhum caso encerrado ainda.");
            iHistorico.Enabled = qtdHistorico > 0;
            iHistorico.Activated += (s, e) => Navegar(_menuPrincipal, _menuHistorico);

            NativeItem iGerar = new NativeItem("Gerar Nova Investigacao",
                "Reabastece o pool com ate 3 casos disponiveis no F6.");
            iGerar.Activated += (s, e) =>
            {
                _geradorCasos.GarantirPool();
                int disp = 0;
                foreach (Caso c in _casoService.ObterDisponiveis()) disp++;
                Notificacao.Sucesso($"Pool atualizado. {disp} caso(s) disponivel(is) no F6.");
                RebuildPrincipal();
            };

            NativeItem iLimpar = new NativeItem("Limpar Cenas Ativas",
                "Remove todos os visuais das cenas (peds, props, blips). Dados preservados.");
            iLimpar.Activated += (s, e) =>
            {
                int limpas = 0;
                foreach (Caso caso in _casoService.ObterDoDetetive())
                {
                    if (_cenaService.CenaMontada(caso.Id)) { _cenaService.LimparCena(caso); limpas++; }
                }
                Notificacao.Aviso(limpas > 0 ? $"{limpas} cena(s) limpa(s)." : "Nenhuma cena ativa.");
            };

            _menuPrincipal.Add(iAbertos);
            _menuPrincipal.Add(iHistorico);
            _menuPrincipal.Add(new NativeItem("---  Ferramentas Globais  ---") { Enabled = false });
            _menuPrincipal.Add(iGerar);
            _menuPrincipal.Add(iLimpar);
        }

        // ===== LISTAS DE CASOS =====

        private void RebuildAbertos()
        {
            _menuAbertos.Clear();
            bool algum = false;
            foreach (Caso caso in _casoService.ObterDoDetetive())
            {
                if (caso.Status != StatusCaso.Aberto) continue;
                AdicionarItemCaso(_menuAbertos, caso);
                algum = true;
            }
            if (!algum) _menuAbertos.Add(new NativeItem("Nenhum caso em andamento.") { Enabled = false });
            AdicionarVoltar(_menuAbertos, _menuPrincipal);
        }

        private void RebuildHistorico()
        {
            _menuHistorico.Clear();
            bool algum = false;
            foreach (Caso caso in _casoService.ObterDoDetetive())
            {
                if (caso.Status == StatusCaso.Aberto) continue;
                AdicionarItemCaso(_menuHistorico, caso);
                algum = true;
            }
            if (!algum) _menuHistorico.Add(new NativeItem("Nenhum caso encerrado.") { Enabled = false });
            AdicionarVoltar(_menuHistorico, _menuPrincipal);
        }

        private void AdicionarItemCaso(NativeMenu menu, Caso caso)
        {
            Caso c = caso;
            string desc = $"[{StatusTexto(c.Status)}]  Peds: {c.Peds.Count}  |  Ev: {c.Evidencias.Count}  |  Cam: {c.Cameras.Count}";
            NativeItem item = new NativeItem(c.Titulo, desc);
            item.Activated += (s, e) =>
            {
                _casoAtual = c;
                _menuCaso.Name = c.Titulo;
                Navegar(menu, _menuCaso);
                Logger.Menu("Detetive", $"abriu caso '{c.Titulo}'");
            };
            menu.Add(item);
        }

        private static void AdicionarVoltar(NativeMenu de, NativeMenu para)
        {
            NativeItem v = new NativeItem("< Voltar");
            v.Activated += (s, e) => Navegar(de, para);
            de.Add(v);
        }

        // ===== MENU DO CASO (tabs) =====

        private void ConstruirMenuCaso()
        {
            NativeItem iCena = new NativeItem("Jurisdicao e Cena", "Assumir, isolar e processar a cena.");
            NativeItem iEvid = new NativeItem("Evidencias e Lab", "Coletar evidencias e enviar ao laboratorio.");
            NativeItem iDna = new NativeItem("DNA e Suspeitos", "Cruzar DNA e classificar os peds.");
            NativeItem iCam = new NativeItem("Cameras", "Revisar gravacoes da area.");
            NativeItem iTel = new NativeItem("Telefone e Mandados", "Emitir mandados e rastrear suspeitos.");
            NativeItem iStatus = new NativeItem("Status do Caso", "Atualizar o status da investigacao.");

            iCena.Activated += (s, e) => Navegar(_menuCaso, _menuCena);
            iEvid.Activated += (s, e) => Navegar(_menuCaso, _menuEvidencias);
            iDna.Activated += (s, e) => Navegar(_menuCaso, _menuDna);
            iCam.Activated += (s, e) => Navegar(_menuCaso, _menuCameras);
            iTel.Activated += (s, e) => Navegar(_menuCaso, _menuTelefone);
            iStatus.Activated += (s, e) => Navegar(_menuCaso, _menuStatus);

            // Volta para a lista correta baseado no status atual do caso
            NativeItem voltar = new NativeItem("< Voltar");
            voltar.Activated += (s, e) =>
            {
                _menuCaso.Visible = false;
                bool aberto = _casoAtual != null && _casoAtual.Status == StatusCaso.Aberto;
                if (aberto) _menuAbertos.Visible = true;
                else _menuHistorico.Visible = true;
            };

            _menuCaso.Add(iCena);
            _menuCaso.Add(iEvid);
            _menuCaso.Add(iDna);
            _menuCaso.Add(iCam);
            _menuCaso.Add(iTel);
            _menuCaso.Add(iStatus);
            _menuCaso.Add(voltar);
        }

        // ===== TABS DE INVESTIGACAO =====

        private void RebuildCena()
        {
            _menuCena.Clear();
            if (_casoAtual == null) { VoltarCaso(_menuCena); return; }
            Caso caso = _casoAtual;

            NativeItem iJur = new NativeItem("Assumir jurisdicao",
                caso.JurisdicaoAssumida ? "Ja assumida." : "Assume a responsabilidade pela cena.")
            { Enabled = !caso.JurisdicaoAssumida };
            iJur.Activated += (s, e) =>
            {
                if (caso.AssumirJurisdicao()) { _casoService.Salvar(); Notificacao.Info("Jurisdicao assumida."); }
                RebuildCena();
            };

            NativeItem iIsol = new NativeItem("Isolar area",
                !caso.JurisdicaoAssumida ? "Assuma a jurisdicao primeiro." :
                caso.CenaIsolada ? "Ja isolada." : "Cerca a cena com fita de isolamento.")
            { Enabled = caso.JurisdicaoAssumida && !caso.CenaIsolada };
            iIsol.Activated += (s, e) =>
            {
                if (caso.IsolarCena())
                {
                    _cenaService.SpawnarFitaIsolamento(caso);
                    _casoService.Salvar();
                    Notificacao.Info("Area isolada.");
                }
                RebuildCena();
            };

            NativeItem iProc = new NativeItem("Processar cena",
                !caso.CenaIsolada ? "Isole a cena primeiro." :
                caso.CenaProcessada ? "Ja processada." : "Libera a coleta de evidencias.")
            { Enabled = caso.CenaIsolada && !caso.CenaProcessada };
            iProc.Activated += (s, e) =>
            {
                if (caso.ProcessarCena()) { _casoService.Salvar(); Notificacao.Sucesso("Cena processada. Colete as evidencias."); }
                RebuildCena();
            };

            _menuCena.Add(iJur);
            _menuCena.Add(iIsol);
            _menuCena.Add(iProc);
            VoltarCaso(_menuCena);
        }

        private void RebuildEvidencias()
        {
            _menuEvidencias.Clear();
            if (_casoAtual == null) { VoltarCaso(_menuEvidencias); return; }
            Caso caso = _casoAtual;

            if (caso.Evidencias.Count == 0)
            {
                _menuEvidencias.Add(new NativeItem("Sem evidencias registradas.") { Enabled = false });
                VoltarCaso(_menuEvidencias);
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
                            if (!caso.CenaProcessada) { Notificacao.Alerta("Processe a cena antes de coletar."); return; }
                            if (evidencia.Coletar(TempoJogo.Agora()))
                            {
                                if (evidencia.PropVivo != null && evidencia.PropVivo.Exists())
                                {
                                    try { evidencia.PropVivo.Delete(); } catch { }
                                    evidencia.PropVivo = null;
                                }
                                _casoService.Salvar();
                                Notificacao.Sucesso($"Coletado: {evidencia.Titulo}");
                            }
                            RebuildEvidencias();
                        };
                        break;

                    case EstadoEvidencia.Coletada:
                        item.Activated += (s, e) =>
                        {
                            if (_laboratorioService.EnviarParaAnalise(evidencia)) RebuildEvidencias();
                        };
                        break;

                    case EstadoEvidencia.NoLab:
                    case EstadoEvidencia.Analisada:
                        item.Enabled = false;
                        break;
                }

                _menuEvidencias.Add(item);
            }

            VoltarCaso(_menuEvidencias);
        }

        private static string DescricaoEvidencia(Evidencia ev)
        {
            switch (ev.Estado)
            {
                case EstadoEvidencia.NaCena: return "Na cena. Selecione para COLETAR.";
                case EstadoEvidencia.Coletada: return "Coletada. Selecione para ENVIAR AO LAB.";
                case EstadoEvidencia.NoLab: return "No laboratorio. Aguardando laudo...";
                case EstadoEvidencia.Analisada: return $"LAUDO: {ev.ResultadoForense}{(ev.PossuiDna ? $"  DNA: {ev.PerfilDnaId}." : "")}";
                default: return ev.Descricao;
            }
        }

        private void RebuildDna()
        {
            _menuDna.Clear();
            if (_casoAtual == null) { VoltarCaso(_menuDna); return; }
            Caso caso = _casoAtual;

            foreach (PedDoCaso ped in caso.Peds)
            {
                PedDoCaso p = ped;
                NativeListItem<string> item = new NativeListItem<string>(p.Nome, DescricaoPed(caso, p), RolesNomes);
                item.SelectedIndex = IndiceRole(p.Role);
                item.Activated += (s, e) =>
                {
                    p.AlterarRole(RolesOrdem[item.SelectedIndex]);
                    _casoService.Salvar();
                    item.Description = DescricaoPed(caso, p);
                    Notificacao.Info($"{p.Nome}: {RolesNomes[item.SelectedIndex]}");
                };
                _menuDna.Add(item);
            }

            VoltarCaso(_menuDna);
        }

        private static string DescricaoPed(Caso caso, PedDoCaso ped)
        {
            string ident = ped.DataNascimento == DateTime.MinValue
                ? "Nao identificado."
                : $"{ped.DataNascimento:dd/MM/yyyy} | {ped.Genero}{(ped.Procurado ? " | PROCURADO" : "")}.";

            string dna = "";
            if (ped.PossuiDna)
                foreach (Evidencia ev in caso.Evidencias)
                    if (ev.Estado == EstadoEvidencia.Analisada && ev.PossuiDna && ev.PerfilDnaId == ped.PerfilDnaId)
                    { dna = "  [DNA CONFIRMADO em evidencia!]"; break; }

            return $"{RolesNomes[IndiceRole(ped.Role)]}. {ident}{dna}  (Enter = aplicar papel)";
        }

        private static int IndiceRole(RolePed role)
        {
            for (int i = 0; i < RolesOrdem.Length; i++) if (RolesOrdem[i] == role) return i;
            return 0;
        }

        private void RebuildCameras()
        {
            _menuCameras.Clear();
            if (_casoAtual == null) { VoltarCaso(_menuCameras); return; }

            if (_casoAtual.Cameras.Count == 0)
            {
                _menuCameras.Add(new NativeItem("Nenhuma camera registrada nesta area.") { Enabled = false });
                VoltarCaso(_menuCameras);
                return;
            }

            foreach (GravacaoCamera cam in _casoAtual.Cameras)
            {
                GravacaoCamera g = cam;
                string desc = g.Revisada
                    ? $"Revisada. Conteudo: {g.InfoRevelada}"
                    : "Selecione para acessar o conteudo da gravacao.";
                NativeItem item = new NativeItem(g.Local, desc);
                item.Activated += (s, e) =>
                {
                    // fix #12B: sem render ao vivo — entrega a informacao da filmagem direto.
                    if (!g.Revisada) { g.MarcarRevisada(); _casoService.Salvar(); }
                    Notificacao.Camera($"{g.Local}: {g.InfoRevelada}");
                    RebuildCameras();
                };
                _menuCameras.Add(item);
            }

            VoltarCaso(_menuCameras);
        }

        private void RebuildTelefone()
        {
            _menuTelefone.Clear();
            if (_casoAtual == null) { VoltarCaso(_menuTelefone); return; }

            foreach (PedDoCaso ped in _casoAtual.Peds)
            {
                PedDoCaso p = ped;
                string desc = p.MandadoEmitido
                    ? $"Mandado emitido. Tel: {p.RegistroTelefonico}"
                    : "Selecione para EMITIR MANDADO (revela telefone + rastreio no mapa).";
                NativeItem item = new NativeItem(p.Nome, desc);
                item.Activated += (s, e) => { _mandadoService.Emitir(p); RebuildTelefone(); };
                _menuTelefone.Add(item);
            }

            VoltarCaso(_menuTelefone);
        }

        private void RebuildStatus()
        {
            _menuStatus.Clear();
            if (_casoAtual == null) { VoltarCaso(_menuStatus); return; }
            Caso caso = _casoAtual;

            NativeListItem<string> item = new NativeListItem<string>(
                "Status do caso", $"Atual: {StatusTexto(caso.Status)}", StatusNomes);
            item.SelectedIndex = IndiceStatus(caso.Status);
            item.Activated += (s, e) =>
            {
                StatusCaso novo = StatusOrdem[item.SelectedIndex];
                if (_casoService.AtualizarStatus(caso.Id, novo))
                {
                    if (novo == StatusCaso.Resolvido || novo == StatusCaso.Arquivado)
                    {
                        _cenaService.RemoverCenaCompleta(caso);
                        // fix #3: remove blips de mandado de todos os peds do caso
                        foreach (PedDoCaso p in caso.Peds) _mandadoService.RemoverRastreamento(p.Id);
                    }
                    Notificacao.Info($"Caso {StatusTexto(novo)}.");
                }
                item.Description = $"Atual: {StatusTexto(caso.Status)}";
            };

            _menuStatus.Add(item);
            VoltarCaso(_menuStatus);
        }

        private void VoltarCaso(NativeMenu secao)
        {
            NativeItem v = new NativeItem("< Voltar");
            v.Activated += (s, e) => Navegar(secao, _menuCaso);
            secao.Add(v);
        }

        // ===== HELPERS =====

        private static int IndiceStatus(StatusCaso status)
        {
            for (int i = 0; i < StatusOrdem.Length; i++) if (StatusOrdem[i] == status) return i;
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