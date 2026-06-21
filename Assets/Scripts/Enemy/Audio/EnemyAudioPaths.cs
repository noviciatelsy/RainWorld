/// <summary>
/// Resources 路径（相对 Assets/Resources，不含扩展名）。
/// </summary>
public static class EnemyAudioPaths
{
    private const string Root = "Audios/SFX/游戏部分音效/游戏部分音效/怪物音效";

    public const string ShibieMoveLoop = Root + "/尸鳖/虫子爬行声（尸鳖，血蜘蛛都可以用）";
    public const string ShibieStomp = Root + "/尸鳖/尸鳖被踩音效";

    public const string BigRobotIdleLoop = Root + "/巨型机器人与电池/巨型机器人待机音效（循环，机器人启动后播放）";
    public const string BigRobotSlash = Root + "/巨型机器人与电池/巨型机器人出刀音效";
    public const string BigRobotShutdown = Root + "/巨型机器人与电池/巨型机器人死亡音效";

    public const string GhostIdleLoop = Root + "/幽灵/幽灵音效1";
    public const string GhostDeath = Root + "/幽灵/幽灵死亡音效";

    public const string WolfSpiderAttack = Root + "/洞穴跳蛛/洞穴跳蛛攻击音效（血蜘蛛要是有攻击也可以用这个）";
    public const string WolfSpiderJump = Root + "/洞穴跳蛛/洞穴跳蛛跳跃音效";
    public const string WolfSpiderLand = Root + "/洞穴跳蛛/落地音效（主角，跳蛛落地通用）";
    public const string WolfSpiderStomp = Root + "/洞穴跳蛛/蜘蛛死亡（晕）音效（洞穴跳蛛，血蜘蛛通用）";

    public const string SnailCrawlLoop = Root + "/蜗牛/蜗牛爬行音效";
    public const string SnailEat = Root + "/蜗牛/蜗牛进食音效";

    public const string BatWingLoop = Root + "/蝙蝠们/蝙蝠扇翅膀音效（一直循环）";
    public const string BatAttack = Root + "/蝙蝠们/蝙蝠/蝙蝠攻击_爱给网_aigei_com";
    public const string BatSpotPlayer = Root + "/蝙蝠们/蝙蝠王/蝙蝠王发现玩家音效";

    public const string BatKingAttack = Root + "/蝙蝠们/蝙蝠王/蝙蝠王攻击";
    public const string BatKingSpotPlayer = Root + "/蝙蝠们/蝙蝠王/蝙蝠王发现玩家音效";

    public const string RobotDashLoop = Root + "/重拳机器人/重拳机器人冲锋音效（如果技术允许建议做成距玩家越近音效频率越快，这个音效放慢了也能当工业区房间警报音效）";
    public const string RobotHit = Root + "/重拳机器人/重拳机器人命中音效";

    public const string MoleStealWarning = Root + "/鼹鼠/鼹鼠偷东西预警音（发出这个音效后正式生成爪子偷东西）";
    public const string MoleStealLoop = Root + "/鼹鼠/鼹鼠爪子移动音效（循环）";
    public const string MoleGift = Root + "/鼹鼠/鼹鼠送礼音效";
    public const string MoleDigOut = Root + "/鼹鼠/鼹鼠钻出来音效";
    public const string MoleDigIn = Root + "/鼹鼠/鼹鼠钻回去音效";

    public const string MoleParentSleepLoop = Root + "/鼹鼠爷爷/鼹鼠爷爷睡觉音效";
    public const string MoleParentWake = Root + "/鼹鼠爷爷/鼹鼠爷爷惊醒音效";
}
