using api.Models;

namespace api.Models
{
    public class GameRoom
    {
        public string RoomId { get; set; } = Guid.NewGuid().ToString();
        public List<Player> Players { get; set; } = new();

        // 游戏进度
        public int CurrentTurn { get; set; } = 1; // 当前回合数
        public int ActivePlayerIndex { get; set; } = 0; // 当前行动的玩家索引

        // 地图设置
        public int MapSize { get; set; } = 20;
        public int SafeZoneRadius { get; set; } = 10; // 初始安全区半径

        public Dictionary<string, string> GridItems { get; set; } = new();

        // 虫箭位置 (如果未被拾取)
        public (int X, int Y)? ArrowPosition { get; set; } = null;
    }
}