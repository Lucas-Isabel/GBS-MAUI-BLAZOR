using System;
using System.Collections.Generic;
using System.Linq;

namespace IMPORTADOR_CLIPSE.Global
{
    public class AppState
    {
        // Variável global
        public List<string> urls { get; private set; }

        public bool IsImport { get; private set; } = false;

        // Você pode adicionar eventos para notificar mudanças, se necessário
        public event Action OnChange;

        // Construtor para inicializar a lista de URLs
        public AppState()
        {
            urls = new List<string> { "https://0.0.0.0/" };
        }

        public void Reset()
        {
            urls = new List<string> { "https://0.0.0.0/" };
        }

        public void AddUrl(string url)
        {
            // Adiciona o URL apenas se ele não estiver presente
            if (!urls.Contains(url))
            {
                urls.Add(url);
                NotifyStateChanged(); // Notifica que o estado mudou
            }
        }

        public void SetIsImport(bool value)
        {
            IsImport = value;
            NotifyStateChanged(); // Notifica que o estado de IsImport mudou
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
