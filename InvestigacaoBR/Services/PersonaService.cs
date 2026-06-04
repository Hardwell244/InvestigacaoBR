using InvestigacaoBR.Core;
using InvestigacaoBR.Data;
using LSPD_First_Response;
using LSPD_First_Response.Engine.Scripting.Entities; // Persona, Gender, ELicenseState
using LSPD_First_Response.Mod.API;                    // Functions
using Rage;
using System;

namespace InvestigacaoBR.Services
{
    /// <summary>
    /// Ponte entre o sistema de Persona do LSPDFR e o nosso PedDoCaso.
    ///  - Identificar: PUXA a persona do ped vivo e faz o snapshot no PedDoCaso (peds do mundo).
    ///  - AplicarIdentidade: GRAVA a identidade autorada no ped vivo (peds autorados da cena).
    /// É a única classe que toca a API de Persona; os modelos nunca a chamam direto.
    /// </summary>
    public class PersonaService
    {
        /// <summary>
        /// Puxa a Persona do ped vivo e snapshota no PedDoCaso (nome, nascimento, genero,
        /// procurado, modelo). Direção principal — usada para peds do mundo que o detetive aborda.
        /// </summary>
        public bool Identificar(Ped ped, PedDoCaso pedDoCaso)
        {
            if (pedDoCaso == null)
            {
                Logger.Warn("PersonaService.Identificar: PedDoCaso nulo.");
                return false;
            }
            if (ped == null || !ped.Exists())
            {
                Logger.Warn("PersonaService.Identificar: ped inválido/inexistente.");
                return false;
            }

            try
            {
                Persona persona = Functions.GetPersonaForPed(ped);
                if (persona == null)
                {
                    Logger.Warn("PersonaService.Identificar: GetPersonaForPed retornou nulo.");
                    return false;
                }

                string nome = persona.FullName;
                DateTime nascimento = persona.Birthday;
                string genero = GeneroParaTexto(persona.Gender);
                bool procurado = persona.Wanted;
                string modelo = ped.Model.Name;

                pedDoCaso.PedVivo = ped;
                pedDoCaso.RegistrarIdentificacao(nome, nascimento, genero, procurado, modelo);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "PersonaService.Identificar");
                return false;
            }
        }

        /// <summary>
        /// Grava a identidade autorada do PedDoCaso no ped vivo, para que rodar o ID no
        /// computador do LSPDFR mostre o mesmo nome. Usado nos peds autorados da cena.
        /// </summary>
        public bool AplicarIdentidade(Ped ped, PedDoCaso pedDoCaso)
        {
            if (pedDoCaso == null)
            {
                Logger.Warn("PersonaService.AplicarIdentidade: PedDoCaso nulo.");
                return false;
            }
            if (ped == null || !ped.Exists())
            {
                Logger.Warn("PersonaService.AplicarIdentidade: ped inválido/inexistente.");
                return false;
            }

            try
            {
                string[] partes = SepararNome(pedDoCaso.Nome);
                Gender genero = TextoParaGenero(pedDoCaso.Genero);
                DateTime nascimento = pedDoCaso.DataNascimento == DateTime.MinValue
                    ? new DateTime(1990, 1, 1)
                    : pedDoCaso.DataNascimento;

                // ---- CORREÇÃO DO CONSTRUTOR DA PERSONA (Padrão Moderno LSPDFR) ----
                // Passamos apenas os 4 parâmetros essenciais exigidos pelo construtor nativo
                Persona persona = new Persona(partes[0], partes[1], genero, nascimento);

                // Aplicamos os estados adicionais diretamente nas propriedades do objeto criado
                persona.Wanted = pedDoCaso.Procurado;
                persona.ELicenseState = ELicenseState.Valid;
                // ------------------------------------------------------------------

                Functions.SetPersonaForPed(ped, persona);
                pedDoCaso.PedVivo = ped;
                Logger.Info($"Identidade autorada aplicada perfeitamente ao ped e injetada no LSPDFR: '{pedDoCaso.Nome}'.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "PersonaService.AplicarIdentidade (Falha ao injetar Persona no LSPDFR, usando fallback)");

                // Fallback de segurança: Garante o vínculo na nossa UI mesmo se a API do LSPDFR falhar
                pedDoCaso.PedVivo = ped;
                return false;
            }
        }

        // ----- Helpers Mantidos e Protegidos -----

        private static string GeneroParaTexto(Gender genero)
        {
            return genero == Gender.Female ? "Feminino" : "Masculino";
        }

        private static Gender TextoParaGenero(string texto)
        {
            if (!string.IsNullOrEmpty(texto) && texto.Trim().ToLowerInvariant().StartsWith("f"))
            {
                return Gender.Female;
            }
            return Gender.Male;
        }

        /// <summary>Quebra "Nome Sobrenome" em [forename, surname]. Robusto a nome vazio/sem sobrenome.</summary>
        private static string[] SepararNome(string nomeCompleto)
        {
            if (string.IsNullOrEmpty(nomeCompleto))
            {
                return new[] { "John", "Doe" };
            }

            string[] split = nomeCompleto.Trim().Split(new[] { ' ' }, 2);
            return split.Length == 2 ? split : new[] { split[0], string.Empty };
        }
    }
}