using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Logging;
using Realms;

namespace Speckle.Core.Transports
{
    /// <summary>
    /// A Realm storage of speckle objects.
    /// </summary>
    public class RealmTransport : ITransport, IDisposable, ICloneable
    {
        //public Dictionary<string, string> Objects;

        public CancellationToken CancellationToken { get; set; }

        public string TransportName { get; set; } = "Realm";

        public Action<string, int> OnProgressAction { get; set; }

        public Action<string, Exception> OnErrorAction { get; set; }

        public int SavedObjectCount { get; set; } = 0;

        private RealmConfiguration Config { get; set; } = new RealmConfiguration("speckle.realm");

        public RealmTransport()
        {
            Log.AddBreadcrumb("New Realm Transport");
        }

        /// <summary>
        /// Delete the local data
        /// </summary>
        public void Delete()
        {
            Realm.DeleteRealm(Config);
        }

        public void BeginWrite()
        {
            SavedObjectCount = 0;
        }

        public void EndWrite() { }

        async public void SaveObject(string hash, string serializedObject)
        {
            if (CancellationToken.IsCancellationRequested) return; // Check for cancellation

            using var realm = await Realm.GetInstanceAsync(Config);
            await realm.WriteAsync(() =>
                realm.Add(new SpeckleObject { Hash = hash, SerializedObject = serializedObject })
            );
            SavedObjectCount++;
            OnProgressAction?.Invoke(TransportName, 1);
        }

        public void SaveObject(string id, ITransport sourceTransport)
        {
            throw new NotImplementedException();
        }

        public string GetObject(string hash)
        {
            if (CancellationToken.IsCancellationRequested) return null; // Check for cancellation
            using var realm = Realm.GetInstance(Config);
            var obj = realm.Find<SpeckleObject>(hash);
            if (obj != null)
                return obj.SerializedObject;
            else
                return null;
        }

        public Task<string> CopyObjectAndChildren(string id, ITransport targetTransport, Action<int> onTotalChildrenCountKnown = null)
        {
            throw new NotImplementedException();
        }

        public bool GetWriteCompletionStatus()
        {
            return true; // can safely assume it's always true, as ops are atomic?
        }

        public Task WriteComplete()
        {
            return Utilities.WaitUntil(() => true);
        }

        public override string ToString()
        {
            return $"Realm Transport {TransportName}";
        }

        public async Task<Dictionary<string, bool>> HasObjects(List<string> objectIds)
        {
            Dictionary<string, bool> ret = new Dictionary<string, bool>();
            using var realm = await Realm.GetInstanceAsync(Config);
            foreach (string objectId in objectIds)
            {
                ret[objectId] = realm.Find<SpeckleObject>(objectId) != null;
            }
            return ret;
        }

        public void Dispose()
        {
            OnErrorAction = null;
            OnProgressAction = null;
            SavedObjectCount = 0;
        }

        public object Clone() => new RealmTransport()
        {
            TransportName = TransportName,
            OnErrorAction = OnErrorAction,
            OnProgressAction = OnProgressAction,
            CancellationToken = CancellationToken,
            Config = Config,
            SavedObjectCount = SavedObjectCount
        };
    }
}
