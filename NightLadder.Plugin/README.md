# NightLadder - Sistema de Elo para V Rising

[![Version](https://img.shields.io/badge/version-0.1.0-blue.svg)](#) [![Framework](https://img.shields.io/badge/BepInEx-IL2CPP-green)](#) [![VCF](https://img.shields.io/badge/VCF-supported-orange)](#)

## Descrição
NightLadder é um mod que implementa um sistema de ranqueamento (elo) para servidores de V Rising. Ele registra vitórias/derrotas entre jogadores, concede/perde pontos, promove/rebaixa entre elos e mantém um ranking persistente. Integra BepInEx, Harmony e VampireCommandFramework.

## Principais recursos
- Captura de mortes estável: patch direto em `DeathEventListenerSystem.OnUpdate`.
- Proteção de kill-steal: integração com `VampireDownedServerEventSystem` para creditar a kill ao "downer" quando disponível.
- Assistências (clã-only por padrão): rastreadas via `StatChangeSystem.OnUpdate` (janela ~30s), até 2 assistentes por kill.
- Penalização por diferença de nível (anti-farm) suave e com teto configurável.
- Perda de PDL simétrica e carryover de pontos entre elos (promoções/rebaixamentos com excedente).
- Mensagens padronizadas no chat e comandos administrativos.
- Persistência: LiteDB com upsert parcial e import opcional de JSON na primeira carga.

## Instalação
1. Requisitos: BepInEx (IL2CPP), Harmony, VampireCommandFramework.
2. Copie as DLLs para `BepInEx/plugins`.
3. Na primeira execução serão criados:
   - `BepInEx/plugins/NightLadder/rankconfig.json`
   - `BepInEx/plugins/NightLadder/ranks.json`
   - `BepInEx/plugins/NightLadder/admins.json`

## Configuração (rankconfig.json)
Parâmetros relevantes:
- SameTierKillPoints, PerTierDifferenceBonus, PerTierDifferencePenalty
- DraculaSlots
- LevelPenaltyEnabled, LevelGapThreshold, LevelPenaltyPerLevelPercent, LevelPenaltyMaxReductionPercent
- LevelTrackingMode: "Live" ou "Max"
- AssistClanOnlyEnabled

Observações:
- Carryover sempre ativo (promoção/rebaixamento com excedente). No primeiro step, pontos não ficam negativos.
- A perda da vítima é o negativo exato do ganho do killer, considerando elo e penalização por nível.

## Comandos (VCF)
Públicos
- `.rank elo | .rk el`
- `.rank top [n] | .rk tp [n]`
- `.rank whoami | .rk id`

Debug/Admin
- `.rank debugelo | .rk dbg`
- `.rank debugelo.deaths true|false | .rk dbgd true|false`
- `.rank admin.add <PlatformId> <pontos> | .rk add <id> <pontos>`
- `.rank admin.set <PlatformId> <pontos> | .rk set <id> <pontos>`
- `.rank admin.step <PlatformId> <índice> | .rk stp <id> <índice>`
- `.rank admin.reset <PlatformId> | .rk rs <id>`
- `.rank admin.sim <A> <B> | .rk sim <A> <B>`
- `.rank admin.win <K> <V> [KName] [VName] | .rk win <K> <V> [KName] [VName]`
- `.rank admin.mywin <V> [VName] | .rk my <V> [VName]`
- `.rank admin.save | .rk sv`

## Notas
- Requer BepInEx IL2CPP e as DLLs do jogo/mod loader (Unity.Entities, ProjectM.* etc.).
- Componentes e razões de StatChange podem variar entre versões; mantenha o mod atualizado caso o jogo seja atualizado.
