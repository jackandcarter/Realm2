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

#if UNITY_EDITOR
        protected virtual void OnEnable()
        {
            EnsureHistoryCreated();
        }

        protected virtual void OnValidate()
        {
            EnsureHistoryCreated();
            History.RecordModification(CurrentUserName);
            EditorUtility.SetDirty(this);
        }

        internal void RecordCloneCreation()
        {
            EnsureHistoryCreated();
            History.RecordCreation(CurrentUserName);
            EditorUtility.SetDirty(this);
        }

        internal void RecordManualModification()
        {
            EnsureHistoryCreated();
            History.RecordModification(CurrentUserName);
            EditorUtility.SetDirty(this);
        }

        private void EnsureHistoryCreated()
        {
            if (history == null)
            {
                history = new AssetHistoryMetadata();
            }

            history.EnsureCreated(CurrentUserName);
        }

        private static string CurrentUserName
        {
            get
            {
                var name = Environment.UserName;
                return string.IsNullOrWhiteSpace(name) ? "Unknown" : name.Trim();
            }
        }
#else
        protected virtual void OnEnable()
        {
            if (history == null)
            {
                history = new AssetHistoryMetadata();
            }
        }

        protected virtual void OnValidate()
        {
            if (history == null)
            {
                history = new AssetHistoryMetadata();
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
