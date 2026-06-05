using System.Collections.Generic;

namespace InvestigacaoBR.Data
{
    public enum PersonalidadeParceiro
    {
        Correto = 0,  // Det. Miller  — pelo livro, denuncia corrupcao
        ZonaCinza = 1,  // Det. Torres  — pragmatico, nao e santo
        Corrupto = 2   // Det. Johnson — sempre sugere o caminho facil
    }

    public class Partner
    {
        public string Nome { get; set; }
        public string ModeloPed { get; set; }
        public PersonalidadeParceiro Personalidade { get; set; }
        public string Descricao { get; set; }

        // Comentarios por contexto — indexados por PersonalidadeParceiro
        public List<string> ComentariosChegada { get; set; }
        public List<string> ComentariosEvidencia { get; set; }
        public List<string> ComentariosResolucao { get; set; }
        public List<string> ComentariosPropina { get; set; }
    }
}