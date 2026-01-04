namespace api.Models
{
    public class CharacterStats
    {
        public string Name { get; set; } = string.Empty; // 角色名 (承太郎, DIO等)

        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }

        public int MaxEnergy { get; set; } // 上限最高十点 [cite: 20]
        public int CurrentEnergy { get; set; }

        public int Speed { get; set; } // 默认1格 [cite: 18]
        public int AttackRange { get; set; } // 攻击距离 [cite: 19]

        public int BaseAccuracy { get; set; } // 基础命中概率 [cite: 14]
        public int BaseEvasion { get; set; } = 0; // 闪避加成

        public int Actions { get; set; } = 1;
        public int BonusActions { get; set; } = 1;
        // 反应资源
        public int Reactions { get; set; } = 1;

        // 下回合需要扣除的附赠动作 (欠债)
        public int BonusActionDebt { get; set; } = 0;

        public bool IsAlive => CurrentHp > 0;
    }
}