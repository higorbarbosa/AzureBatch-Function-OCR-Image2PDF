# AzureBatch-Function-OCR-Image2PDF
Exemplo de uso do Azure Batch para Processamento de Imagens com OCR usando Azure Function Apps + Blob Trigger.

# Configuracao

Adicionar em Settings > Configuration

batchAccount = nome da Conta no Azure Batch

batchEndpoint = Endpoint do Azure Batch Account

batchJob = nome do Job criado para o processamento

batchKey = chave do Azure Batch Account

inputContainerName = container do Azure Blob Storage para entrada de arquivos (imagens)

OutputContainerSAS =  SAS token da pasta de saida do Azure Blob Storage (pdfs)

