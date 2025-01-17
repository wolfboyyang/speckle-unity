﻿using Objects.Converter.Unity;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sentry;
using Sentry.Protocol;
using Speckle.Core.Kits;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
  /// <summary>
  /// A Speckle Sender, it's a wrapper around a basic Speckle Client
  /// that handles conversions for you
  /// </summary>
  public class Sender : MonoBehaviour
  {

    private ServerTransport transport;

    /// <summary>
    /// Converts and sends the data of the last commit on the Stream
    /// </summary>
    /// <param name="streamId">ID of the stream to send to</param>
    /// <param name="gameObjects">List of gameObjects to convert and send</param>
    /// <param name="account">Account to use. If not provided the default account will be used</param>
    /// <param name="branchName">Name of branch to send to</param>
    /// <param name="createCommit">When true, will create a commit using the root object</param>
    /// <param name="onDataSentAction">Action to run after the data has been sent</param>
    /// <param name="onProgressAction">Action to run when there is download/conversion progress</param>
    /// <param name="onErrorAction">Action to run on error</param>
    /// <exception cref="SpeckleException"></exception>
    public void Send(string streamId,
      List<GameObject> gameObjects,
      Account account = null,
      string branchName = "main",
      bool createCommit = true,
      Action<string> onDataSentAction = null,
      Action<ConcurrentDictionary<string, int>> onProgressAction = null,
      Action<string, Exception> onErrorAction = null)
    {
      try
      {
        var data = ConvertRecursivelyToSpeckle(gameObjects);
        var client = new Client(account ?? AccountManager.GetDefaultAccount());
        transport = new ServerTransport(client.Account, streamId);

        Task.Run(async () =>
        {
          var res = await Operations.Send(
            data,
            new List<ITransport>() { transport },
            useDefaultCache: true,
            disposeTransports: true,
            onProgressAction: onProgressAction,
            onErrorAction: onErrorAction
          );

          Analytics.TrackEvent(client.Account, Analytics.Events.Send);

          if (createCommit)
          {
            await client.CommitCreate(
              new CommitCreateInput
              {
                streamId = streamId,
                branchName = branchName,
                objectId = res,
                message = "No message",
                sourceApplication = VersionedHostApplications.Unity,
              });
          }
          
          transport?.Dispose();
          onDataSentAction?.Invoke(res);
        });
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e, true, SentryLevel.Error);
      }
    }

    private void OnDestroy()
    {
      transport?.Dispose();
    }

    #region private methods

    private Base ConvertRecursivelyToSpeckle(List<GameObject> gos)
    {
      if (gos.Count == 1)
      {
        return RecurseTreeToNative(gos[0]);
      }

      var @base = new Base();
      @base["objects"] = gos.Select(x => RecurseTreeToNative(x)).Where(x => x != null).ToList();
      return @base;
    }

    private Base RecurseTreeToNative(GameObject go)
    {
      var converter = new ConverterUnity();
      if (converter.CanConvertToSpeckle(go))
      {
        try
        {
          return converter.ConvertToSpeckle(go);
        }
        catch (Exception e)
        {
          Debug.LogError(e);
          return null;
        }
      }

      if (go.transform.childCount > 0)
      {
        var @base = new Base();
        var objects = new List<Base>();
        for (var i = 0; i < go.transform.childCount; i++)
        {
          var goo = RecurseTreeToNative(go.transform.GetChild(i).gameObject);
          if (goo != null)
            objects.Add(goo);
        }

        if (objects.Any())
        {
          @base["objects"] = objects;
          return @base;
        }
      }

      return null;
    }

    #endregion
  }
}