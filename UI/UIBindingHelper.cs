namespace kg.ValheimEnchantmentSystem.UI;

internal static class UIBindingHelper
{
    public static Transform FindRequired(Transform root, string path)
    {
        Transform target = root.Find(path);
        if (target == null)
        {
            throw new InvalidOperationException($"UI binding path not found: {path}");
        }

        return target;
    }

    public static Transform? FindOptional(Transform root, string path) => root.Find(path);

    public static T GetRequired<T>(Transform root, string path) where T : Component
    {
        T component = FindRequired(root, path).GetComponent<T>();
        if (component == null)
        {
            throw new InvalidOperationException($"UI component not found at path: {path} ({typeof(T).Name})");
        }

        return component;
    }

    public static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
    }
}
