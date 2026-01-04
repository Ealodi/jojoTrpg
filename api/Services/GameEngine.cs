using api.Models;

namespace api.Services
{
    public class GameEngine
    {
        private readonly Random _rng = new Random();

        public int RollD20()
        {
            return _rng.Next(1, 21); // 生成 1 到 20
        }

        public (bool IsHit, bool IsCrit, bool IsFumble, int Damage) CalculateAttack(
            CharacterStats attacker,
            CharacterStats target,
            int skillDamage)
        {
            int roll = RollD20();

            // 大失败 (1d20=20? 原文说1d20=20是大失败可能笔误，通常DND中1是大失败，20是大成功。
            // 我们严格遵照文档编写：
            if (roll == 20) return (false, false, true, 0); // MISS

            if (roll == 1) return (true, true, false, skillDamage * 2); // 双倍伤害

            // 假设基础命中阈值是 12，加上攻击者的加成，减去目标的闪避
            int hitThreshold = 12 - attacker.BaseAccuracy + target.BaseEvasion;

            // 如果 roll 点数大于阈值则命中
            bool isHit = roll > hitThreshold;

            return (isHit, false, false, isHit ? skillDamage : 0);
        }

        public bool ValidateMove(Player player, int targetX, int targetY)
        {
            // 曼哈顿距离计算
            int distance = Math.Abs(player.X - targetX) + Math.Abs(player.Y - targetY);

            // 检查速度与剩余动作
            if (distance <= player.Stats.Speed && player.Stats.Actions > 0)
            {
                return true;
            }
            return false;
        }
    }
}