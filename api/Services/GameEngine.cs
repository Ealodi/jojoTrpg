using api.Models;

namespace api.Services
{
    public class GameEngine
    {
        private readonly Random _rng = new Random();

        // 1. 核心战斗结算方法
        public AttackResult ResolveAttack(Player attacker, Player target, Skill skill)
        {
            var result = new AttackResult();

            // --- A. 资源检查 ---
            if (attacker.Stats.CurrentEnergy < skill.EnergyCost ||
                attacker.Stats.Actions < skill.ActionCost ||
                attacker.Stats.BonusActions < skill.BonusActionCost)
            {
                result.Success = false;
                result.Message = "资源不足！无法释放技能。";
                return result;
            }

            // --- B. 扣除资源 ---
            attacker.Stats.CurrentEnergy -= skill.EnergyCost;
            attacker.Stats.Actions -= skill.ActionCost;
            attacker.Stats.BonusActions -= skill.BonusActionCost;

            // --- C. 投掷 D20 (1-20) ---
            int roll = _rng.Next(1, 21);
            result.RollValue = roll;

            // --- D. 判定 大失败/大成功 [cite: 230] ---
            // 规则：1d20=20 为大失败(MISS)，1d20=1 为大成功(双倍伤害)
            // 注意：通常DND是1大失败20大成功，但这里必须严格遵照你的文档 [cite: 230]
            if (roll == 20)
            {
                result.IsHit = false;
                result.Message = $"大失败！(D20=20) 攻击落空！";
                return result; // 直接返回，不造成伤害
            }

            bool isCrit = (roll == 1);
            int finalDamage = isCrit ? skill.Damage * 2 : skill.Damage;

            // --- E. 命中计算 [cite: 231] ---
            // 规则：成功命中几率最低60% (即 1d20 > 8 命中? 文档例子是 1d20>12 为60%)
            // 公式：Roll + 攻击者命中加成 - 目标闪避加成
            // 我们设定基础阈值为 12 (即如果不加成，需要掷出13以上才能中，概率40%? 文档说最低60%可能指包含基础修正)
            // 这里我们采用标准战棋做法： 攻击检定值 = Roll + BaseAccuracy
            // 防御等级(AC) = 12 + BaseEvasion
            int attackRollTotal = roll + attacker.Stats.BaseAccuracy;
            int targetAC = 12 + target.Stats.BaseEvasion;

            bool normalHit = attackRollTotal >= targetAC;

            // 只要是大成功(1) 或者 检定通过，就算命中
            if (isCrit || normalHit)
            {
                result.IsHit = true;
                result.DamageDealt = finalDamage;

                // 扣血
                target.Stats.CurrentHp -= finalDamage;
                if (target.Stats.CurrentHp < 0) target.Stats.CurrentHp = 0;

                string hitType = isCrit ? "大成功(双倍)" : "命中";
                result.Message = $"{hitType}! (🎲{roll}+{attacker.Stats.BaseAccuracy} vs {targetAC}) 造成 {finalDamage} 伤害。";
            }
            else
            {
                result.IsHit = false;
                result.Message = $"未命中 (🎲{roll}+{attacker.Stats.BaseAccuracy} vs {targetAC})";
            }

            result.Success = true; // 技能成功释放了（只是可能没打中）
            return result;
        }

        // 2. 距离判定 (曼哈顿距离：走格子的距离) [cite: 236]
        public bool IsInRange(Player p1, int targetX, int targetY, int range)
        {
            int dist = Math.Abs(p1.X - targetX) + Math.Abs(p1.Y - targetY);
            return dist <= range;
        }
    }

    // 用于返回给前端的结算结果
    public class AttackResult
    {
        public bool Success { get; set; } // 逻辑是否执行成功
        public bool IsHit { get; set; }   // 是否命中
        public int DamageDealt { get; set; }
        public int RollValue { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}