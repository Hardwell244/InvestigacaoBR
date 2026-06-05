using System;
using System.Collections.Generic;
using InvestigacaoBR.Core;
using InvestigacaoBR.Data;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// 5C: Gera eventos vivos que ocorrem enquanto o jogador esta em servico.
    /// Tres tipos: dica de informante, testemunha reaparece, suspeito mudou localizacao.
    /// Chamado do MainLoop via Tick() — controla o proprio timer interno.
    /// </summary>
    public class EventoVivoService
    {
        private readonly CasoService _casoService;

        private int _ticks;
        private int _proximoEvento;

        // Evita repetir o mesmo tipo de evento no mesmo caso na mesma sessao
        private readonly HashSet<string> _eventosDisparados = new HashSet<string>();

        private static readonly string[] DicasInformante =
        {
            "Fonte anonima: visto no bairro horas antes do incidente.",
            "Informante: o suspeito tem contatos conhecidos na area.",
            "Dica recebida: movimentacao suspeita no local dias antes.",
            "Fonte confidencial: pessoa de interesse foi vista fuguindo da regiao.",
            "Informante: ha relatos de atividade semelhante em casos anteriores."
        };

        private static readonly string[] DicasTestemunha =
        {
            "lembrou de um detalhe que omitiu por medo.",
            "entrou em contato com nova informacao sobre o veiculo envolvido.",
            "reconheceu o suspeito em foto de outro registro.",
            "forneceu horario mais preciso do que o depoimento inicial.",
            "relatou ter visto um segundo individuo que nao mencionou antes."
        };

        public EventoVivoService(CasoService casoService)
        {
            _casoService = casoService;
            _proximoEvento = Aleatorio.Inteiro(10800, 21600); // 3-6 min a 60fps
        }

        // ===== TICK (chamado do MainLoop do EntryPoint) =====

        public void Tick()
        {
            _ticks++;
            if (_ticks < _proximoEvento) return;

            _ticks = 0;
            _proximoEvento = Aleatorio.Inteiro(10800, 21600); // reseta timer

            TentarDispararEvento();
        }

        // ===== LOGICA =====

        private void TentarDispararEvento()
        {
            List<Caso> ativos = new List<Caso>();
            foreach (Caso c in _casoService.ObterDoDetetive())
                if (c.Status == StatusCaso.Aberto) ativos.Add(c);

            if (ativos.Count == 0) return;

            Caso caso = Aleatorio.Item(ativos);
            int tipo = Aleatorio.Inteiro(0, 2);

            switch (tipo)
            {
                case 0: DispararDicaInformante(caso); break;
                case 1: DispararTestemunhaReaparece(caso); break;
                case 2: DispararSuspeitoMudou(caso); break;
            }
        }

        private void DispararDicaInformante(Caso caso)
        {
            string chave = $"informante_{caso.Id}";
            if (_eventosDisparados.Contains(chave)) return;
            _eventosDisparados.Add(chave);

            string dica = Aleatorio.Item(DicasInformante);
            string msg = $"Sobre '{caso.Titulo}': {dica}";

            Notificacao.Mandado($"Informante: {msg}");
            TimelineService.Registrar(caso.Id, $"[Informante] {msg}", "INFORMANTE");
            _casoService.Salvar();

            Logger.Info($"EventoVivo DicaInformante: '{caso.Titulo}'.");
        }

        private void DispararTestemunhaReaparece(Caso caso)
        {
            // Precisa ter pelo menos uma testemunha identificada
            PedDoCaso testemunha = null;
            foreach (PedDoCaso p in caso.Peds)
                if (p.Role == RolePed.Testemunha && p.DataNascimento != DateTime.MinValue)
                { testemunha = p; break; }

            if (testemunha == null) return;

            string chave = $"testemunha_{testemunha.Id}";
            if (_eventosDisparados.Contains(chave)) return;
            _eventosDisparados.Add(chave);

            string detalhe = Aleatorio.Item(DicasTestemunha);
            string msg = $"{testemunha.Nome} {detalhe}";

            Notificacao.Info($"Nova informacao — {msg}");
            TimelineService.Registrar(caso.Id, $"[Testemunha reaparece] {msg}", "INFORMANTE");
            _casoService.Salvar();

            Logger.Info($"EventoVivo TestemunhaReaparece: '{testemunha.Nome}' no caso '{caso.Titulo}'.");
        }

        private void DispararSuspeitoMudou(Caso caso)
        {
            // So dispara se ainda nao emitiu mandado pro culpado
            PedDoCaso culpado = null;
            foreach (PedDoCaso p in caso.Peds)
                if (p.EhCulpadoReal && !p.MandadoEmitido)
                { culpado = p; break; }

            if (culpado == null) return;

            string chave = $"mudou_{culpado.Id}";
            if (_eventosDisparados.Contains(chave)) return;
            _eventosDisparados.Add(chave);

            // Atualiza localizacao conhecida com offset aleatorio
            culpado.LocalConhecidoX += Aleatorio.Real(-50f, 50f);
            culpado.LocalConhecidoY += Aleatorio.Real(-50f, 50f);

            string msg = $"Inteligencia indica que {culpado.Nome} pode ter mudado de local. Rastreamento atualizado.";
            Notificacao.Alerta(msg);
            TimelineService.Registrar(caso.Id, msg, "INFORMANTE");
            _casoService.Salvar();

            Logger.Info($"EventoVivo SuspeitoMudou: '{culpado.Nome}' no caso '{caso.Titulo}'.");
        }
    }
}