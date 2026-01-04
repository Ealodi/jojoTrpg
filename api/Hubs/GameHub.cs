using api.Models;
using api.Services;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace api.Hubs
{
    public class GameHub : Hub
    {
        // 内存中存储房间状态 (开发阶段使用静态字典，生产环境应使用 Redis)
        private static ConcurrentDictionary<string, GameRoom> _rooms = new();
        private readonly GameEngine _engine;

        public GameHub(GameEngine engine)
        {
            _engine = engine;
        }

        // 玩家创建/加入房间
        public async Task JoinRoom(string roomId, string username, string characterName)
        {
            // 获取或创建房间
            var room = _rooms.GetOrAdd(roomId, new GameRoom { RoomId = roomId });

            // 创建新玩家
            var newPlayer = new Player
            {
                ConnectionId = Context.ConnectionId,
                UserId = username,
                Stats = InitCharacterStats(characterName) // 初始化属性
            };

            room.Players.Add(newPlayer);

            // 将连接加入 SignalR 组
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // 广播更新给房间内所有人
            await Clients.Group(roomId).SendAsync("RoomUpdated", room);
            await Clients.Group(roomId).SendAsync("ReceiveLog", $"系统: {username} ({characterName}) 加入了游戏。");
        }

        // 玩家移动请求
        public async Task MovePiece(string roomId, int x, int y)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player == null) return;

                // 验证移动逻辑
                if (_engine.ValidateMove(player, x, y))
                {
                    player.X = x;
                    player.Y = y;
                    player.Stats.Actions--; // 消耗动作 [cite: 12]

                    // 广播新位置
                    await Clients.Group(roomId).SendAsync("RoomUpdated", room);
                }
            }
        }

        private CharacterStats InitCharacterStats(string name)
        {
            if (name == "承太郎")
            {
                return new CharacterStats
                {
                    Name = "承太郎",
                    MaxHp = 8,
                    CurrentHp = 8,
                    MaxEnergy = 5,
                    CurrentEnergy = 5,
                    Speed = 2,
                    BaseAccuracy = 4
                };
            }
            // ... 添加其他角色
            return new CharacterStats { Name = "杂鱼" };
        }

        public async Task UseSkill(string roomId, string skillId, int targetX, int targetY)
        {
            // 1. 找到房间和玩家
            if (_rooms.TryGetValue(roomId, out var room))
            {
                var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player == null) return;

                // 2. 获取技能详情
                var skills = SkillLibrary.GetSkillsForCharacter(player.Stats.Name);
                var skill = skills.FirstOrDefault(s => s.Id == skillId);
                if (skill == null) return;

                // 3. 验证距离
                if (!_engine.IsInRange(player, targetX, targetY, skill.Range))
                {
                    await Clients.Caller.SendAsync("ReceiveLog", "❌ 目标超出射程！");
                    return;
                }

                // 4. 处理对敌攻击
                if (skill.TargetType == TargetType.Enemy)
                {
                    var target = room.Players.FirstOrDefault(p => p.X == targetX && p.Y == targetY);
                    if (target != null)
                    {
                        // 调用引擎结算
                        var result = _engine.ResolveAttack(player, target, skill);

                        if (result.Success)
                        {
                            // 广播更新后的房间状态（血量变化）
                            await Clients.Group(roomId).SendAsync("RoomUpdated", room);
                            // 广播战斗日志
                            await Clients.Group(roomId).SendAsync("ReceiveLog", $"⚔️ {player.Stats.Name} 使用了 {skill.Name}: {result.Message}");
                        }
                        else
                        {
                            await Clients.Caller.SendAsync("ReceiveLog", $"⚠️ {result.Message}");
                        }
                    }
                }
            }
        }
    }

}
