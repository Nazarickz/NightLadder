# NightLadder - Sistema de Elo para V Rising

[![Version](https://img.shields.io/badge/version-0.1.0-blue.svg)](#) [![Framework](https://img.shields.io/badge/BepInEx-IL2CPP-green)](#) [![VCF](https://img.shields.io/badge/VCF-supported-orange)](#)

## Descri��o
NightLadder � um mod que implementa um sistema de ranqueamento (elo) para servidores de V Rising. Ele registra vit�rias/derrotas entre jogadores, concede/perde pontos, promove/rebaixa entre elos e mant�m um ranking persistente. Integra BepInEx, Harmony e VampireCommandFramework.

## Principais recursos
- Captura de mortes est�vel: patch direto em `DeathEventListenerSystem.OnUpdate`.
- Prote��o de kill-steal: integra��o com `VampireDownedServerEventSystem` para creditar a kill ao "downer" quando dispon�vel.
- Assist�ncias (cl�-only por padr�o): rastreadas via `StatChangeSystem.OnUpdate` (janela ~30s), at� 2 assistentes por kill.
- Penaliza��o por diferen�a de n�vel (anti-farm) suave e com teto configur�vel.
- Perda de PDL sim�trica e carryover de pontos entre elos (promo��es/rebaixamentos com excedente).
- Mensagens padronizadas no chat e comandos administrativos.
- Persist�ncia: LiteDB com upsert parcial e import opcional de JSON na primeira carga.

## Instala��o
1. Requisitos: BepInEx (IL2CPP), Harmony, VampireCommandFramework.
2. Copie as DLLs para `BepInEx/plugins`.
3. Na primeira execu��o ser�o criados:
   - `BepInEx/plugins/NightLadder/rankconfig.json`
   - `BepInEx/plugins/NightLadder/ranks.json`
   - `BepInEx/plugins/NightLadder/admins.json`

## Configura��o (rankconfig.json)
Par�metros relevantes:
- SameTierKillPoints, PerTierDifferenceBonus, PerTierDifferencePenalty
- DraculaSlots
- LevelPenaltyEnabled, LevelGapThreshold, LevelPenaltyPerLevelPercent, LevelPenaltyMaxReductionPercent
- LevelTrackingMode: "Live" ou "Max"
- AssistClanOnlyEnabled

Observa��es:
- Carryover sempre ativo (promo��o/rebaixamento com excedente). No primeiro step, pontos n�o ficam negativos.
- A perda da v�tima � o negativo exato do ganho do killer, considerando elo e penaliza��o por n�vel.

## Comandos (VCF)
P�blicos
- `.rank elo | .rk el`
- `.rank top [n] | .rk tp [n]`
- `.rank whoami | .rk id`

Debug/Admin
- `.rank debugelo | .rk dbg`
- `.rank debugelo.deaths true|false | .rk dbgd true|false`
- `.rank admin.add <PlatformId> <pontos> | .rk add <id> <pontos>`
- `.rank admin.set <PlatformId> <pontos> | .rk set <id> <pontos>`
- `.rank admin.step <PlatformId> <�ndice> | .rk stp <id> <�ndice>`
- `.rank admin.reset <PlatformId> | .rk rs <id>`
- `.rank admin.sim <A> <B> | .rk sim <A> <B>`
- `.rank admin.win <K> <V> [KName] [VName] | .rk win <K> <V> [KName] [VName]`
- `.rank admin.mywin <V> [VName] | .rk my <V> [VName]`
- `.rank admin.save | .rk sv`

## Notas
- Requer BepInEx IL2CPP e as DLLs do jogo/mod loader (Unity.Entities, ProjectM.* etc.).
- Componentes e raz�es de StatChange podem variar entre vers�es; mantenha o mod atualizado caso o jogo seja atualizado.
