using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

public class ModuleConfig : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.Dependencies.AddSingleton(GameApiClient.Create());
    }
}

public class MyModule
{
    private ILogger<MyModule> logger;
    private IGameApiClient gameApiClient;

    public MyModule(ILogger<MyModule> logger, IGameApiClient gameApiClient)
    {
        this.logger = logger;
        this.gameApiClient = gameApiClient;
    }

    [CloudCodeFunction("SavePlayerData")]
    public async void SavePlayerData(IExecutionContext context, string nickname)
    {
        try
        {
            SetItemBody setItemBody = new SetItemBody("Nickname", nickname);
            await gameApiClient.CloudSaveData.SetPublicItemAsync(
                context, context.AccessToken, context.ProjectId, context.PlayerId!, setItemBody);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
        }
    }

    [CloudCodeFunction("GetPlayerData")]
    public async Task<string> GetPlayerData(IExecutionContext context, string playerId)
    {
        List<string> keyList = new List<string> { "Nickname" };
        var response = await gameApiClient.CloudSaveData.GetPublicItemsAsync(
            context, context.AccessToken, context.ProjectId, playerId, keyList);
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            logger.LogError(response.ErrorText);
            return string.Empty;
        }

        Item? item = response.Data.Results.FirstOrDefault(item => item.Key == "Nickname");
        return item?.Value.ToString() ?? string.Empty;
    }
}


