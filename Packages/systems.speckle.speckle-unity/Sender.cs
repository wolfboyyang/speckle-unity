#nullable enable
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Sentry;
using Speckle.ConnectorUnity.Converter;
using Speckle.Core.Kits;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    /// <summary>
    /// A Speckle Sender, it's a wrapper around a basic Speckle Client
    /// that handles conversions for you
    /// </summary>
    [RequireComponent(typeof(ConverterComponent))]
    public class Sender : MonoBehaviour
    {
        [field: SerializeField]
        public ConverterComponent Converter { get; protected set; } = default!;

        public Account account;
        


        private void Awake()
        {
            Converter = GetComponent<ConverterComponent>();
        }

        public void SendChildren(string streamId,
            Account? account = null,
            Action<string>? onDataSentAction = null,
            Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
            Action<string, Exception>? onErrorAction = null)
        {
            Base data = Converter.RecursivelyConvertToSpeckle(this.gameObject, o => Converter.ConverterInstance.CanConvertToSpeckle(o));
            Send(streamId, data, account, onDataSentAction, onProgressAction, onErrorAction);
        }



#region Static Members
        
        /// <summary>
        /// Converts and sends the data of the last commit on the Stream
        /// </summary>
        /// <param name="streamId">ID of the stream to send to</param>
        /// <param name="data">Objects to send</param>
        /// <param name="account">Account to use. If not provided the default account will be used</param>
        /// <param name="onDataSentAction">Action to run after the data has been sent</param>
        /// <param name="onProgressAction">Action to run when there is download/conversion progress</param>
        /// <param name="onErrorAction">Action to run on error</param>
        /// <exception cref="SpeckleException"></exception>
        public static void Send(string streamId, Base data,
            Account? account = null,
            Action<string>? onDataSentAction = null,
            Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
            Action<string, Exception>? onErrorAction = null)
        {
            try
            {
                Task.Run(async () =>
                {
                    var res = await Helpers.Send(streamId, data, "Data from unity!",
                        sourceApplication: VersionedHostApplications.Unity,
                        account: account,
                        onErrorAction: onErrorAction,
                        onProgressAction: onProgressAction);

                    onDataSentAction?.Invoke(res);
                });
            }
            catch (Exception e)
            {
                throw new SpeckleException(e.Message, e, true, SentryLevel.Error);
            }
        }
    }
#endregion
}