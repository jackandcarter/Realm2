using System;

namespace Client.Building
{
    [Serializable]
    public class PlotPermissionEntry
    {
        public string userId;
        public string permission;
    }

    [Serializable]
    public class PlotPermissionSnapshot
    {
        public string plotId;
        public string ownerUserId;
        public PlotPermissionEntry[] permissions;
    }
}
