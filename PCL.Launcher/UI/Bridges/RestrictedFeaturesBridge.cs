namespace PCL.UI.Bridges;

/// <summary>功能限制桥接：本分支不启用基于设备地区的本地功能限制。</summary>
public sealed class RestrictedFeaturesBridge : IRestrictedFeatures
{
    public bool IsRestricted => false;

    public bool Check(string feature) => true;

    public void ThrowIfRestricted(string feature)
    {
    }
}
