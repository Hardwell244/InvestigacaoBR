using InvestigacaoBR.Core;
using InvestigacaoBR.Data;
using InvestigacaoBR.Services;
using LemonUI;
using LemonUI.Menus;
using System;
using System.Collections.Generic;

namespace InvestigacaoBR.UI
{
    public class MenuDetetive
    {
        private static readonly RolePed[] RolesOrdem = { RolePed.Indefinido, RolePed.Testemunha, RolePed.PessoaDeInteresse, RolePed.Inocente, RolePed.Culpado };
        private static readonly string[] RolesNomes = { "Indefinido", "Testemunha", "Pessoa de Interesse", "Inocente", "Culpado" };
        private static readonly StatusCaso[] StatusOrdem = { StatusCaso.Aberto, StatusCaso.Arquivado, StatusCaso.Resolvido };
        private static readonly string[] StatusNomes = { "Aberto", "Arquivado", "Resolvido" };

        // Servicos
        private readonly CasoService _casoService;
        private readonly CenaService _cenaService;
        private readonly LaboratorioService _laboratorioService;
        private readonly CameraService _cameraService;
        private readonly MandadoService _mandadoService;
        private readonly GeradorCasos _geradorCasos;
        private readonly DetectiveService _detectiveService;
        private readonly PartnerService _partnerService;  // <<< CAMPO QUE ESTAVA FALTANDO

        // Menus investigacao
        private readonly NativeMenu _menuPrincipal;
        private readonly NativeMenu _menuAbertos;
        private readonly NativeMenu _menuHistorico;
        private readonly NativeMenu _menuCaso;
        private readonly NativeMenu _menuCena;
        private readonly NativeMenu _menuEvidencias;
        private readonly NativeMenu _menuDna;
        private readonly NativeMenu _menuCameras;
        private readonly NativeMenu _menuTelefone;
        private readonly NativeMenu _menuStatus;
        private readonly NativeMenu _menuPerfil;
        private readonly NativeMenu _menuDiario;

        private Caso _casoAtual;
        private readonly HashSet<Guid> _dnaMatchesNotificados = new HashSet<Guid>();

        public MenuDetetive(ObjectPool pool,
            CasoService casoService, CenaService cenaService,
            LaboratorioService laboratorioService, CameraService cameraService,
            MandadoService mandadoService, GeradorCasos geradorCasos,
            DetectiveService detectiveService, PartnerService partnerService)
        {
            _casoService = casoService;
            _cenaService = cenaService;
            _laboratorioService = laboratorioService;
            _cameraService = cameraService;
            _mandadoService = mandadoService;
            _geradorCasos = geradorCasos;
            _detectiveService = detectiveService;
            _partnerService = partnerService;

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
            _menuPerfil = new NativeMenu("CARREIRA", "Meu Perfil");
            _menuDiario = new NativeMenu("DETETIVE", "Diario do Caso");

            foreach (NativeMenu m in new[] {
                _menuPrincipal, _menuAbertos, _menuHistorico, _menuCaso,
                _menuCena, _menuEvidencias, _menuDna, _menuCameras, _menuTelefone, _menuStatus,
                _menuPerfil, _menuDiario })
                pool.Add(m);

            _menuPrincipal.Shown += (s, e) => RebuildPrincipal();
            _menuAbertos.Shown += (s, e) => RebuildAbertos();
            _menuHistorico.Shown += (s, e) => RebuildHistorico();
            _menuCena.Shown += (s, e) => RebuildCena();
            _menuEvidencias.Shown += (s, e) => RebuildEvidencias();
            _menuDna.Shown += (s, e) => { RebuildDna(); VerificarMatchesDNA(); };
            _menuCameras.Shown += (s, e) => RebuildCameras();
            _menuTelefone.Shown += (s, e) => RebuildTelefone();
            _menuStatus.Shown += (s, e) => RebuildStatus();
            _menuPerfil.Shown += (s, e) => RebuildPerfil();
            _menuDiario.Shown += (s, e) => RebuildDiario();

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
            foreach (NativeMenu m in new[] {
                _menuPrincipal, _menuAbertos, _menuHistorico, _menuCaso,
                _menuCena, _menuEvidencias, _menuDna, _menuCameras, _menuTelefone, _menuStatus,
                _menuPerfil, _menuDiario })
                m.Visible = false;
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

            // Perfil
            string patente = _detectiveService?.Perfil != null
                ? DetectiveService.NomePatente(_detectiveService.Perfil.Patente) : "Agente";
            NativeItem iPerfil = new NativeItem($"Meu Perfil  [{patente}]",
                "XP, reputacao, integridade e estatisticas da carreira.");
            iPerfil.Activated += (s, e) => Navegar(_menuPrincipal, _menuPerfil);

            // Parceiro (5B)
            string[] nomesParceiros = { "Det. Miller (Correto)", "Det. Torres (Neutro)", "Det. Johnson (Corrupto)" };
            NativeListItem<string> iParceiro = new NativeListItem<string>(
                $"Parceiro: {(_partnerService?.NomeParceiro ?? "—")}",
                "Parceiro de investigacao. Personalidade afeta comentarios e propostas.",
                nomesParceiros);
            iParceiro.SelectedIndex = _partnerService?.IndiceAtual ?? 0;
            iParceiro.Activated += (s, e) =>
                _partnerService?.SelecionarParceiro(iParceiro.SelectedIndex, _detectiveService);

            // Casos
            int qtdAbertos = 0, qtdHistorico = 0;
            foreach (Caso c in _casoService.ObterDoDetetive())
            {
                if (c.Status == StatusCaso.Aberto) qtdAbertos++;
                else qtdHistorico++;
            }

            NativeItem iAbertos = new NativeItem(
                $"Investigacoes em Andamento  [{qtdAbertos}]",
                qtdAbertos > 0 ? "Acessar casos abertos." : "Nenhum caso aberto. Use F6.");
            iAbertos.Enabled = qtdAbertos > 0;
            iAbertos.Activated += (s, e) => Navegar(_menuPrincipal, _menuAbertos);

            NativeItem iHistorico = new NativeItem(
                $"Historico  [{qtdHistorico}]",
                qtdHistorico > 0 ? "Casos resolvidos e arquivados." : "Nenhum caso encerrado.");
            iHistorico.Enabled = qtdHistorico > 0;
            iHistorico.Activated += (s, e) => Navegar(_menuPrincipal, _menuHistorico);

            NativeItem iGerar = new NativeItem("Gerar Nova Investigacao",
                "Reabastece o pool com casos disponiveis no F6.");
            iGerar.Activated += (s, e) =>
            {
                _geradorCasos.GarantirPool();
                int disp = 0;
                foreach (Caso c in _casoService.ObterDisponiveis()) disp++;
                Notificacao.Sucesso($"Pool atualizado. {disp} caso(s) disponivel(is) no F6.");
                RebuildPrincipal();
            };

            NativeItem iLimpar = new NativeItem("Limpar Cenas Ativas",
                "Remove visuais de todas as cenas. Dados preservados.");
            iLimpar.Activated += (s, e) =>
            {
                int limpas = 0;
                foreach (Caso caso in _casoService.ObterDoDetetive())
                    if (_cenaService.CenaMontada(caso.Id)) { _cenaService.LimparCena(caso); limpas++; }
                Notificacao.Aviso(limpas > 0 ? $"{limpas} cena(s) limpa(s)." : "Nenhuma cena ativa.");
            };

            _menuPrincipal.Add(iPerfil);
            _menuPrincipal.Add(iParceiro);
            _menuPrincipal.Add(new NativeItem("--- Investigacoes ---") { Enabled = false });
            _menuPrincipal.Add(iAbertos);
            _menuPrincipal.Add(iHistorico);
            _menuPrincipal.Add(new NativeItem("--- Ferramentas ---") { Enabled = false });
            _menuPrincipal.Add(iGerar);
            _menuPrincipal.Add(iLimpar);
        }

        // ===== PERFIL DO DETETIVE =====

        private void RebuildPerfil()
        {
            _menuPerfil.Clear();

            if (_detectiveService?.Perfil == null)
            {
                _menuPerfil.Add(new NativeItem("Perfil nao carregado.") { Enabled = false });
                AdicionarVoltar(_menuPerfil, _menuPrincipal);
                return;
            }

            DetectiveProfile p = _detectiveService.Perfil;

            _menuPerfil.Add(new NativeItem(p.Nome,
                $"Matricula #{p.Matricula}  |  {DetectiveService.NomePatente(p.Patente)}")
            { Enabled = false });

            _menuPerfil.Add(new NativeItem("Experiencia",
                p.PatenteMaxima ? $"XP: {p.XP} — patente maxima." : _detectiveService.ResumoXP())
            { Enabled = false });

            string repCor = p.Reputacao >= 70 ? "~g~" : p.Reputacao >= 40 ? "~y~" : "~r~";
            _menuPerfil.Add(new NativeItem("Reputacao",
                $"{repCor}{p.Reputacao}/100~s~  {_detectiveService.BarraReputacao()}")
            { Enabled = false });

            string intCor = p.Integridade >= 70 ? "~b~" : p.Integridade >= 40 ? "~y~" : "~r~";
            _menuPerfil.Add(new NativeItem("Integridade",
                $"{intCor}{p.Integridade}/100~s~  {_detectiveService.BarraIntegridade()}")
            { Enabled = false });

            if (p.DinheiroPropinas > 0)
                _menuPerfil.Add(new NativeItem("Propinas Aceitas",
                    $"~r~${p.DinheiroPropinas}k~w~  |  {p.PropinaRecusadas} recusadas.")
                { Enabled = false });

            _menuPerfil.Add(new NativeItem("--- Estatisticas ---") { Enabled = false });
            _menuPerfil.Add(new NativeItem("Casos Resolvidos", p.CasosResolvidos.ToString()) { Enabled = false });
            _menuPerfil.Add(new NativeItem("Casos Arquivados", p.CasosArquivados.ToString()) { Enabled = false });
            _menuPerfil.Add(new NativeItem("Prisoes Corretas", p.PrisoesCertas.ToString()) { Enabled = false });
            _menuPerfil.Add(new NativeItem("Mandados Emitidos", p.MandadosEmitidos.ToString()) { Enabled = false });
            _menuPerfil.Add(new NativeItem("Evidencias Coletadas", p.EvidenciasColetadas.ToString()) { Enabled = false });

            AdicionarVoltar(_menuPerfil, _menuPrincipal);
        }

        // ===== DIARIO DO CASO =====

        private void RebuildDiario()
        {
            _menuDiario.Clear();
            if (_casoAtual == null || _casoAtual.Timeline == null || _casoAtual.Timeline.Count == 0)
            {
                _menuDiario.Add(new NativeItem("Nenhum registro ainda.", "Acoes na cena aparecerao aqui.") { Enabled = false });
                VoltarCaso(_menuDiario);
                return;
            }

            List<TimelineEntry> tl = _casoAtual.Timeline;
            int inicio = Math.Max(0, tl.Count - 20);
            for (int i = tl.Count - 1; i >= inicio; i--)
            {
                TimelineEntry e = tl[i];
                _menuDiario.Add(new NativeItem(
                    $"[{e.Autor,-8}] {e.DataHora:HH:mm}", e.Texto)
                { Enabled = false });
            }
            if (tl.Count > 20)
                _menuDiario.Add(new NativeItem($"... e mais {tl.Count - 20} registros anteriores.") { Enabled = false });

            VoltarCaso(_menuDiario);
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
            NativeItem iEvid = new NativeItem("Evidencias e Lab", "Coletar e enviar ao laboratorio.");
            NativeItem iDna = new NativeItem("DNA e Suspeitos", "Cruzar DNA e classificar os peds.");
            NativeItem iCam = new NativeItem("Cameras", "Revisar gravacoes da area.");
            NativeItem iTel = new NativeItem("Telefone e Mandados", "Emitir mandados e rastrear.");
            NativeItem iStatus = new NativeItem("Status do Caso", "Atualizar o status da investigacao.");
            NativeItem iDiario = new NativeItem("Diario da Investigacao", "Historico cronologico de todas as acoes.");

            iCena.Activated += (s, e) => Navegar(_menuCaso, _menuCena);
            iEvid.Activated += (s, e) => Navegar(_menuCaso, _menuEvidencias);
            iDna.Activated += (s, e) => Navegar(_menuCaso, _menuDna);
            iCam.Activated += (s, e) => Navegar(_menuCaso, _menuCameras);
            iTel.Activated += (s, e) => Navegar(_menuCaso, _menuTelefone);
            iStatus.Activated += (s, e) => Navegar(_menuCaso, _menuStatus);
            iDiario.Activated += (s, e) => Navegar(_menuCaso, _menuDiario);

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
            _menuCaso.Add(iDiario);
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
                if (caso.AssumirJurisdicao())
                {
                    _casoService.Salvar();
                    TimelineService.Registrar(caso.Id, "Detetive assumiu a jurisdicao da cena.", "DETETIVE");
                    Notificacao.Info("Jurisdicao assumida.");
                }
                RebuildCena();
            };

            NativeItem iIsol = new NativeItem("Isolar area",
                !caso.JurisdicaoAssumida ? "Assuma a jurisdicao primeiro." :
                caso.CenaIsolada ? "Ja isolada." : "Cerca a cena com fita.")
            { Enabled = caso.JurisdicaoAssumida && !caso.CenaIsolada };
            iIsol.Activated += (s, e) =>
            {
                if (caso.IsolarCena())
                {
                    _cenaService.SpawnarFitaIsolamento(caso);
                    _casoService.Salvar();
                    TimelineService.Registrar(caso.Id, "Area isolada com fita de perimetro.", "DETETIVE");
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
                if (caso.ProcessarCena())
                {
                    _casoService.Salvar();
                    TimelineService.Registrar(caso.Id, "Cena processada. Coleta liberada.", "DETETIVE");
                    Notificacao.Sucesso("Cena processada. Colete as evidencias.");
                }
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
                                { try { evidencia.PropVivo.Delete(); } catch { } evidencia.PropVivo = null; }
                                _casoService.Salvar();
                                TimelineService.Registrar(caso.Id, $"Evidencia coletada: '{evidencia.Titulo}'.", "DETETIVE");
                                _detectiveService?.RegistrarEvidenciaColetada();
                                _partnerService?.ComentarEvidenciaEncontrada(caso.Id);
                                Notificacao.Sucesso($"Coletado: {evidencia.Titulo}");
                            }
                            RebuildEvidencias();
                        };
                        break;

                    case EstadoEvidencia.Coletada:
                        item.Activated += (s, e) =>
                        {
                            if (_laboratorioService.EnviarParaAnalise(evidencia))
                            {
                                TimelineService.Registrar(caso.Id, $"'{evidencia.Titulo}' enviada ao laboratorio.", "DETETIVE");
                                RebuildEvidencias();
                            }
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
                    TimelineService.Registrar(caso.Id, $"{p.Nome} classificado como {RolesNomes[item.SelectedIndex]}.", "DETETIVE");
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
                    { dna = "  [DNA CONFIRMADO]"; break; }
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
                NativeItem item = new NativeItem(g.Local,
                    g.Revisada ? $"Revisada: {g.InfoRevelada}" : "Selecione para acessar o conteudo.");
                item.Activated += (s, e) =>
                {
                    if (!g.Revisada)
                    {
                        g.MarcarRevisada(); _casoService.Salvar();
                        TimelineService.Registrar(_casoAtual.Id, $"Camera '{g.Local}': {g.InfoRevelada}", "DETETIVE");
                    }
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
                NativeItem item = new NativeItem(p.Nome,
                    p.MandadoEmitido ? $"Mandado emitido. Tel: {p.RegistroTelefonico}" : "Selecione para EMITIR MANDADO.");
                item.Activated += (s, e) =>
                {
                    if (_mandadoService.Emitir(p))
                    {
                        TimelineService.Registrar(_casoAtual.Id, $"Mandado emitido para {p.Nome}.", "MANDADO");
                        _detectiveService?.RegistrarMandadoEmitido();
                    }
                    RebuildTelefone();
                };
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
                        foreach (PedDoCaso p in caso.Peds) _mandadoService.RemoverRastreamento(p.Id);

                        if (novo == StatusCaso.Resolvido)
                        {
                            int xp = _detectiveService?.RegistrarResolucao(caso) ?? 0;
                            TimelineService.Registrar(caso.Id, $"Caso RESOLVIDO.{(xp > 0 ? $" +{xp} XP." : "")}", "SISTEMA");
                            _partnerService?.ComentarResolucao(caso);
                        }
                        else
                        {
                            _detectiveService?.RegistrarArquivamento();
                            TimelineService.Registrar(caso.Id, "Caso arquivado. -5 reputacao.", "SISTEMA");
                        }
                        _casoService.Salvar();
                    }
                    Notificacao.Info($"Caso {StatusTexto(caso.Status)}.");
                }
                item.Description = $"Atual: {StatusTexto(caso.Status)}";
            };

            _menuStatus.Add(item);
            VoltarCaso(_menuStatus);
        }

        private void VerificarMatchesDNA()
        {
            if (_casoAtual == null) return;
            foreach (PedDoCaso ped in _casoAtual.Peds)
            {
                if (!ped.PossuiDna || _dnaMatchesNotificados.Contains(ped.Id)) continue;
                foreach (Evidencia ev in _casoAtual.Evidencias)
                {
                    if (ev.Estado != EstadoEvidencia.Analisada || !ev.PossuiDna || ev.PerfilDnaId != ped.PerfilDnaId) continue;
                    Notificacao.Lab($"DNA de ~b~{ped.Nome}~w~ bate com '{ev.Titulo}'! Classifique como ~r~Culpado~w~.");
                    TimelineService.Registrar(_casoAtual.Id, $"DNA de {ped.Nome} confirmado em '{ev.Titulo}'.", "LAB");
                    _dnaMatchesNotificados.Add(ped.Id);
                    break;
                }
            }
        }

        private void VoltarCaso(NativeMenu secao)
        {
            NativeItem v = new NativeItem("< Voltar");
            v.Activated += (s, e) => Navegar(secao, _menuCaso);
            secao.Add(v);
        }

        private static int IndiceStatus(StatusCaso s)
        {
            for (int i = 0; i < StatusOrdem.Length; i++) if (StatusOrdem[i] == s) return i;
            return 0;
        }

        private static string StatusTexto(StatusCaso s)
        {
            switch (s)
            {
                case StatusCaso.Disponivel: return "Disponivel";
                case StatusCaso.Aberto: return "Aberto";
                case StatusCaso.Arquivado: return "Arquivado";
                case StatusCaso.Resolvido: return "Resolvido";
                default: return s.ToString();
            }
        }
    }
}