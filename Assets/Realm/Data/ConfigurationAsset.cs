using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Realm.Data
{
    public abstract class ConfigurationAsset : ScriptableObject
    {
        [SerializeField]
        private AssetHistoryMetadata history = new AssetHistoryMetadata();

        public AssetHistoryMetadata History
        {
            get
            {
                if (history == null)
                {
                    history = new AssetHistoryMetadata();
                }

                return history;
            }
        }

        protected virtual void OnEnable()
        {
            EnsureHistoryInitialized();
        }

        protected virtual void OnValidate()
        {
            EnsureHistoryInitialized();

#if UNITY_EDITOR
            History.RecordModification(CurrentUserName);
            EditorUtility.SetDirty(this);
#endif
        }

        public void RecordCloneCreation()
        {
            EnsureHistoryInitialized();

#if UNITY_EDITOR
            History.RecordCreation(CurrentUserName);
            EditorUtility.SetDirty(this);
#endif
        }

        public void RecordManualModification()
        {
            EnsureHistoryInitialized();

#if UNITY_EDITOR
            History.RecordModification(CurrentUserName);
            EditorUtility.SetDirty(this);
#endif
        }

        private void EnsureHistoryInitialized()
        {
            if (history == null)
            {
                history = new AssetHistoryMetadata();
            }

#if UNITY_EDITOR
            history.EnsureCreated(CurrentUserName);
#endif
        }

#if UNITY_EDITOR
        private static string CurrentUserName
        {
            get
            {
                var name = Environment.UserName;
                return string.IsNullOrWhiteSpace(name) ? "Unknown" : name.Trim();
            }
        }
#endif
    }

    [Serializable]
    public class AssetHistoryMetadata
    {
        [SerializeField]
        private string createdBy;

        [SerializeField]
        private string createdUtc;

        [SerializeField]
        private string modifiedBy;

        [SerializeField]
        private string modifiedUtc;

        public string CreatedBy => createdBy;
        public string CreatedUtc => createdUtc;
        public string ModifiedBy => modifiedBy;
        public string ModifiedUtc => modifiedUtc;

#if UNITY_EDITOR
        public void EnsureCreated(string userName)
        {
            if (!string.IsNullOrEmpty(createdUtc))
            {
                return;
            }

            RecordCreation(userName);
        }

        public void RecordCreation(string userName)
        {
            var resolved = ResolveUserName(userName);
            var timestamp = DateTime.UtcNow.ToString("O");
            createdBy = resolved;
            createdUtc = timestamp;
            modifiedBy = resolved;
            modifiedUtc = timestamp;
        }

        public void RecordModification(string userName)
        {
            if (string.IsNullOrEmpty(createdUtc))
            {
                RecordCreation(userName);
                return;
            }

            modifiedBy = ResolveUserName(userName);
            modifiedUtc = DateTime.UtcNow.ToString("O");
        }

        private static string ResolveUserName(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
        }
#endif
    }
}
