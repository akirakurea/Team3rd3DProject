using System.Collections.Generic;

/// <summary>메뉴별 차단 요청을 합친다. 한 메뉴가 닫혀도 다른 메뉴의 요청은 유지한다.</summary>
public sealed class InputPermission
{
    readonly HashSet<object> blockers = new HashSet<object>();

    public bool AllowsGameplay(bool stateAllowsGameplay, bool timeRunning)
        => stateAllowsGameplay && timeRunning && blockers.Count == 0;

    public bool SetBlocked(object owner, bool blocked)
    {
        if (owner == null) return false;
        return blocked ? blockers.Add(owner) : blockers.Remove(owner);
    }
}
