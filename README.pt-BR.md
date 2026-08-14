<div align="center">
  <img src="assets/branding/Icon-Baru-Board.svg" width="96" alt="Baru Board">
  <h1>Baru Board</h1>
  <p>Um quadro branco de desktop, local e offline, para pensar visualmente.</p>
  <p><a href="README.md">English</a></p>
</div>

## Sobre

O Baru Board é um aplicativo desktop open source para rascunhar, diagramar e
organizar ideias em um canvas praticamente infinito. Os quadros ficam em um
arquivo na sua máquina e o aplicativo funciona totalmente offline.

Foi construído com C#, .NET 10 e [Avalonia](https://avaloniaui.net). O canvas é
desenhado diretamente pelo `DrawingContext` do Avalonia em um controle próprio:
os elementos vivem em coordenadas de mundo e são renderizados por um único
renderer, em vez de existirem como controles de interface individuais.

## Funcionalidades

- Canvas infinito com pan e zoom centrado no cursor
- Retângulos, elipses, linhas e setas
- Texto com edição no próprio quadro
- Desenho livre com suavização de traço, e borracha para a tinta
- Imagens importadas do disco ou soltas sobre o quadro
- Seleção simples, múltipla e por área
- Mover, redimensionar imagens com proporção, alinhar e distribuir
- Grade adaptativa com encaixe opcional
- Desfazer e refazer sem limite prático
- Copiar, colar e duplicar, inclusive entre quadros
- Salvamento automático com recuperação após queda
- Exportação em PNG do quadro inteiro, da seleção ou da área visível
- Arquivos recentes

## Situação atual

A versão 1.0.0 é a primeira publicação pública do código-fonte. O aplicativo está
funcional e coberto por uma suíte automatizada de testes, mas ainda não há
binários prontos para download — veja [Executando a partir do código](#executando-a-partir-do-código).

## Requisitos

- [SDK do .NET 10](https://dotnet.microsoft.com/download) ou superior
- Windows ou Linux

## Executando a partir do código

```bash
git clone https://github.com/LCASouza/Baru-Board.git
cd Baru-Board
dotnet run --project src/BaruBoard.App
```

## Uso básico

Os quadros começam vazios. Escolha uma ferramenta na barra superior, desenhe no
canvas e salve o quadro em um arquivo `.baru`.

- **Criar**: selecione uma ferramenta de forma e arraste no canvas. Um clique
  simples cria o elemento com tamanho padrão.
- **Selecionar**: com a ferramenta de seleção, clique em um elemento, segure
  <kbd>Shift</kbd> ou <kbd>Ctrl</kbd> para somar ou remover da seleção, ou
  arraste no espaço vazio para selecionar tudo que o retângulo tocar.
- **Mover e redimensionar**: arraste um elemento selecionado, ou use as alças que
  aparecem quando há um único elemento selecionado. Imagens mantêm a proporção.
- **Editar texto**: dê um duplo clique em um texto. Confirme com
  <kbd>Ctrl</kbd>+<kbd>Enter</kbd> ou clicando fora, cancele com <kbd>Esc</kbd>.
- **Salvar e abrir**: <kbd>Ctrl</kbd>+<kbd>S</kbd> e <kbd>Ctrl</kbd>+<kbd>O</kbd>.
- **Exportar**: <kbd>Ctrl</kbd>+<kbd>E</kbd>, escolhendo a região, a escala e se o
  fundo deve ser transparente.

## Ferramentas

| Ferramenta | Atalho | Comportamento |
| --- | --- | --- |
| Selecionar | <kbd>V</kbd> | Selecionar, mover, redimensionar e excluir elementos |
| Retângulo | <kbd>R</kbd> | Arraste para desenhar um retângulo |
| Elipse | <kbd>O</kbd> | Arraste para desenhar uma elipse |
| Linha | <kbd>L</kbd> | Arraste para desenhar uma linha reta |
| Seta | <kbd>A</kbd> | Arraste para desenhar uma seta |
| Texto | <kbd>T</kbd> | Clique para criar um texto e comece a digitar |
| Caneta | <kbd>P</kbd> | Desenha traços à mão livre |
| Borracha | <kbd>E</kbd> | Remove os traços à mão livre sob o cursor |

## Navegação

| Ação | Entrada |
| --- | --- |
| Pan | Arrastar com o botão do meio, ou <kbd>Space</kbd> + botão esquerdo |
| Zoom | Roda do mouse, centrada no ponteiro |
| Ajustar o quadro à janela | <kbd>Ctrl</kbd>+<kbd>0</kbd> |
| Zoom em 100% | <kbd>Ctrl</kbd>+<kbd>1</kbd> |

## Atalhos de teclado

| Ação | Atalho |
| --- | --- |
| Novo quadro | <kbd>Ctrl</kbd>+<kbd>N</kbd> |
| Abrir | <kbd>Ctrl</kbd>+<kbd>O</kbd> |
| Salvar | <kbd>Ctrl</kbd>+<kbd>S</kbd> |
| Salvar como | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>S</kbd> |
| Exportar PNG | <kbd>Ctrl</kbd>+<kbd>E</kbd> |
| Desfazer | <kbd>Ctrl</kbd>+<kbd>Z</kbd> |
| Refazer | <kbd>Ctrl</kbd>+<kbd>Y</kbd> ou <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Z</kbd> |
| Copiar / Colar / Duplicar | <kbd>Ctrl</kbd>+<kbd>C</kbd> / <kbd>Ctrl</kbd>+<kbd>V</kbd> / <kbd>Ctrl</kbd>+<kbd>D</kbd> |
| Selecionar tudo | <kbd>Ctrl</kbd>+<kbd>A</kbd> |
| Limpar seleção | <kbd>Esc</kbd> |
| Excluir seleção | <kbd>Delete</kbd> ou <kbd>Backspace</kbd> |
| Suspender o encaixe na grade | Segurar <kbd>Alt</kbd> ao arrastar |

## Formato de arquivo

Os quadros são salvos em arquivos `.baru`. Um quadro é um container zip que
contém o `board.json` e as imagens referenciadas por ele:

```text
quadro.baru
├── board.json
└── assets/
    └── <sha256>.png
```

O `board.json` traz o campo `formatVersion`, os metadados do quadro, o viewport
salvo e a lista de elementos. Os assets são endereçados pelo SHA-256 do próprio
conteúdo, verificado na abertura do arquivo. Arquivos gravados em versões
anteriores do formato continuam sendo lidos.

## Local e privado

O Baru Board não tem contas, servidores nem acesso à rede. Os quadros são
arquivos comuns que pertencem a você, e os dados do aplicativo — como arquivos
recentes e cópias de recuperação — ficam no diretório de perfil do seu usuário.

## Compilando

```bash
dotnet build
```

## Executando os testes

```bash
dotnet test
```

O projeto possui uma suíte automatizada de testes cobrindo a geometria, a
matemática do viewport, os comandos de edição, o formato de arquivo e os
cálculos de exportação.

## Roadmap

Direções planejadas, sem compromisso de data:

- Cores dos elementos e mais opções de estilo de texto
- Colar imagens da área de transferência do sistema
- Notas adesivas e cartões de checklist
- Empacotamento e publicação de versões desktop para Windows e Linux

## Contribuindo

Issues e pull requests são bem-vindos. Antes de abrir um pull request, verifique
que `dotnet build` e `dotnet test` passam.

## Licença

Distribuído sob a [Licença MIT](LICENSE).

## Idioma

[English](README.md)
