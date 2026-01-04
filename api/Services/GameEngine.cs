using api.Models;

namespace api.Services
{
    public class GameEngine
    {
        private readonly Random _rng = new Random();

        // 掷骰子 (1d20)
        public int RollD20()
        {
            return _rng.Next(1, 21);
        }

        // 移动验证 (对应 GameHub 的 ValidateMove 调用)
        public bool ValidateMove(Player player, int targetX, int targetY)
        {
            // 计算曼哈顿距离
            int distance = Math.Abs(player.X - targetX) + Math.Abs(player.Y - targetY);

            // 检查是否在移动范围内
            // 注意：这里只检查距离，资源(Actions/BonusActions)扣除逻辑已移交 GameHub 处理
            if (distance <= player.Stats.Speed && distance > 0)
            {
                return true;
            }
            return false;
        }

        // 技能范围检查 (对应 GameHub 的 IsInRange 调用)
        public bool IsInRange(Player p1, int targetX, int targetY, int range)
        {
            int distance = Math.Abs(p1.X - targetX) + Math.Abs(p1.Y - targetY);
            return distance <= range;
        }
        // reactionType: 0=无, 1=闪避(Dodge), 2=格挡(Block), 3=反击(Counter)
        public (bool IsHit, bool IsCrit, int Damage, string Log) ResolveCombat(
            CharacterStats attacker,
            CharacterStats target,
            int baseDamage,
            int reactionType)
        {
            int roll1 = RollD20();
            int roll2 = RollD20();
            int finalRoll = roll1;
            string rollLog = $"D20={roll1}";

            // 1. 处理闪避 (Dodge): 劣势 (取两次最低)
            if (reactionType == 1)
            {
                finalRoll = Math.Min(roll1, roll2);
                rollLog = $"闪避劣势(D20={roll1},{roll2} 取 {finalRoll})";
            }

            // 命中判定
            int hitThreshold = 12 - attacker.BaseAccuracy + target.BaseEvasion;

            // 2. 处理格挡 (Block): 假设增加防御等级或者直接减伤？
            // 根据文档常见设计，格挡通常是命中照常，但伤害减半。这里我们先按命中算。

            bool isCrit = (finalRoll == 1); // 大成功
            bool isFumble = (finalRoll == 20); // 大失败

            bool isHit = (finalRoll > hitThreshold) || isCrit;
            if (isFumble) isHit = false;

            int finalDamage = 0;
            string resultLog = "";

            if (isHit)
            {
                // 计算基础伤害
                int dmg = isCrit ? baseDamage * 2 : baseDamage;

                // 3. 处理格挡 (Block): 伤害减半 (向下取整，最少1点)
                if (reactionType == 2)
                {
                    dmg = Math.Max(1, dmg / 2);
                    resultLog += " [已格挡]";
                }

                finalDamage = dmg;
                resultLog += isCrit ? " 大成功(双倍)!" : " 命中!";
            }
            else
            {
                resultLog = " 未命中/被闪避";
            }

            return (isHit, isCrit, finalDamage, $"{rollLog} vs {hitThreshold} => {resultLog}");
        }
        
    }
}