/// <summary>다른 기능에 이동 의도 여부만 알립니다. 물리 속도와 엔진 객체는 노출하지 않습니다.</summary>
public interface ICatMotionState
{
    bool IsMoving { get; }
}
