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
- O projeto tambem deve abrir em versoes proximas da linha Unity 6, caso o ambiente de avaliacao use uma revisao compativel.

## Descricao

`Sector 7: Dead Mail` coloca o jogador num turno noturno dentro de uma sala de processamento postal. Cada encomenda deve ser comparada com o respetivo relatorio, verificando destino, formato, codigo de barras, logotipo, fita, peso e outros sinais.

O objetivo e processar 10 encomendas corretamente antes de cometer 3 erros. Durante o turno, a anomalia no corredor atras do jogador torna-se mais ativa e obriga o jogador a prestar atencao aos sons, virar para tras e usar a lanterna quando necessario.

## Funcionalidades implementadas

- Menu principal, briefing em formato de fita/cassete, pause, game over, try again, video final e tela de vitoria.
- Cena jogavel em primeira pessoa com posto de trabalho, mesa, botoes fisicos, painel frontal e corredor atras do jogador.
- Sistema de encomendas com entrada lateral, movimento de esteira, validacao de aceitar/rejeitar e relatorios de inspeccao.
- Primeira encomenda sempre virada de frente para facilitar a aprendizagem; depois, as encomendas podem aparecer de frente ou de lado para justificar o uso da rotacao.
- Painel frontal integrado na parede com hora, pedidos, tempo da encomenda, vidas e blocos verdes de progresso.
- Timer por encomenda com dificuldade progressiva: o tempo comeca maior e vai diminuindo ate ao minimo configurado.
- Rotacao suave da camera ao olhar para tras.
- Lanterna disponivel apenas quando o jogador esta virado para o corredor.
- Anomalia com dois tipos de alerta: batidas para procurar a criatura no corredor e vozes/olhos brilhantes para encontrar com a lanterna.
- Sistema de derrota por 3 erros, timeout de encomenda ou jumpscare da anomalia.
- Sistema de vitoria ao completar 10 pedidos, com transicao para video final e tela de vitoria.
- Audio de ambiente, menu, fita, botoes, relatorios, esteira, acerto, erro, lanterna, anomalia, jumpscare, pause e telas finais.

## Como jogar

- Rato: mover o olhar dentro dos limites da camera.
- `S`: virar lentamente para tras ou voltar para a frente.
- `F`: ligar/desligar a lanterna quando estiver olhando para tras.
- `E` ou clique: abrir/fechar o relatorio ou interagir com o botao apontado.
- `A` / `D`: rodar a encomenda.
- `Enter`: aceitar a encomenda.
- `Q`: rejeitar a encomenda.
- `Esc`: abrir/fechar pause.

Objetivo: acertar 10 encomendas antes de acumular 3 erros e antes de a anomalia vencer o jogador.

## Condicoes de fim de jogo

- Vitoria: o contador de pedidos chega a `10/10`; a gameplay para, toca o video final e depois aparece a tela de vitoria.
- Derrota por erro: 3 decisoes erradas ou timeouts de encomenda causam game over.
- Derrota por anomalia: ignorar os alertas do corredor pode ativar o jumpscare e causar game over.
- Try Again: reinicia diretamente a cena jogavel depois da introducao das fitas.

## Como abrir o projeto

1. Abrir o Unity Hub.
2. Escolher `Add project from disk`.
3. Selecionar a pasta raiz deste projeto.
4. Abrir com Unity 6000.3.10f1 ou versao compativel.
5. Abrir a cena `Assets/Scenes/MainMenu.unity` para testar desde o menu.
6. Em alternativa, abrir `Assets/Scenes/SampleScene.unity` para testar diretamente a cena jogavel.
7. Premir `Play`.

## Build Settings

Cenas esperadas no Build Settings:

1. `Assets/Scenes/MainMenu.unity`
2. `Assets/Scenes/SampleScene.unity`

## Assets

- Imagens PNG para menu principal, pause, game over, try again, vitoria, relatorios e elementos visuais.
- Video final em `Assets/Resources/EndGame/final_video.mp4`.
- Sons em MP3/WAV para musica, efeitos, fita, voz, lanterna, botoes, caixa, corredor, porta, erro/acerto e tensao.
- Materiais e objetos criados/organizados para manter a estetica de horror industrial, sala escura, roxo frio e processamento postal anomalico.

## Estrutura principal

- `Assets/Scripts/GameManager.cs`: controla vitoria, derrota, erros, estado geral e atalhos de teste.
- `Assets/Scripts/InspectionStation.cs`: controla o fluxo das encomendas, timer de avaliacao e acoes do jogador.
- `Assets/Scripts/PackageConveyor.cs`: controla entrada, saida, movimento e rotacao inicial das encomendas.
- `Assets/Scripts/ReportPanel.cs`: controla abertura e paginas dos relatorios.
- `Assets/Scripts/StationStatusMonitor.cs`: atualiza o painel frontal, pedidos, hora, vidas e tempo da encomenda.
- `Assets/Scripts/CorridorFlashlightAnomalyController.cs`: controla lanterna, corredor, olhos, criatura, sons e jumpscare.
- `Assets/Scripts/AudioManager.cs`: centraliza efeitos sonoros e loops da gameplay.
- `Assets/Scripts/BackgroundMusicManager.cs`: controla musica de menu, briefing, pause, telas finais e fase.
- `Assets/Scripts/UIManager.cs`: controla menu principal, pause, briefing, conclusoes e telas de interface.

## Requisitos tecnicos visiveis no projeto

- Rigidbody e CapsuleCollider no jogador.
- Rigidbody kinematic e Collider nas encomendas.
- Collider nos botoes fisicos.
- Tags configuradas para encomendas, zonas de inspecao, anomalia e botoes de decisao.
- Uso de SceneManager para menu principal, restart, try again e telas finais.
- Scripts separados para gestao do jogo, encomendas, UI, audio, camera, lanterna/anomalia e telas finais.

## Estado de entrega

- Projeto pronto para avaliacao.
- Gameplay principal completa com uma noite jogavel.
- Repositorio Git atualizado no GitHub.
- Builds de verificacao dos projetos `Sector13.Runtime` e `Sector13.EditMode.Tests` concluida sem erros.
