/// <summary>설치 시간 정책에 전달할, 장치와 무관한 입력입니다.</summary>
public readonly struct PlacementInputFrame
{
    public readonly bool Pressed, Held, Cancel;
    public PlacementInputFrame(bool pressed, bool held, bool cancel)
    { Pressed = pressed; Held = held; Cancel = cancel; }
}

/// <summary>
/// 좌클릭 또는 F 중 처음 설치를 시작한 키만 유지합니다.
/// F를 누른 순간 획득 대상이 있으면 수집이 우선이며, 같은 누름을 설치에 다시 쓰지 않습니다.
/// </summary>
public sealed class PlacementInput
{
    enum Source { None, Mouse, F, Collection, Blocked }
    Source source;
    bool ownsHold;
    public bool ClaimsInteractionInput { get; private set; }

    public PlacementInputFrame Read(bool enabled, bool mousePressed, bool mouseHeld,
        bool fPressed, bool fHeld, bool cancel, bool canPlace, bool hasInteractionTarget)
    {
        bool anyHeld = mouseHeld || fHeld;
        if (!anyHeld)
        {
            Reset();
            return new PlacementInputFrame(false, false, !enabled || cancel);
        }
        if (!enabled || cancel) return Block();

        switch (source)
        {
            case Source.Blocked:
            case Source.Collection:
                return Cancelled();

            case Source.Mouse:
                if (fPressed)
                {
                    // 진행 중인 좌클릭 설치를 버리고, 새 F 입력의 기존 수집 동작을 허용합니다.
                    source = Source.Collection;
                    ClaimsInteractionInput = false;
                    return Cancelled();
                }
                if (!mouseHeld || !canPlace) return Block();
                return new PlacementInputFrame(false, true, false);

            case Source.F:
                // 다른 키를 추가로 눌러도 시간이나 시작 키가 바뀌지 않습니다.
                if (!fHeld || !canPlace) return Block();
                return new PlacementInputFrame(false, true, false);
        }

        // 동시에 눌렀다면 F를 먼저 판정합니다.
        if (fPressed)
        {
            if (!fHeld || hasInteractionTarget || !canPlace)
            {
                source = Source.Collection;
                return Cancelled();
            }
            source = Source.F;
            ClaimsInteractionInput = true;
            ownsHold = true;
            return new PlacementInputFrame(true, true, false);
        }
        if (mousePressed && mouseHeld && canPlace)
        {
            source = Source.Mouse;
            ownsHold = true;
            return new PlacementInputFrame(true, true, false);
        }

        // 키를 이미 누른 채 UI를 닫거나 슬롯을 바꿔도 새 설치로 취급하지 않습니다.
        return Block();
    }

    PlacementInputFrame Block()
    {
        source = Source.Blocked;
        return Cancelled();
    }

    PlacementInputFrame Cancelled()
    {
        // 설치를 시작하지 않은 일반 좌클릭은 발사/집기 담당자에게 그대로 남깁니다.
        // 시작한 설치는 취소 후에도 두 키를 모두 뗄 때까지 해제 잠금을 유지합니다.
        return new PlacementInputFrame(false, ownsHold, true);
    }

    public void Reset()
    {
        source = Source.None;
        ownsHold = false;
        ClaimsInteractionInput = false;
    }
}
