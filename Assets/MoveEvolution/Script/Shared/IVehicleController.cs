using UnityEngine;

/// <summary>
/// 駕駛員（玩家或 NPC）與車子之間的唯一溝通方式。
/// 約定：車頭永遠朝 VehicleTransform.forward（+Z）。
/// </summary>
public interface IVehicleController
{
    // throttle: -1 倒車 ~ 1 全速前進
    // steer:    -1 左轉 ~ 1 右轉
    // brake:    0 不煞車 ~ 1 全力煞車（煞車優先於油門）
    // handbrake: 手煞車，輪子鎖死
    void SetInput(float throttle, float steer, float brake, bool handbrake = false);

    // 車子目前的主體。車被撞毀時可能是 null，使用前要檢查。
    Transform VehicleTransform { get; }

    float CurrentSpeedKmh { get; }

    // 以下三項是即時值，輪胎、引擎掉了就會改變
    float EstimatedMaxSpeedKmh { get; }   // 理論最高速，不能開時為 0
    float TotalMass { get; }
    bool IsOperational { get; }           // 有引擎、有驅動輪、有油

    // 車體大小，座標是 VehicleTransform 的區域座標，避障可以用
    Bounds LocalBounds { get; }
}