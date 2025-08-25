# NightLadder — Sistema de Elo para V Rising

Badges: versão 0.1.0 • BepInEx IL2CPP • Harmony • VampireCommandFramework (VCF)

Visão geral
- NightLadder é um mod de servidor para V Rising focado em PvP com sistema de ranqueamento (elo) persistente.
- Registra vitórias/derrotas, calcula ganhos/perdas de pontos considerando diferença de elo e nível, e promove/rebaixa entre elos com carryover de pontos.
- Integra BepInEx IL2CPP + Harmony para ganchos estáveis no servidor e expõe comandos via VampireCommandFramework.

Estrutura do repositório
- NightLadder.Core/ — núcleo do sistema (modelos, serviços, persistência)
- NightLadder.Plugin/ — integração com o servidor (ganchos, comandos, bootstrap)
- NightLadder.sln — solução .NET 6

Recursos principais
- Captura estável de mortes: patch em DeathEventListenerSystem (via Harmony).
- Anti kill-steal: prioriza o jogador que derrubou a vítima quando detectado.
- Assistências: janela temporal (~30s), até 2 assistentes por abate, opção “clã-only”.
- Penalização por diferença de nível (anti-farm): suave, com teto configurável.
- Perda de pontos simétrica e carryover entre elos nas promoções/rebaixamentos.
- Persistência em LiteDB com import opcional de JSON na primeira carga.
- Comandos amigáveis para jogadores e administrativos para GMs.

Requisitos
- .NET 6 SDK (para build).
- Servidor com BepInEx (IL2CPP).
- Harmony.
- VampireCommandFramework (VCF) para os comandos no chat.

Instalação
1) Build
- Abra a solução NightLadder.sln com .NET 6 instalado.
- Ajuste no NightLadder.Plugin.csproj (se necessário) os caminhos de Interop/Core/Dlls do seu ambiente de servidor/loader.
- Compile em Release.

2) Deploy
- Copie as DLLs geradas para BepInEx/plugins (recomendado: em uma subpasta BepInEx/plugins/NightLadder).
- Na primeira execução, serão criados automaticamente:
  - BepInEx/plugins/NightLadder/rankconfig.json (configuração do elo)
  - BepInEx/plugins/NightLadder/ranks.json (persistência opcional para import)
  - BepInEx/plugins/NightLadder/ranks.ldb (banco LiteDB)
  - BepInEx/plugins/NightLadder/admins.json (lista de admins/whitelist)

Configuração (rankconfig.json)
- SameTierKillPoints: pontos base para kills no mesmo grupo de elo.
- PerTierDifferenceBonus: bônus por diferença positiva de elo entre vítima > killer.
- PerTierDifferencePenalty: penalidade por diferença quando killer > vítima.
- DraculaSlots: quantidade de vagas no topo (rank “Drácula”).
- LevelPenaltyEnabled: habilita penalidade por diferença de nível.
- LevelGapThreshold: a partir de qual diferença aplica a penalidade (killerLevel - victimLevel).
- LevelPenaltyPerLevelPercent: redução adicional por nível acima do limiar (ex.: 0.05 = 5% por nível).
- LevelPenaltyMaxReductionPercent: redução máxima acumulada (ex.: 0.8 = até 80%).
- LevelTrackingMode: “Live” (nível atual) ou “Max” (maior nível observado).
- AssistClanOnlyEnabled: se true, apenas assistências do mesmo clã contam.

Progressão de elos (exemplo padrão)
- Osso ? Osso-Reforçado ? Cobre ? Cobre-Impiedoso ? Ferro ? Ferro-Impiedoso ? Ouro-sol ? Prata-Escura ? Sanguíneo ? Drácula
- Cada step possui pontos-alvo (ThresholdPoints) e regra de reset parcial ao promover (ResetsOnPromotion), resultando em carryover controlado.

Como o cálculo de pontos funciona (resumo)
- Ponto base: SameTierKillPoints.
- Ajuste por diferença de elo: bônus/penalidade por steps de distância (agrupando sub-steps equivalentes).
- Penalidade por nível (opcional): reduz proporcionalmente o ganho se o killer tem nível muito superior.
- Simetria: a perda da vítima é o negativo exato do ganho do killer após todas as regras.

Comandos (VCF)
Públicos
- .rank elo | .rk el — mostra seu elo e pontos.
- .rank top [n] | .rk tp [n] — top N por pontos (padrão 10).
- .rank whoami | .rk id — mostra seu PlatformId e nome.

Admin/Debug
- .rank debugelo | .rk dbg
- .rank debugelo.deaths true|false | .rk dbgd true|false
- .rank admin.add <PlatformId> <pontos> | .rk add <id> <pontos>
- .rank admin.set <PlatformId> <pontos> | .rk set <id> <pontos>
- .rank admin.step <PlatformId> <índice> | .rk stp <id> <índice>
- .rank admin.reset <PlatformId> | .rk rs <id>
- .rank admin.sim <A> <B> | .rk sim <A> <B>
- .rank admin.win <K> <V> [KName] [VName] | .rk win ...
- .rank admin.mywin <V> [VName] | .rk my ...
- .rank admin.save | .rk sv

Arquitetura
- Core (NightLadder.Core)
  - Models: RankConfig, RankStep, PlayerRank, etc.
  - Services: RankManager/RankService — regras de cálculo, progressão e ranking.
  - Storage: IRankStorage, LiteDbRankStorage, JsonRankStorage (import/export).
- Plugin (NightLadder.Plugin)
  - Hooks (Harmony): DeathEventListenerPatch, VampireDownedPatch, StatChangeHook.
  - Bootstrap: HarmonyBootstrap, inicializa RankManager e aplica patches quando o servidor estiver pronto.
  - Comandos: RankCommands e ShortRankCommands (VCF).
  - Serviços utilitários: LevelService, AdminService, ServerWorldUtility.

Boas práticas e dicas
- Faça backup periódico do ranks.ldb (e opcionalmente do ranks.json de export).
- Ajuste DraculaSlots e thresholds de steps para o perfil do seu servidor (casual vs competitivo).
- Se quiser desabilitar o anti-farm por nível, defina LevelPenaltyEnabled = false.
- Para servidores com muitos clãs, considere manter AssistClanOnlyEnabled = true.

Compilação e desenvolvimento
- Requisitos: .NET 6 SDK.
- Projetos: NightLadder.Core (biblioteca), NightLadder.Plugin (plugin servidor).
- Ajustes de caminho das dependências do jogo/loader podem ser necessários no csproj do plugin.

Compatibilidade
- V Rising (servidor) com BepInEx IL2CPP.
- Harmony para patching.
- VampireCommandFramework para comandos no chat.

Licença
- Caso deseje publicar publicamente, adicione um arquivo LICENSE apropriado ao projeto.

Créditos
- Desenvolvido para a comunidade de servidores V Rising, com foco em PvP saudável e competitivo.
