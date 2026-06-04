using System.Collections.Generic;

namespace InvestigacaoBR.Services
{
    public class PontoCena
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Heading { get; set; }

        public PontoCena(float x, float y, float z, float heading)
        {
            X = x; Y = y; Z = z; Heading = heading;
        }
    }

    public static class PoolsCaso
    {
        // ---------- Locais ----------
        public static readonly List<PontoCena> LocaisHomicidio = new List<PontoCena>
        {
            new PontoCena(-1487.6f, -668.2f, 28.2f,  120f),
            new PontoCena( 195.4f,  -930.5f, 30.6f,   70f),
            new PontoCena(-1037.8f,-1396.1f,  5.0f,  200f),
            new PontoCena(  94.3f, -1290.4f, 29.2f,  300f),
            new PontoCena(-247.9f, -1980.6f, 27.6f,   45f)
        };

        public static readonly List<PontoCena> LocaisTrafico = new List<PontoCena>
        {
            new PontoCena(-1166.2f,-1571.8f,  4.4f,  30f),
            new PontoCena(   82.6f,-1958.4f, 21.1f, 320f),
            new PontoCena( -628.9f,-1632.4f, 25.0f,  90f),
            new PontoCena( 1392.6f, 3604.7f, 38.9f, 200f)
        };

        public static readonly List<PontoCena> LocaisRouboCarga = new List<PontoCena>
        {
            new PontoCena( 1208.4f,-3115.6f,  5.5f,  90f),
            new PontoCena( -440.6f,-2789.3f,  6.0f, 150f),
            new PontoCena(  151.9f,-3209.5f,  5.9f, 270f),
            new PontoCena(  708.2f, -963.7f, 30.4f,   0f)
        };

        // ---------- Modelos de peds ----------
        public static readonly List<string> ModelosVitima = new List<string>
        {
            "a_m_y_business_01", "a_f_y_business_02", "a_m_m_business_01",
            "a_m_y_hipster_01",  "a_f_y_hipster_02",  "a_m_y_genstreet_01"
        };

        public static readonly List<string> ModelosSuspeito = new List<string>
        {
            "g_m_y_lost_01", "g_m_y_mexgoon_01", "g_m_y_ballasout_01",
            "a_m_y_stbla_01","a_m_m_eastsa_02",  "s_m_y_dealer_01"
        };

        public static readonly List<string> ModelosCivil = new List<string>
        {
            "a_m_y_skater_01", "a_f_y_runner_01",    "a_m_m_tourist_01",
            "a_f_m_business_02","a_m_y_vinewood_01", "a_f_y_soucent_01"
        };

        // ---------- Props de evidencia ----------
        public static readonly List<string> PropsArmaBranca = new List<string>
        {
            "prop_cs_knife", "w_me_knife_01", "prop_w_me_knife_01"
        };

        public static readonly List<string> PropsCapsula = new List<string>
        {
            "prop_ld_casing", "bkr_prop_weed_bag_01b"
        };

        public static readonly List<string> PropsItemPessoal = new List<string>
        {
            "prop_cs_ciggy_01", "prop_beer_bottle", "prop_plastic_cup_02",
            "prop_phone_ing",   "prop_cs_cuffs_01"
        };

        public static readonly List<string> PropsDroga = new List<string>
        {
            "prop_drug_package", "prop_meth_bag_01", "bkr_prop_coke_block_01b", "prop_weed_bottle"
        };

        public static readonly List<string> PropsDinheiro = new List<string>
        {
            "prop_anim_cash_pile_01", "prop_money_bag_01", "prop_cash_crate_01"
        };

        public static readonly List<string> PropsCarga = new List<string>
        {
            "prop_box_wood02a_pile", "prop_boxpile_07d", "ng_proc_box_01a", "prop_cs_cardbox_01"
        };

        public static readonly List<string> PropsFerramenta = new List<string>
        {
            "prop_tool_boltcutter", "prop_tool_crowbar", "prop_cs_tablet"
        };

        // ---------- fix #10: nomes americanos (match do LSPDFR/GTA V) ----------
        public static readonly List<string> NomesMasculinos = new List<string>
        {
            "James",   "Robert",  "Michael", "David",    "William",
            "Richard", "Joseph",  "Thomas",  "Charles",  "Christopher",
            "Daniel",  "Matthew", "Anthony", "Mark",     "Andrew",
            "Steven",  "Kevin",   "Jason",   "Ryan",     "Brian"
        };

        public static readonly List<string> NomesFemininos = new List<string>
        {
            "Patricia", "Jennifer", "Linda",    "Barbara", "Susan",
            "Jessica",  "Sarah",    "Karen",    "Lisa",    "Nancy",
            "Betty",    "Margaret", "Sandra",   "Ashley",  "Dorothy",
            "Kimberly", "Emily",    "Donna",    "Michelle","Carol"
        };

        public static readonly List<string> Sobrenomes = new List<string>
        {
            "Smith",    "Johnson",   "Williams", "Brown",    "Jones",
            "Garcia",   "Miller",    "Davis",    "Rodriguez","Martinez",
            "Hernandez","Lopez",     "Wilson",   "Anderson", "Taylor",
            "Thomas",   "Moore",     "Jackson",  "Martin",   "Lee"
        };
    }
}