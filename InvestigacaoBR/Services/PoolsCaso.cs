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
        { X = x; Y = y; Z = z; Heading = heading; }
    }

    public static class PoolsCaso
    {
        // ===== LOCAIS =====

        public static readonly List<PontoCena> LocaisHomicidio = new List<PontoCena>
        {
            new PontoCena(-1487.6f, -668.2f,  28.2f, 120f),
            new PontoCena(  195.4f, -930.5f,  30.6f,  70f),
            new PontoCena(-1037.8f,-1396.1f,   5.0f, 200f),
            new PontoCena(   94.3f,-1290.4f,  29.2f, 300f),
            new PontoCena( -247.9f,-1980.6f,  27.6f,  45f)
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

        public static readonly List<PontoCena> LocaisChacina = new List<PontoCena>
        {
            new PontoCena(   47.8f,-1649.2f, 29.4f, 180f),
            new PontoCena( -145.8f,-1478.4f, 34.5f,  90f),
            new PontoCena( -393.2f,-1830.4f, 25.6f, 270f),
            new PontoCena(  397.8f,-1660.4f, 29.3f,  45f),
            new PontoCena(  140.2f,-1453.8f, 28.3f,   0f)
        };

        public static readonly List<PontoCena> LocaisTraficoArmas = new List<PontoCena>
        {
            new PontoCena( 1042.8f,-3105.4f,  5.5f, 180f),
            new PontoCena(  430.5f,-3287.4f,  5.8f,  90f),
            new PontoCena( 1149.2f,-2052.8f, 29.7f, 270f),
            new PontoCena( 1694.5f, 3260.8f, 41.2f,   0f)
        };

        public static readonly List<PontoCena> LocaisLaboratorio = new List<PontoCena>
        {
            new PontoCena( 1830.4f, 3680.5f, 34.1f, 180f),
            new PontoCena( 2354.8f, 3176.4f, 50.2f,  90f),
            new PontoCena(  488.2f, 2748.5f, 46.8f, 270f),
            new PontoCena(  808.5f,-2648.4f, 20.0f,  45f)
        };

        public static readonly List<PontoCena> LocaisLatrocinio = new List<PontoCena>
        {
            new PontoCena( 1703.2f, 3756.8f, 34.3f, 270f),
            new PontoCena( -723.4f, -935.8f, 19.1f,  90f),
            new PontoCena(   75.2f,-1757.4f, 29.3f,   0f),
            new PontoCena( -469.8f, 6014.5f, 31.4f, 180f)
        };

        // --- Fase 3 parte 2 ---
        public static readonly List<PontoCena> LocaisSequestro = new List<PontoCena>
        {
            new PontoCena( -757.4f,-1470.8f,  5.1f,  90f),  // LSIA parking
            new PontoCena( 1252.8f, -775.4f, 59.3f, 180f),  // Mirror Park
            new PontoCena(-1638.6f, -913.4f, 12.2f, 270f),  // Del Perro parking
            new PontoCena( -252.4f,  558.6f,165.4f,  45f),  // Vinewood Hills
            new PontoCena( 1820.4f, 3745.8f, 34.1f,   0f)   // Sandy Shores
        };

        public static readonly List<PontoCena> LocaisIncendio = new List<PontoCena>
        {
            new PontoCena( 1098.4f,-1940.8f, 29.7f, 180f),  // La Mesa industrial
            new PontoCena( -432.8f,-1532.4f, 32.1f,  90f),  // Strawberry
            new PontoCena(  473.2f,-2703.8f, 17.0f, 270f),  // Cypress Flats
            new PontoCena( 2148.4f, 3328.6f, 47.2f,   0f),  // Sandy Shores rural
            new PontoCena( -924.8f,-2603.4f, 14.1f, 135f)   // LSIA industrial
        };

        public static readonly List<PontoCena> LocaisLavagem = new List<PontoCena>
        {
            new PontoCena(  148.2f, -638.4f, 43.6f,   0f),  // Pillbox Hill
            new PontoCena( 1168.8f,-2878.4f,  4.9f,  90f),  // El Burro Heights
            new PontoCena(  648.4f,-3352.8f,  6.0f, 180f),  // Terminal
            new PontoCena( 1288.6f,-1952.4f, 31.3f, 270f),  // La Mesa
            new PontoCena( -248.4f,  248.6f, 83.4f,  45f)   // Rockford Hills
        };

        public static readonly List<PontoCena> LocaisInvasao = new List<PontoCena>
        {
            new PontoCena( -352.4f,  183.6f, 84.2f,  90f),  // Rockford Hills
            new PontoCena( 1198.8f, -818.4f, 58.1f, 270f),  // Mirror Park
            new PontoCena( -104.6f,  323.8f,110.3f,   0f),  // Vinewood
            new PontoCena( -507.4f,-1667.8f, 25.2f, 180f),  // Strawberry
            new PontoCena( -458.8f, -347.4f, 34.3f, 135f)   // Morningwood
        };

        public static readonly List<PontoCena> LocaisRouboVeiculo = new List<PontoCena>
        {
            new PontoCena( -318.4f, -748.6f, 33.4f,  90f),  // Downtown
            new PontoCena(-1218.8f, -438.4f, 18.2f, 180f),  // Vespucci
            new PontoCena( -498.4f,-1038.8f, 28.1f, 270f),  // Little Seoul
            new PontoCena(-1558.6f, -848.4f, 12.3f,   0f),  // Del Perro
            new PontoCena( -238.4f,-1958.8f, 28.0f,  45f)   // Strawberry
        };

        // ===== MODELOS DE PEDS =====

        public static readonly List<string> ModelosVitima = new List<string>
        {
            "a_m_y_business_01","a_f_y_business_02","a_m_m_business_01",
            "a_m_y_hipster_01", "a_f_y_hipster_02", "a_m_y_genstreet_01"
        };

        public static readonly List<string> ModelosSuspeito = new List<string>
        {
            "g_m_y_lost_01",    "g_m_y_mexgoon_01","g_m_y_ballasout_01",
            "a_m_y_stbla_01",   "a_m_m_eastsa_02", "s_m_y_dealer_01"
        };

        public static readonly List<string> ModelosGangue = new List<string>
        {
            "g_m_y_ballasout_01","g_m_y_ballasout_02","g_m_y_ballasout_03",
            "g_m_y_famca_01",    "g_m_y_famdnf_01",   "g_m_y_famfor_01",
            "g_m_y_lost_01",     "g_m_y_lost_02",
            "g_m_y_mexgoon_01",  "g_m_y_mexgoon_02",  "g_m_y_mexgoon_03"
        };

        public static readonly List<string> ModelosCivil = new List<string>
        {
            "a_m_y_skater_01",   "a_f_y_runner_01",   "a_m_m_tourist_01",
            "a_f_m_business_02", "a_m_y_vinewood_01", "a_f_y_soucent_01"
        };

        // Fase 3 parte 2
        public static readonly List<string> ModelosPolicia = new List<string>
        {
            "s_m_y_cop_01", "s_m_y_hwaycop_01"
        };

        // ===== PROPS DE EVIDENCIA =====

        public static readonly List<string> PropsArmaBranca = new List<string>
        {
            "prop_cs_bottle_opaque01",  // frasco/objeto cortante como placeholder
            "prop_beer_bottle",         // garrafa de vidro
            "prop_cs_ciggy_01"          // item pessoal descartado (anel/cigarro)
        };

        public static readonly List<string> PropsCapsula = new List<string>
        {
            "prop_ld_casing","bkr_prop_weed_bag_01b"
        };

        public static readonly List<string> PropsItemPessoal = new List<string>
        {
            "prop_cs_ciggy_01","prop_beer_bottle","prop_plastic_cup_02",
            "prop_phone_ing",  "prop_cs_cuffs_01"
        };

        public static readonly List<string> PropsDroga = new List<string>
        {
            "prop_drug_package","prop_meth_bag_01","bkr_prop_coke_block_01b","prop_weed_bottle"
        };

        public static readonly List<string> PropsDinheiro = new List<string>
        {
            "prop_anim_cash_pile_01","prop_money_bag_01","prop_cash_crate_01"
        };

        public static readonly List<string> PropsCarga = new List<string>
        {
            "ng_proc_box_01a",       // caixa generica — confirmada valida
            "prop_cs_cardbox_01",    // caixa de papelao — confirmada valida
            "prop_boxpile_07d"       // pilha de caixas
        };

        public static readonly List<string> PropsFerramenta = new List<string>
        {
            "prop_cs_tablet",     // tablet / notebook — confirmado valido
            "prop_cs_cuffs_01",   // algemas — confirmado valido
            "prop_phone_ing"      // celular — confirmado valido
        };

        public static readonly List<string> PropsSangue = new List<string>
        {
            "prop_ped_blood_01","prop_rag_bloodied","ba_prop_battle_blood_01a"
        };

        public static readonly List<string> PropsCaixaArma = new List<string>
        {
            "prop_gun_case_01","prop_box_guncase_02a","prop_ld_case_01"
        };

        public static readonly List<string> PropsLaboratorio = new List<string>
        {
            "prop_meth_bag_01","bkr_prop_coke_block_01b",
            "prop_drug_package","prop_cs_bottle_opaque01",
            "bkr_prop_weed_bag_01b","prop_weed_bottle"
        };

        // Fase 3 parte 2
        public static readonly List<string> PropsAcelerante = new List<string>
        {
            "prop_jerrycan","prop_cs_petrol_can","prop_gas_tank_01"
        };

        public static readonly List<string> PropsEletronico = new List<string>
        {
            "prop_laptop_01a","prop_cs_tablet","prop_phone_ing"
        };

        // ===== NOMES AMERICANOS =====

        public static readonly List<string> NomesMasculinos = new List<string>
        {
            "James","Robert","Michael","David","William",
            "Richard","Joseph","Thomas","Charles","Christopher",
            "Daniel","Matthew","Anthony","Mark","Andrew",
            "Steven","Kevin","Jason","Ryan","Brian"
        };

        public static readonly List<string> NomesFemininos = new List<string>
        {
            "Patricia","Jennifer","Linda","Barbara","Susan",
            "Jessica","Sarah","Karen","Lisa","Nancy",
            "Betty","Margaret","Sandra","Ashley","Dorothy",
            "Kimberly","Emily","Donna","Michelle","Carol"
        };

        public static readonly List<string> Sobrenomes = new List<string>
        {
            "Smith","Johnson","Williams","Brown","Jones",
            "Garcia","Miller","Davis","Rodriguez","Martinez",
            "Hernandez","Lopez","Wilson","Anderson","Taylor",
            "Thomas","Moore","Jackson","Martin","Lee"
        };
    }
}