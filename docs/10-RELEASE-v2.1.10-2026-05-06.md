# Release v2.1.10 - 2026-05-06

## Resumo

Esta release foca no fluxo de save manual e sync cloud do projeto. O botao de sincronizacao ficou mais claro, mais seguro e mais util para o usuario.

## O que mudou neste push

- o save manual agora valida o bundle do projeto antes de sincronizar com a nuvem
- o sync passa a incluir os circuitos personalizados da sessao atual, com mensagem de status mais completa
- os erros de sync ficaram mais claros quando existe projeto desincronizado, circuito faltando ou referencia invalida
- o botao `SYNC` ganhou cooldown visual de 10 segundos depois de cada execucao
- o feedback visual de status ficou mais duradouro para permitir leitura de listas maiores de circuitos sincronizados
- a numeracao do app foi atualizada para `v2.1.10`

## Artefatos desta release

- `Builds/Release/DigitalLogicSim-Unifil-Setup-v2.1.10.exe`
- `Builds/Release/Digital-Logic-Sim-Unifil-Windows-v2.1.10.zip`
- `Builds/Release/Digital-Logic-Sim-Unifil-Linux-v2.1.10.zip`

## Observacoes

- o instalador Windows segue o padrao atual do repositorio com Inno Setup
- o pacote Linux inclui um `README-Linux.txt` com instrucoes de execucao basicas
- esta release foi preparada para acompanhar os ultimos commits locais antes do push
