using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Rage;
using InvestigacaoBR.Core;

namespace InvestigacaoBR.Data
{
    /// <summary>
    /// Um Ped envolvido no caso. Concentra:
    ///  1. Identidade/exibicao (Id, Nome, Descricao, Observacoes);
    ///  2. Dados pessoais (snapshot da Persona do LSPDFR: nascimento, genero, procurado);
    ///  3. Conhecimento do jogador (Role) — comeca Indefinido e muda na investigacao;
    ///  4. Verdade autorada (PerfilDnaId, EhCulpadoReal) — identifica o culpado;
    ///  5. Mundo/spawn (modelo, offset, heading, morto) e telefone/mandado.
    ///
    /// O snapshot da Persona e do modelo e necessario porque ambos so existem enquanto o ped
    /// existe; ao despawnar/recarregar somem. Guardamos os campos aqui (persistidos) para o
    /// registro do caso sobreviver. Quem le/grava a Persona e o PersonaService — este modelo
    /// nunca chama a API do LSPDFR direto.
    /// </summary>
    public class PedDoCaso
    {
        // ----- Identidade e exibicao -----
        public Guid Id { get; set; }

        /// <summary>Nome do Ped. Para peds do mundo, sincronizado da Persona do LSPDFR.</summary>
        public string Nome { get; set; }

        /// <summary>Descricao fisica/contextual base do Ped.</summary>
        public string Descricao { get; set; }

        /// <summary>Observacoes acumuladas durante a investigacao (de testemunhas, cameras, etc.).</summary>
        public List<string> Observacoes { get; set; }

        // ----- Dados pessoais (snapshot da Persona do LSPDFR) -----
        /// <summary>Data de nascimento (Persona.Birthday). DateTime.MinValue = nao identificado.</summary>
        public DateTime DataNascimento { get; set; }

        /// <summary>Genero como texto (traduzido do enum da Persona pelo PersonaService).</summary>
        public string Genero { get; set; }

        /// <summary>Se o Ped esta marcado como procurado no LSPDFR (Persona.Wanted).</summary>
        public bool Procurado { get; set; }

        // ----- Conhecimento do jogador -----
        /// <summary>Papel atual atribuido pelo jogador. Altere SEMPRE via AlterarRole().</summary>
        public RolePed Role { get; set; }

        // ----- Verdade autorada -----
        /// <summary>Id do perfil de DNA deste Ped (ex.: "DNA-001"). Cruzado com as evidencias.</summary>
        public string PerfilDnaId { get; set; }

        /// <summary>VERDADE: este Ped e o real culpado. Usado para validar a resolucao do caso.</summary>
        public bool EhCulpadoReal { get; set; }

        // ----- Mundo / spawn -----
        /// <summary>
        /// Modelo do Ped. Duplo proposito: modelo autorado para spawnar peds da cena, OU
        /// snapshot do modelo real de um ped do mundo identificado (ped.Model.Name).
        /// Vazio = sem modelo definido.
        /// </summary>
        public string ModeloPed { get; set; }

        /// <summary>Offset X em relacao a origem da cena do crime.</summary>
        public float OffsetX { get; set; }

        /// <summary>Offset Y em relacao a origem da cena do crime.</summary>
        public float OffsetY { get; set; }

        /// <summary>Offset Z em relacao a origem da cena do crime.</summary>
        public float OffsetZ { get; set; }

        /// <summary>Direcao (graus) que o Ped encara ao spawnar.</summary>
        public float Heading { get; set; }

        /// <summary>Se true, o Ped e spawnado morto (ex.: a vitima na cena).</summary>
        public bool SpawnarMorto { get; set; }

        /// <summary>
        /// Quando true, este ped e registrado no caso mas NAO spawnado fisicamente na cena.
        /// Usado para culpados que ja fugiram (sequestro, latrocinio) — existem via mandado.
        /// </summary>
        [XmlIgnore]
        public bool NaoSpawnarNaCena { get; set; } = false;

        // ----- Telefone / mandado (autorado) -----
        /// <summary>Registro telefonico autorado. Revelado apos o mandado.</summary>
        public string RegistroTelefonico { get; set; }

        /// <summary>Se o mandado ja foi emitido. Libera o registro telefonico e o rastreamento.</summary>
        public bool MandadoEmitido { get; set; }

        /// <summary>Localizacao conhecida X (blip de rastreamento liberado pelo mandado).</summary>
        public float LocalConhecidoX { get; set; }

        /// <summary>Localizacao conhecida Y.</summary>
        public float LocalConhecidoY { get; set; }

        /// <summary>Localizacao conhecida Z.</summary>
        public float LocalConhecidoZ { get; set; }

        // ----- Vinculo de sessao (NAO persistido) -----
        /// <summary>Ped vivo no mundo na sessao atual. Usado no spawn da cena e na limpeza do END.</summary>
        [XmlIgnore]
        public Ped PedVivo { get; set; }

        /// <summary>True se o Ped vivo existe e e valido agora (Exists() e a checagem segura do RPH).</summary>
        [XmlIgnore]
        public bool EstaSpawnado => PedVivo != null && PedVivo.Exists();

        /// <summary>True se este Ped tem DNA autorado.</summary>
        [XmlIgnore]
        public bool PossuiDna => !string.IsNullOrEmpty(PerfilDnaId);

        // ----- Construtores -----
        /// <summary>Construtor sem parametros: OBRIGATORIO para serializacao XML. Nao remover.</summary>
        public PedDoCaso()
        {
            Id = Guid.NewGuid();
            Nome = string.Empty;
            Descricao = string.Empty;
            Observacoes = new List<string>();
            DataNascimento = DateTime.MinValue;
            Genero = string.Empty;
            Procurado = false;
            Role = RolePed.Indefinido;
            PerfilDnaId = string.Empty;
            EhCulpadoReal = false;
            ModeloPed = string.Empty;
            SpawnarMorto = false;
            RegistroTelefonico = string.Empty;
            MandadoEmitido = false;
        }

        /// <summary>Conveniencia: cria um Ped do caso ja com nome, descricao e papel.</summary>
        public PedDoCaso(string nome, string descricao, RolePed role) : this()
        {
            Nome = string.IsNullOrEmpty(nome) ? string.Empty : nome;
            Descricao = string.IsNullOrEmpty(descricao) ? string.Empty : descricao;
            Role = role;
        }

        // ----- Mutacoes com log centralizado -----

        /// <summary>Altera o papel do Ped e registra a transicao no log.</summary>
        public void AlterarRole(RolePed novoRole)
        {
            if (novoRole == Role)
            {
                Logger.Info($"AlterarRole ignorado para '{Nome}': papel ja e {Role}.");
                return;
            }

            RolePed anterior = Role;
            Role = novoRole;
            Logger.State($"Role do Ped '{Nome}'", anterior.ToString(), novoRole.ToString());
        }

        /// <summary>
        /// Emite o mandado: libera o registro telefonico e o rastreamento na UI. Idempotente.
        /// </summary>
        public bool EmitirMandado()
        {
            if (MandadoEmitido)
            {
                Logger.Info($"EmitirMandado ignorado para '{Nome}': mandado ja emitido.");
                return false;
            }

            MandadoEmitido = true;
            Logger.State($"Mandado do Ped '{Nome}'", "Nao emitido", "Emitido");
            return true;
        }

        /// <summary>
        /// Registra a identificacao do Ped: snapshot da Persona (nome, nascimento, genero,
        /// procurado) e do modelo. Chamado pelo PersonaService ao identificar um ped do mundo.
        /// </summary>
        public void RegistrarIdentificacao(string nome, DateTime nascimento, string genero, bool procurado, string modelo)
        {
            Nome = string.IsNullOrEmpty(nome) ? Nome : nome;
            DataNascimento = nascimento;
            Genero = string.IsNullOrEmpty(genero) ? string.Empty : genero;
            Procurado = procurado;
            if (!string.IsNullOrEmpty(modelo))
            {
                ModeloPed = modelo;
            }

            Logger.Info($"Ped identificado: '{Nome}' | nasc. {DataNascimento:dd/MM/yyyy} | {Genero} | procurado={Procurado} | modelo='{ModeloPed}'.");
        }

        /// <summary>Adiciona uma observacao a investigacao do Ped e loga.</summary>
        public void AdicionarObservacao(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                Logger.Warn($"AdicionarObservacao ignorada para '{Nome}': texto vazio.");
                return;
            }

            Observacoes.Add(texto);
            Logger.Info($"Observacao adicionada ao Ped '{Nome}'. Total: {Observacoes.Count}.");
        }
    }
}