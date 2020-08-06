# POSTGhost
ASP.NET Core Service that records requests for security testing.

This tool is for security research only!  The service will record any calls made to it for analysis and security research.

Steps:

- Get an Azure Storage account and an Azure KeyVault
- Create 2 containers (requests, bodys)
- Get SAS material for the account, it needs to be able to write/add/create.
- Put your SAS token in the KeyVault as a secret named "sasToken"
  - This is the SAS token type that starts with `SharedAccessSignature=` and ends with `BlobEndpoint=https://<yourstorageacct>.blob.core.windows.net/;`
- Publish this project to an Azure AppService
  - https://docs.microsoft.com/en-US/visualstudio/deployment/quickstart-deploy-to-azure?view=vs-2019
- Enable MSI on the AppService
  - https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet
- Grant your AppService MSI permissions to read the KeyVault with a new Access Policy
  - It needs "Get" permissions
  - Make sure you actually save the policy once you add it...
- Use a KeyVault reference in the AppService's app settings to "sasToken"
  - like this
  `{
    "name": "sasToken",
    "value": "@Microsoft.KeyVault(SecretUri=https://<yourVault>.vault.azure.net/secrets/sasToken/3d8cfc6<secretVersion>789519)",
    "slotSetting": false
  },`
  - https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references
  - Changing this app setting will reboot the site, which will then have access to the KeyVault secret
- That should be it.  Visit your app service in a browser, postman, or ... whatever else ...

Further:
- Run these commands in your ADX/Kusto database of your choice and you can now see your requests in Kusto :D
  - Get a URL SAS token for the requests and bodys containers; they will need list/read permissions.

`.create external table PostGhostRequests (Data:string)  
kind=blob 
dataformat=tsv 
( 
   h@'https://<storageaccount>.blob.core.windows.net/requests?<fullSASMaterialHere>' 
) `

and

`.create external table PostGhostBodys (Data:string)  
kind=blob 
dataformat=tsv 
( 
   h@'https://<storageaccount>.blob.core.windows.net/bodys?<fullSASMaterialHere>' 
) `
