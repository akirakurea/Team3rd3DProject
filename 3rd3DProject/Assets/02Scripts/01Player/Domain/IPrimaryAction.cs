/// <summary>설치 같은 선택 동작이 좌클릭을 사용하는 동안 집기·발사의 중복 실행을 막는 작은 계약입니다.</summary>
public interface IPrimaryAction
{
    bool ClaimsPrimaryInput { get; }
}

/// <summary>F 유지 동작이 시작된 동안 즉시 획득 입력을 중복 처리하지 않습니다.</summary>
public interface IInteractionAction
{
    bool ClaimsInteractionInput { get; }
}

/// <summary>현재 조준선에 기존 F 상호작용 대상이 있는지 확인하는 작은 조회 계약입니다.</summary>
public interface IInteractionTargetQuery
{
    bool HasInteractionTarget();
}
