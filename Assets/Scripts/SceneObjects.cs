using UnityEngine;

// Helpers for building the game's objects. Each one looks for an existing child first, so the same code
// either reuses the objects saved in the scene (see Tools > Keep the Ball Up > Build Scene) or creates any that are missing.
// Callers only set up objects that were just created, so changes made in the Inspector are kept.
public static class SceneObjects
{
    // Finds the child called 'name' under parent, or creates it with these components. Returns true if it was just created.
    public static bool GetOrCreate(Transform parent, string name, out GameObject go, params System.Type[] components)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        if (existing != null)
        {
            go = existing.gameObject;
            return false;
        }

        go = new GameObject(name, components);
        if (parent != null) go.transform.SetParent(parent, false);
        return true;
    }

    // Same, and also gets (or adds) a component on it
    public static bool GetOrCreate<T>(Transform parent, string name, out T component) where T : Component
    {
        bool created = GetOrCreate(parent, name, out GameObject go);
        component = GetOrAdd<T>(go);
        return created;
    }

    public static T GetOrAdd<T>(GameObject go) where T : Component
    {
        return go.TryGetComponent(out T component) ? component : go.AddComponent<T>();
    }
}
