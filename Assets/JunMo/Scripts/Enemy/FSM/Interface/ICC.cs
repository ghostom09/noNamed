using UnityEngine;

public interface ICC
{
    void Apply();
    void Exit();
    void Update();
    bool IsDone { get; }
}