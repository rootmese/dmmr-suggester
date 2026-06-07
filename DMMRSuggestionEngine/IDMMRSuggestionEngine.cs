using System;
using System.Collections.Generic;

namespace DMMRSuggestionEngine
{
    /// <summary>
    /// Interface pública da engine de sugestões DMMR.
    /// </summary>
    /// <typeparam name="T">Tipo dos itens (deve ser não-nulo).</typeparam>
    public interface IDMMRSuggestionEngine<T> where T : notnull
    {
        /// <summary>
        /// Configurações de re-ranking (peso fuzzy vs peso do item).
        /// </summary>
        DMMRSuggestionEngine<T>.ReRankSettings Config { get; set; }

        /// <summary>
        /// Carrega os dados na engine.
        /// ATENÇÃO: Este método NÃO é thread-safe. Deve ser chamado antes de qualquer busca.
        /// </summary>
        void LoadData(IEnumerable<T> data, Func<T, string> textSelector, Func<T, float>? weightSelector = null);

        /// <summary>
        /// Sugere itens baseados na query, com tolerância a erros.
        /// </summary>
        List<T> Suggest(string query, int maxAllowedErrors = 2, int maxResults = 5);

        /// <summary>
        /// Limpa o cache interno de resultados.
        /// </summary>
        void ClearCache();
    }
}