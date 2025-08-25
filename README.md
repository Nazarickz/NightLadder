# NightLadder - Sistema de Elo para V Rising

[![Version](https://img.shields.io/badge/version-0.1.0-blue.svg)](#) [![Framework](https://img.shields.io/badge/BepInEx-IL2CPP-green)](#) [![VCF](https://img.shields.io/badge/VCF-supported-orange)](#)

## Descrição
NightLadder é um mod de servidor para V Rising que implementa um sistema de ranqueamento (elo) para PvP. Ele registra vitórias/derrotas entre jogadores, ajusta pontos com base na diferença de elo e nível, promove/rebaixa entre elos e mantém ranking persistente. Integra BepInEx IL2CPP, Harmony e VampireCommandFramework.

Este repositório contém apenas o NightLadder (core + plugin), sem integrações com outros plugins.

- Código do plugin: NightLadder.Plugin/
- Núcleo (modelos/serviços/armazenamento): NightLadder.Core/

## Principais recursos
- Rastreamento de mortes estável (patch em DeathEventListenerSystem).
- Proteção de kill?steal (credita ao jogador que derrubou a vítima quando aplicável).
- Assistências de combate: janela temporal (~30s), até 2 assistentes, opção clã?only.
- Penalização suave por diferença de nível (anti?farm), com teto configurável.
- Perda de pontos simétrica; carryover entre elos em promoções/rebaixamentos.
- Mensagens claras no chat e comandos administrativos.
- Persistência com LiteDB; import opcional a partir de JSON na primeira carga.

## Instalação
1. Requisitos: BepInEx (IL2CPP), Harmony, VampireCommandFramework.
2. Copie as DLLs geradas para `BepInEx/plugins` no servidor.
3. Na primeira execução serão criados:
   - `BepInEx/plugins/NightLadder/rankconfig.json` (config do elo)
   - `BepInEx/plugins/NightLadder/ranks.json` (persistência opcional)
   - `BepInEx/plugins/NightLadder/admins.json` (whitelist de admins)

## Configuração (rankconfig.json)
- SameTierKillPoints: base de pontos para kills no mesmo grupo.
- PerTierDifferenceBonus / PerTierDifferencePenalty: ajuste por diferença de elo.
- DraculaSlots: número de vagas no topo.
- LevelPenaltyEnabled, LevelGapThreshold, LevelPenaltyPerLevelPercent, LevelPenaltyMaxReductionPercent.
- LevelTrackingMode: "Live" (nível atual) ou "Max" (maior nível observado).
- AssistClanOnlyEnabled: limita assistências ao mesmo clã.

Observações
- Carryover sempre ativo; no primeiro step os pontos não ficam negativos.
- A perda da vítima é o negativo exato do ganho do killer, após regras de elo/nível.

## Comandos (VCF)
Públicos
- `.rank elo | .rk el` — seu elo e pontos.
- `.rank top [n] | .rk tp [n]` — top N por pontos (padrão 10).
- `.rank whoami | .rk id` — seu PlatformId e nome.

Admin/Debug
- `.rank debugelo` | `.rk dbg`
- `.rank debugelo.deaths true|false` | `.rk dbgd true|false`
- `.rank admin.add <PlatformId> <pontos>` | `.rk add <id> <pontos>`
- `.rank admin.set <PlatformId> <pontos>` | `.rk set <id> <pontos>`
- `.rank admin.step <PlatformId> <índice>` | `.rk stp <id> <índice>`
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
