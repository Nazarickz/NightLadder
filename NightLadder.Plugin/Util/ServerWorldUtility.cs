using System;
using Unity.Entities;

namespace NightLadder.Plugin.Util;

// Utilitário central para obter o EntityManager do mundo do servidor em V Rising (IL2CPP)
public static class ServerWorldUtility
{
    // Tenta obter o EntityManager do mundo servidor para o servidor dedicado de V Rising
    public static EntityManager GetServerEntityManager()
    {
        // Se já está em cache
        if (NightLadder.Plugin.App.ServerEntityManager is EntityManager em) return em;

        // Procura mundos por nomes comuns (sem LINQ para evitar dependências de extensão)
        World? server = null;
        var worlds = World.s_AllWorlds;
        if (worlds != null)
        {
            foreach (var w in worlds)
            {
                if (w == null) continue;
                var name = w.Name ?? string.Empty;
                if (name == "Server" || name == "VRisingServer" || name.IndexOf("Server", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    server = w;
                    break;
                }
            }
        }

        if (server == null)
        {
            // Volta para o mundo padrão
            server = World.DefaultGameObjectInjectionWorld;
        }
        if (server == null)
        {
            throw new InvalidOperationException("Mundo do servidor não encontrado ainda.");
        }
        return server.EntityManager;
    }
}
