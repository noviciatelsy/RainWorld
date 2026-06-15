public enum ElevatorFloor
{
    Ground = 0,
    Cave = 1,
    Factory = 2
}

public static class ElevatorFloorUtility
{
    public static string ToDisplayName(ElevatorFloor floor)
    {
        switch (floor)
        {
            case ElevatorFloor.Ground:
                return "地面";
            case ElevatorFloor.Cave:
                return "洞穴";
            case ElevatorFloor.Factory:
                return "工厂";
            default:
                return floor.ToString();
        }
    }

    public static string ToSaveKey(ElevatorFloor floor)
    {
        return floor.ToString();
    }
}
