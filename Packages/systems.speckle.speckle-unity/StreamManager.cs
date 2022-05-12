using System;
using System.Collections;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System.Collections.Generic;
using JetBrains.Annotations;
using Speckle.ConnectorUnity.Converter;
using Speckle.Core.Models;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
  [ExecuteAlways]
  [AddComponentMenu("Speckle/Stream Manager")]
  public class StreamManager : MonoBehaviour
  {

    public int SelectedAccountIndex = -1;
    public int SelectedStreamIndex = -1;
    public int SelectedBranchIndex = -1;
    public int SelectedCommitIndex = -1;
    public int OldSelectedAccountIndex = -1;
    public int OldSelectedStreamIndex = -1;

    public Client Client;
    public Account SelectedAccount;
    public Stream SelectedStream;

    public List<Account> Accounts;
    public List<Stream> Streams;
    public List<Branch> Branches;


#if UNITY_EDITOR
    public static bool GenerateMaterials = false;
#endif

    [ItemCanBeNull]
    public List<GameObject> ConvertRecursivelyToNative(Base @base, string id)
    {

      var rc = GetComponent<ConverterComponent>();
      if (rc == null)
        rc = gameObject.AddComponent<ConverterComponent>();

      var rootObject = new GameObject()
      {
          name = id,
      };
          
      return rc.RecursivelyConvertToNative(@base, rootObject.transform);
    }
    
#if UNITY_EDITOR
    [ContextMenu("Open Speckle Stream in Browser")]
    protected void OpenUrlInBrowser()
    {
        string url = $"{SelectedAccount.serverInfo.url}/streams/{SelectedStream.id}/commits/{Branches[SelectedBranchIndex].commits.items[SelectedCommitIndex].id}";
        Application.OpenURL(url);
    }
#endif

  }
}