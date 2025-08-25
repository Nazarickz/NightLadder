# NightLadder - Sistema de Elo para V Rising

[![Version](https://img.shields.io/badge/version-0.1.0-blue.svg)](#) [![Framework](https://img.shields.io/badge/BepInEx-IL2CPP-green)](#) [![VCF](https://img.shields.io/badge/VCF-supported-orange)](#)

## Descri��o
NightLadder � um mod de servidor para V Rising que implementa um sistema de ranqueamento (elo) para PvP. Ele registra vit�rias/derrotas entre jogadores, ajusta pontos com base na diferen�a de elo e n�vel, promove/rebaixa entre elos e mant�m ranking persistente. Integra BepInEx IL2CPP, Harmony e VampireCommandFramework.

Este reposit�rio cont�m apenas o NightLadder (core + plugin), sem integra��es com outros plugins.

- C�digo do plugin: NightLadder.Plugin/
- N�cleo (modelos/servi�os/armazenamento): NightLadder.Core/

## Principais recursos
- Rastreamento de mortes est�vel (patch em DeathEventListenerSystem).
- Prote��o de kill?steal (credita ao jogador que derrubou a v�tima quando aplic�vel).
- Assist�ncias de combate: janela temporal (~30s), at� 2 assistentes, op��o cl�?only.
- Penaliza��o suave por diferen�a de n�vel (anti?farm), com teto configur�vel.
- Perda de pontos sim�trica; carryover entre elos em promo��es/rebaixamentos.
- Mensagens claras no chat e comandos administrativos.
- Persist�ncia com LiteDB; import opcional a partir de JSON na primeira carga.

## Instala��o
1. Requisitos: BepInEx (IL2CPP), Harmony, VampireCommandFramework.
2. Copie as DLLs geradas para `BepInEx/plugins` no servidor.
3. Na primeira execu��o ser�o criados:
   - `BepInEx/plugins/NightLadder/rankconfig.json` (config do elo)
   - `BepInEx/plugins/NightLadder/ranks.json` (persist�ncia opcional)
   - `BepInEx/plugins/NightLadder/admins.json` (whitelist de admins)

## Configura��o (rankconfig.json)
- SameTierKillPoints: base de pontos para kills no mesmo grupo.
- PerTierDifferenceBonus / PerTierDifferencePenalty: ajuste por diferen�a de elo.
- DraculaSlots: n�mero de vagas no topo.
- LevelPenaltyEnabled, LevelGapThreshold, LevelPenaltyPerLevelPercent, LevelPenaltyMaxReductionPercent.
- LevelTrackingMode: "Live" (n�vel atual) ou "Max" (maior n�vel observado).
- AssistClanOnlyEnabled: limita assist�ncias ao mesmo cl�.

Observa��es
- Carryover sempre ativo; no primeiro step os pontos n�o ficam negativos.
- A perda da v�tima � o negativo exato do ganho do killer, ap�s regras de elo/n�vel.

## Comandos (VCF)
P�blicos
- `.rank elo | .rk el` � seu elo e pontos.
- `.rank top [n] | .rk tp [n]` � top N por pontos (padr�o 10).
- `.rank whoami | .rk id` � seu PlatformId e nome.

Admin/Debug
- `.rank debugelo` | `.rk dbg`
- `.rank debugelo.deaths true|false` | `.rk dbgd true|false`
- `.rank admin.add <PlatformId> <pontos>` | `.rk add <id> <pontos>`
- `.rank admin.set <PlatformId> <pontos>` | `.rk set <id> <pontos>`
- `.rank admin.step <PlatformId> <�ndice>` | `.rk stp <id> <�ndice>`
- `.rank admin.reset <PlatformId>` | `.rk rs <id>`
- `.rank admin.sim <A> <B>` | `.rk sim <A> <B>`
- `.rank admin.win <K> <V> [KName] [VName]` | `.rk win ...`
- `.rank admin.mywin <V> [VName]` | `.rk my ...`
- `.rank admin.save` | `.rk sv`

## Build
- .NET 6 SDK.
- Ajuste InteropDir/CoreDir/DllsDir no NightLadder.Plugin.csproj para apontar para as DLLs do jogo/mod loader.

## Uso
- Instale as DLLs em `BepInEx/plugins` e reinicie o servidor.
- Detalhes do plugin: veja NightLadder.Plugin/README.md.
