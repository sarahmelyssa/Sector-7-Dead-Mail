# Sector 7: Dead Mail

Projeto TP1 Unity.

## Elementos do grupo

- Nome: Sarah Melyssa dos Santos Ramos
- Numero: 33415

## Tema escolhido

- First Person / Escape Room
- Jogo de horror em primeira pessoa com inspeccao de encomendas, corredor escuro, lanterna e anomalia.

## Versao do Unity

- Projeto validado em Unity 6000.3.10f1.
- O enunciado refere Unity 6000.3.9f1; esta versao e muito proxima, mas deve ser confirmada com o professor se for exigida exatamente.

## Descricao curta

Em `Sector 7: Dead Mail`, o jogador trabalha numa sala de processamento postal durante uma unica noite. Cada encomenda deve ser comparada com o relatorio: destino, forma, codigo, logo, fita, peso e outros sinais. O jogador vence ao acertar 10 encomendas. O jogador perde com 3 erros ou se ignorar a anomalia do corredor durante tempo demais.

## Funcionalidades implementadas

- Cena em primeira pessoa com posto de trabalho, mesa, caixa, botoes fisicos, painel frontal e corredor atras do jogador.
- Main Menu, introducao em formato de cassete/briefing, pause, Game Over, Try Again, video final e tela de vitoria.
- Painel de objetivos integrado na parede com:
  - `HORA 00 AM` ate `06 AM`;
  - `PEDIDOS 00/10`;
  - `CAIXA 30s` com tempo regressivo;
  - `ERROS 0/3`;
  - blocos verdes de progresso.
- Sistema de encomendas com movimento lateral, timer por caixa e validacao de aceitar/rejeitar.
- Dificuldade incremental: o tempo de avaliacao diminui progressivamente ate um minimo configurado, e a anomalia fica mais agressiva com o progresso.
- Corredor atras do jogador com rotacao suave da camera, prompt discreto de lanterna e anomalia com olhos/jumpscare.
- Lanterna controlavel apenas quando o jogador olha para tras.
- Sons de ambiente, cassete, voz, botoes, caixa/esteira, acerto, erro, porta, lanterna, corredor, anomalia, jumpscare, musica de menu, pause e telas finais.
- Assets visuais prontos para Main Menu, Pause, Game Over, Win e video final.

## Como jogar

- Rato: olhar dentro do limite da camera.
- `S`: virar lentamente para tras / voltar para a frente.
- `F`: ligar/desligar a lanterna quando estiver olhando para tras.
- `E` ou clique: abrir/fechar o relatorio ou interagir com botao apontado.
- `A` / `D`: rodar a encomenda.
- `Enter`: aceitar a encomenda.
- `Q`: rejeitar a encomenda.
- `Esc`: abrir/fechar Pause.
- `Try Again`: reinicia diretamente a cena jogavel, depois da introducao das fitas.

Objetivo: acertar 10 encomendas antes de acumular 3 erros e antes de a anomalia vencer o jogador.

## Condicoes de fim de jogo

- Vitoria: `PEDIDOS` chega a `10/10`; a gameplay para, toca o video final e depois aparece a tela de vitoria.
- Derrota por erro: 3 decisoes erradas ou timeouts de caixa causam Game Over.
- Derrota por anomalia: ignorar a ameaca do corredor pode ativar o jumpscare e causar Game Over.

## Como abrir o projeto

1. Abrir o Unity Hub.
2. Escolher `Add project from disk`.
3. Selecionar a pasta raiz deste projeto.
4. Abrir com Unity 6000.3.10f1 ou versao compativel.
5. Abrir a cena `Assets/Scenes/MainMenu.unity` para testar desde o menu, ou `Assets/Scenes/SampleScene.unity` para a cena jogavel.
6. Premir `Play`.

## Build Settings

Cenas esperadas no Build Settings:

1. `Assets/Scenes/MainMenu.unity`
2. `Assets/Scenes/SampleScene.unity`

## Assets multimedia

- Imagens PNG para menu principal, pause, Game Over/Try Again, vitoria, relatorios e elementos visuais.
- Video MP4 final em `Assets/Resources/EndGame/final_video.mp4`.
- Sons em MP3/WAV para musica, SFX, cassete, voz, lanterna, botoes, caixa, corredor, porta, erro/acerto e tensao.
- Materiais/texturas do asset `BK_AlchemistHouse` usados para dar acabamento escuro, industrial e envelhecido a sala, painel e corredor.

A estetica combina horror industrial, roxo escuro, cassete/VHS e processamento postal anomalico.

## Requisitos tecnicos visiveis no projeto

- Rigidbody e CapsuleCollider no jogador.
- Rigidbody kinematic e Collider nas encomendas.
- Collider nos botoes fisicos.
- Tags configuradas: `Package`, `InspectionZone`, `Anomaly`, `DecisionButton`, entre outras.
- Uso de SceneManager para Main Menu, Restart, Try Again e telas finais.
- Scripts separados para gestao do jogo, encomendas, UI, audio, camera, lanterna/anomalia e telas finais.

## Observacoes e lacunas conhecidas

- Confirmar no Unity Editor, em Game View, se os hotspots de Pause/Win/Game Over continuam alinhados no monitor usado para gravar/apresentar.
- Repositorio Git local criado para entrega. Antes de submeter, confirmar o push para GitHub, a tag `1.0` remota e preencher o ficheiro Moodle com URL e commit hash final.
