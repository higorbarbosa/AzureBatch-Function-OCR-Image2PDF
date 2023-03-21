using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

public static void Run(Stream myBlob, string name, ILogger log)
{
    BatchSharedKeyCredentials credentials = new BatchSharedKeyCredentials(Environment.GetEnvironmentVariable("batchEndpoint"), 
                                                                          Environment.GetEnvironmentVariable("batchAccount"), 
                                                                          Environment.GetEnvironmentVariable("batchKey"));

    string batchJob =                           Environment.GetEnvironmentVariable("batchJob");
    string inputContainerName =                 Environment.GetEnvironmentVariable("inputContainerName");
    string InputContainerConnectionString =     Environment.GetEnvironmentVariable("batchstoragetraining123_STORAGE");
    string OutputContainerSAS =                 Environment.GetEnvironmentVariable("OutputContainerSAS");

    using (BatchClient batchClient = BatchClient.Open(credentials))
    {
        CloudJob job = batchClient.JobOperations.GetJob(batchJob);
        job.Commit();

        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(InputContainerConnectionString);
        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

        CloudBlobContainer container = blobClient.GetContainerReference(inputContainerName);

        List<ResourceFile> inputFiles = new List<ResourceFile>();
        
        SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
        {
            SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
            Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List
        };

        string sasToken = container.GetSharedAccessSignature(sasConstraints);
        string containerSasUrl = String.Format("{0}{1}", container.Uri, sasToken);

        inputFiles.Add(ResourceFile.FromStorageContainerUrl(containerSasUrl));
        
        List<CloudTask> tasks = new List<CloudTask>();
        string inputFilename = Path.GetFileNameWithoutExtension(name);
        string outputPdfFilename = "ocr-" + Path.GetFileNameWithoutExtension(name) + ".pdf";

        log.LogInformation($"Nome da Imagem no container input: \"{name}\"");
        log.LogInformation($"Nome do PDF que sera gerado no contaienr output: \"{outputPdfFilename}\"");

        string uniqueIdentifier = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");

        string taskId = String.Format(inputFilename.Replace(".", string.Empty) + "-" + uniqueIdentifier);
        
        string taskCommandLine = String.Format("/bin/bash -c \"sudo -S ocrmypdf --image-dpi 300 {0} {1}\"", name, outputPdfFilename);
        
        CloudTask task = new CloudTask(taskId, taskCommandLine);
        task.UserIdentity = new UserIdentity(new AutoUserSpecification(elevationLevel: ElevationLevel.Admin, scope: AutoUserScope.Task));

        List<OutputFile> outputFileList = new List<OutputFile>();
        OutputFileBlobContainerDestination outputContainer = new OutputFileBlobContainerDestination(OutputContainerSAS);
       
        OutputFile outputFilePdf = new OutputFile(outputPdfFilename, new OutputFileDestination(outputContainer), new OutputFileUploadOptions(OutputFileUploadCondition.TaskSuccess));
        outputFileList.Add(outputFilePdf);

        task.ResourceFiles = new List<ResourceFile> { inputFiles[0] };
        task.OutputFiles = outputFileList;
        tasks.Add(task);

        batchClient.JobOperations.AddTask(batchJob, tasks);
        log.LogInformation($"Adicionando tarefa:  \"{taskId}\" para  \"{inputFilename}\" ({myBlob.Length} bytes)...");
    }
}
