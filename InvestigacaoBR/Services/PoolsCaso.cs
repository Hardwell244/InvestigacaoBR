using System.Collections.Generic;

namespace InvestigacaoBR.Services
{
    /// <summary>Um ponto de cena: posicao no mundo + heading. Usado para sortear o local do caso.</summary>
    public class PontoCena
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Heading { get; set; }

        public PontoCena(float x, float y, float z, float heading)
        {
            X = x;
            Y = y;
            Z = z;
            Heading = heading;
        }
    }

    /// <summary>
    /// Banco de ingredientes autorados de onde os geradores sorteiam local, peds, armas e provas.
    ///
    /// ATENCAO: coordenadas e alguns modelos/props sao AMOSTRAS de partida — valide/expanda
    /// in-game. Modelos invalidos sao logados e pulados pelo CenaService (nao quebram o jogo).
    /// </summary>
    public static class PoolsCaso
    {
        // ---------- Locais por tipo de caso (ajuste as coords in-game) ----------
        public static readonly List<PontoCena> LocaisHomicidio = new List<PontoCena>
        {
            new PontoCena(-1487.6f, -668.2f, 28.2f, 120f),  // beco centro
            new PontoCena(195.4f, -930.5f, 30.6f, 70f),     // estacionamento
            new PontoCena(-1037.8f, -1396.1f, 5.0f, 200f),  // orla
            new PontoCena(94.3f, -1290.4f, 29.2f, 300f),    // viela strip club
            new PontoCena(-247.9f, -1980.6f, 27.6f, 45f)    // calcada subuario
        };

        public static readonly List<PontoCena> LocaisTrafico = new List<PontoCena>
        {
            new PontoCena(-1166.2f, -1571.8f, 4.4f, 30f),   // praia esquina
            new PontoCena(82.6f, -1958.4f, 21.1f, 320f),    // grove st
            new PontoCena(-628.9f, -1632.4f, 25.0f, 90f),   // galpao
            new PontoCena(1392.6f, 3604.7f, 38.9f, 200f)    // sandy shores
        };

        public static readonly List<PontoCena> LocaisRouboCarga = new List<PontoCena>
        {
            new PontoCena(1208.4f, -3115.6f, 5.5f, 90f),    // porto / docas
            new PontoCena(-440.6f, -2789.3f, 6.0f, 150f),   // terminal contêiner
            new PontoCena(151.9f, -3209.5f, 5.9f, 270f),    // armazem porto
            new PontoCena(708.2f, -963.7f, 30.4f, 0f)       // deposito industrial
        };

        // ---------- Modelos de peds ----------
        public static readonly List<string> ModelosVitima = new List<string>
        {
            "a_m_y_business_01", "a_f_y_business_02", "a_m_m_business_01",
            "a_m_y_hipster_01", "a_f_y_hipster_02", "a_m_y_genstreet_01"
        };

        public static readonly List<string> ModelosSuspeito = new List<string>
        {
            "g_m_y_lost_01", "g_m_y_mexgoon_01", "g_m_y_ballasout_01",
            "a_m_y_stbla_01", "a_m_m_eastsa_02", "s_m_y_dealer_01"
        };

        public static readonly List<string> ModelosCivil = new List<string>
        {
            "a_m_y_skater_01", "a_f_y_runner_01", "a_m_m_tourist_01",
            "a_f_m_business_02", "a_m_y_vinewood_01", "a_f_y_soucent_01"
        };

        // ---------- Armas / provas do homicidio ----------
        public static readonly List<string> PropsArmaBranca = new List<string>
        {
            "prop_cs_knife", "w_me_knife_01", "prop_w_me_knife_01"
        };

        public static readonly List<string> PropsCapsula = new List<string>
        {
            "prop_ld_casing", "bkr_prop_weed_bag_01b" // AMOSTRA: confirme um modelo de capsula valido
        };

        // ---------- Provas gerais (fonte de DNA, etc.) ----------
        public static readonly List<string> PropsItemPessoal = new List<string>
        {
            "prop_cs_ciggy_01", "prop_beer_bottle", "prop_plastic_cup_02",
            "prop_phone_ing", "prop_cs_cuffs_01"
        };

        // ---------- Provas de trafico ----------
        public static readonly List<string> PropsDroga = new List<string>
        {
            "prop_drug_package", "prop_meth_bag_01", "bkr_prop_coke_block_01b",
            "prop_weed_bottle"
        };

        public static readonly List<string> PropsDinheiro = new List<string>
        {
            "prop_anim_cash_pile_01", "prop_money_bag_01", "prop_cash_crate_01"
        };

        // ---------- Provas de roubo de carga ----------
        public static readonly List<string> PropsCarga = new List<string>
        {
            "prop_box_wood02a_pile", "prop_boxpile_07d", "ng_proc_box_01a",
            "prop_cs_cardbox_01"
        };

        public static readonly List<string> PropsFerramenta = new List<string>
        {
            "prop_tool_boltcutter", "prop_tool_crowbar", "prop_cs_tablet"
        };

        // ---------- Nomes (BR) para a identidade autorada dos peds ----------
        public static readonly List<string> NomesMasculinos = new List<string>
        {
            "Joao", "Carlos", "Pedro", "Lucas", "Rafael", "Bruno", "Felipe", "Marcos", "Diego", "Andre"
        };

        public static readonly List<string> NomesFemininos = new List<string>
        {
            "Maria", "Ana", "Juliana", "Camila", "Fernanda", "Patricia", "Beatriz", "Larissa", "Aline", "Carla"
        };

        public static readonly List<string> Sobrenomes = new List<string>
        {
            "Silva", "Souza", "Oliveira", "Santos", "Pereira", "Costa", "Almeida", "Ferreira", "Rodrigues", "Gomes"
        };
    }
}