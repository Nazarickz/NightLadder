# NightLadder � Sistema de Elo para V Rising

Badges: vers�o 0.1.0 � BepInEx IL2CPP � Harmony � VampireCommandFramework (VCF)

Vis�o geral
- NightLadder � um mod de servidor para V Rising focado em PvP com sistema de ranqueamento (elo) persistente.
- Registra vit�rias/derrotas, calcula ganhos/perdas de pontos considerando diferen�a de elo e n�vel, e promove/rebaixa entre elos com carryover de pontos.
- Integra BepInEx IL2CPP + Harmony para ganchos est�veis no servidor e exp�e comandos via VampireCommandFramework.

Estrutura do reposit�rio
- NightLadder.Core/ � n�cleo do sistema (modelos, servi�os, persist�ncia)
- NightLadder.Plugin/ � integra��o com o servidor (ganchos, comandos, bootstrap)
- NightLadder.sln � solu��o .NET 6

Recursos principais
- Captura est�vel de mortes: patch em DeathEventListenerSystem (via Harmony).
- Anti kill-steal: prioriza o jogador que derrubou a v�tima quando detectado.
- Assist�ncias: janela temporal (~30s), at� 2 assistentes por abate, op��o �cl�-only�.
- Penaliza��o por diferen�a de n�vel (anti-farm): suave, com teto configur�vel.
- Perda de pontos sim�trica e carryover entre elos nas promo��es/rebaixamentos.
- Persist�ncia em LiteDB com import opcional de JSON na primeira carga.
- Comandos amig�veis para jogadores e administrativos para GMs.

Requisitos
- .NET 6 SDK (para build).
- Servidor com BepInEx (IL2CPP).
- Harmony.
- VampireCommandFramework (VCF) para os comandos no chat.

Instala��o
1) Build
- Abra a solu��o NightLadder.sln com .NET 6 instalado.
- Ajuste no NightLadder.Plugin.csproj (se necess�rio) os caminhos de Interop/Core/Dlls do seu ambiente de servidor/loader.
- Compile em Release.

2) Deploy
- Copie as DLLs geradas para BepInEx/plugins (recomendado: em uma subpasta BepInEx/plugins/NightLadder).
- Na primeira execu��o, ser�o criados automaticamente:
  - BepInEx/plugins/NightLadder/rankconfig.json (configura��o do elo)
  - BepInEx/plugins/NightLadder/ranks.json (persist�ncia opcional para import)
  - BepInEx/plugins/NightLadder/ranks.ldb (banco LiteDB)
  - BepInEx/plugins/NightLadder/admins.json (lista de admins/whitelist)

Configura��o (rankconfig.json)
- SameTierKillPoints: pontos base para kills no mesmo grupo de elo.
- PerTierDifferenceBonus: b�nus por diferen�a positiva de elo entre v�tima > killer.
- PerTierDifferencePenalty: penalidade por diferen�a quando killer > v�tima.
- DraculaSlots: quantidade de vagas no topo (rank �Dr�cula�).
- LevelPenaltyEnabled: habilita penalidade por diferen�a de n�vel.
- LevelGapThreshold: a partir de qual diferen�a aplica a penalidade (killerLevel - victimLevel).
- LevelPenaltyPerLevelPercent: redu��o adicional por n�vel acima do limiar (ex.: 0.05 = 5% por n�vel).
- LevelPenaltyMaxReductionPercent: redu��o m�xima acumulada (ex.: 0.8 = at� 80%).
- LevelTrackingMode: �Live� (n�vel atual) ou �Max� (maior n�vel observado).
- AssistClanOnlyEnabled: se true, apenas assist�ncias do mesmo cl� contam.

Progress�o de elos (exemplo padr�o)
- Osso ? Osso-Refor�ado ? Cobre ? Cobre-Impiedoso ? Ferro ? Ferro-Impiedoso ? Ouro-sol ? Prata-Escura ? Sangu�neo ? Dr�cula
- Cada step possui pontos-alvo (ThresholdPoints) e regra de reset parcial ao promover (ResetsOnPromotion), resultando em carryover controlado.

Como o c�lculo de pontos funciona (resumo)
- Ponto base: SameTierKillPoints.
- Ajuste por diferen�a de elo: b�nus/penalidade por steps de dist�ncia (agrupando sub-steps equivalentes).
- Penalidade por n�vel (opcional): reduz proporcionalmente o ganho se o killer tem n�vel muito superior.
- Simetria: a perda da v�tima � o negativo exato do ganho do killer ap�s todas as regras.

Comandos (VCF)
P�blicos
- .rank elo | .rk el � mostra seu elo e pontos.
- .rank top [n] | .rk tp [n] � top N por pontos (padr�o 10).
- .rank whoami | .rk id � mostra seu PlatformId e nome.

Admin/Debug
- .rank debugelo | .rk dbg
- .rank debugelo.deaths true|false | .rk dbgd true|false
- .rank admin.add <PlatformId> <pontos> | .rk add <id> <pontos>
- .rank admin.set <PlatformId> <pontos> | .rk set <id> <pontos>
- .rank admin.step <PlatformId> <�ndice> | .rk stp <id> <�ndice>
- .rank admin.reset <PlatformId> | .rk rs <id>
- .rank admin.sim <A> <B> | .rk sim <A> <B>
- .rank admin.win <K> <V> [KName] [VName] | .rk win ...
- .rank admin.mywin <V> [VName] | .rk my ...
- .rank admin.save | .rk sv

Arquitetura
- Core (NightLadder.Core)
  - Models: RankConfig, RankStep, PlayerRank, etc.
  - Services: RankManager/RankService � regras de c�lculo, progress�o e ranking.
  - Storage: IRankStorage, LiteDbRankStorage, JsonRankStorage (import/export).
- Plugin (NightLadder.Plugin)
  - Hooks (Harmony): DeathEventListenerPatch, VampireDownedPatch, StatChangeHook.
  - Bootstrap: HarmonyBootstrap, inicializa RankManager e aplica patches quando o servidor estiver pronto.
  - Comandos: RankCommands e ShortRankCommands (VCF).
  - Servi�os utilit�rios: LevelService, AdminService, ServerWorldUtility.

Boas pr�ticas e dicas
- Fa�a backup peri�dico do ranks.ldb (e opcionalmente do ranks.json de export).
- Ajuste DraculaSlots e thresholds de steps para o perfil do seu servidor (casual vs competitivo).
- Se quiser desabilitar o anti-farm por n�vel, defina LevelPenaltyEnabled = false.
- Para servidores com muitos cl�s, considere manter AssistClanOnlyEnabled = true.

Compila��o e desenvolvimento
- Requisitos: .NET 6 SDK.
- Projetos: NightLadder.Core (biblioteca), NightLadder.Plugin (plugin servidor).
- Ajustes de caminho das depend�ncias do jogo/loader podem ser necess�rios no csproj do plugin.

Compatibilidade
- V Rising (servidor) com BepInEx IL2CPP.
- Harmony para patching.
- VampireCommandFramework para comandos no chat.

Licen�a
- Caso deseje publicar publicamente, adicione um arquivo LICENSE apropriado ao projeto.

Cr�ditos
- Desenvolvido para a comunidade de servidores V Rising, com foco em PvP saud�vel e competitivo.
