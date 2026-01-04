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

        // 修改：移动现在消耗附赠动作 (根据 Skill.cs 的设定)
        public async Task MovePiece(string roomId, int x, int y)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player == null) return;

                // 检查附赠动作是否足够
                if (player.Stats.BonusActions <= 0)
                {
                    await Clients.Caller.SendAsync("ReceiveLog", "❌ 附赠动作不足，无法移动！");
                    return;
                }

                if (_engine.ValidateMove(player, x, y))
                {
                    player.X = x;
                    player.Y = y;
                    player.Stats.BonusActions--; // [cite: 41] 修正为消耗附赠动作
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

        // 技能释放接口
        public async Task UseSkill(string roomId, string skillId, int targetX, int targetY)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;
            var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player == null) return;

            // 1. 获取技能数据
            var skills = SkillLibrary.GetSkillsForCharacter(player.Stats.Name);
            var skill = skills.FirstOrDefault(s => s.Id == skillId);
            if (skill == null) return;

            // 2. 资源检查 (Action/Energy)
            if (player.Stats.Actions < skill.ActionCost || player.Stats.CurrentEnergy < skill.EnergyCost)
            {
                await Clients.Caller.SendAsync("ReceiveLog", $"❌ 资源不足！需要 {skill.ActionCost}动作 / {skill.EnergyCost}能量");
                return;
            }

            // 3. 距离检查
            if (!_engine.IsInRange(player, targetX, targetY, skill.Range))
            {
                await Clients.Caller.SendAsync("ReceiveLog", "❌ 目标超出射程！");
                return;
            }

            // 获取目标
            var target = room.Players.FirstOrDefault(p => p.X == targetX && p.Y == targetY);

            // 如果是攻击技能
            if (skill.TargetType == TargetType.Enemy && target != null)
            {
                // 扣除攻击者资源
                player.Stats.Actions -= skill.ActionCost;
                player.Stats.CurrentEnergy -= skill.EnergyCost;

                // ★ 关键点：检查目标是否有【反应】 ★
                // 必须有反应点数，且不在“被动无法反应”的状态
                if (target.Stats.Reactions > 0)
                {
                    // 挂起攻击，等待目标响应
                    room.CurrentInteraction = new PendingInteraction
                    {
                        AttackerId = player.ConnectionId,
                        TargetId = target.ConnectionId,
                        SkillId = skillId,
                        SkillDamage = skill.Damage
                    };

                    // 通知目标玩家：有人打你了！快选反应！
                    await Clients.Client(target.ConnectionId).SendAsync("RequestReaction", player.Stats.Name, skill.Name);

                    // 通知其他人：正在等待反应
                    await Clients.GroupExcept(roomId, new[] { target.ConnectionId }).SendAsync("ReceiveLog", $"⏳ {player.Stats.Name} 对 {target.Stats.Name} 发动攻击，等待反应...");
                    return;
                }
                else
                {
                    // 没反应次数了，直接结算 (reactionType = 0)
                    await FinalizeAttack(room, player, target, skill, 0);
                }
            }
            // ... 其他类型技能逻辑 ...
            await Clients.Group(roomId).SendAsync("RoomUpdated", room);

            // 这里可以扩展对自己(Self)或队友(Ally)的技能逻辑

            await Clients.Group(roomId).SendAsync("RoomUpdated", room);
        }


        // 2. 目标提交反应 (由前端弹窗调用)
        // reactionType: 0=不反应, 1=闪避, 2=格挡, 3=反击
        public async Task SubmitReaction(string roomId, int reactionType)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;
            var interaction = room.CurrentInteraction;

            // 校验合法性
            if (interaction == null || interaction.TargetId != Context.ConnectionId) return;

            var target = room.Players.FirstOrDefault(p => p.ConnectionId == interaction.TargetId);
            var attacker = room.Players.FirstOrDefault(p => p.ConnectionId == interaction.AttackerId);

            if (target == null || attacker == null) return;

            // 消耗反应资源
            if (reactionType != 0)
            {
                target.Stats.Reactions--;
                target.Stats.BonusActionDebt++; // 记账：下回合扣除附赠动作

                string reactName = reactionType == 1 ? "闪避" : (reactionType == 2 ? "格挡" : "反击");
                await Clients.Group(roomId).SendAsync("ReceiveLog", $"❗ {target.Stats.Name} 使用了 {reactName}！(下回合将消耗附赠动作)");
            }

            // 获取技能原型用于结算
            var skills = SkillLibrary.GetSkillsForCharacter(attacker.Stats.Name);
            var skill = skills.FirstOrDefault(s => s.Id == interaction.SkillId);

            // 3. 特殊处理：反击 (Counter)
            // 规则：反击通常允许目标先打一下攻击者，或者同时结算。
            // 这里简化为：反击者免费对攻击者进行一次普攻判定
            if (reactionType == 3)
            {
                var counterRes = _engine.ResolveCombat(target.Stats, attacker.Stats, 2, 0); // 假设反击伤害2
                attacker.Stats.CurrentHp -= counterRes.Damage;
                await Clients.Group(roomId).SendAsync("ReceiveLog", $"⚔️ [反击] {target.Stats.Name} 回敬一击: {counterRes.Log}");
            }

            // 4. 结算原攻击
            await FinalizeAttack(room, attacker, target, skill, reactionType);

            // 清除挂起状态
            room.CurrentInteraction = null;
            await Clients.Group(roomId).SendAsync("RoomUpdated", room);
        }

        // 内部方法：最终结算
        private async Task FinalizeAttack(GameRoom room, Player attacker, Player target, Skill skill, int reactionType)
        {
            // 如果反击把攻击者打死了，原攻击是否取消？通常是取消。
            if (!attacker.Stats.IsAlive)
            {
                await Clients.Group(room.RoomId).SendAsync("ReceiveLog", "🚫 攻击者已倒下，攻击中止！");
                return;
            }

            var result = _engine.ResolveCombat(attacker.Stats, target.Stats, skill.Damage, reactionType);

            if (result.IsHit)
            {
                target.Stats.CurrentHp -= result.Damage;
                if (target.Stats.CurrentHp < 0) target.Stats.CurrentHp = 0;
            }

            await Clients.Group(room.RoomId).SendAsync("ReceiveLog", $"{attacker.Stats.Name} -> {target.Stats.Name}: {result.Log}");
        }
    }

}
