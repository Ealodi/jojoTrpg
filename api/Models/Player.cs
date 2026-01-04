using api.Models;

namespace api.Models
{
    public class Player
    {
        public string ConnectionId { get; set; } = string.Empty; // SignalR的连接ID
        public string UserId { get; set; } = string.Empty;       // 用户名
        public int TeamId { get; set; } // 1-4组 [cite: 1]

        // 坐标
        public int X { get; set; }
        public int Y { get; set; }

        public CharacterStats Stats { get; set; } = new();

        // 特殊状态
        public bool HasArrow { get; set; } = false; // 是否持有虫箭 [cite: 4]
        public int ArrowKillTimer { get; set; } = 0; // 虫箭4回合击杀计时
    }
}