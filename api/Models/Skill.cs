namespace api.Models
{
    public enum TargetType { Self, Enemy, Ally, EmptyGrid }

    public class Skill
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public int EnergyCost { get; set; }
        public int ActionCost { get; set; } // 通常为 1
        public int BonusActionCost { get; set; } // 通常为 0，部分技能为 1
        // 属性 [cite: 19]
        public int Range { get; set; }
        public int Damage { get; set; }
        public TargetType TargetType { get; set; }
    }

    // 预设技能库 (简单工厂模式)
    public static class SkillLibrary
    {
        public static List<Skill> GetSkillsForCharacter(string charName)
        {
            var skills = new List<Skill>();

            // 通用技能：移动 [cite: 41]
            skills.Add(new Skill { Id = "move", Name = "移动", ActionCost = 0, BonusActionCost = 1, Range = 0, Description = "消耗附赠动作移动" });

            // 承太郎技能 [cite: 78-81]
            if (charName == "承太郎")
            {
                skills.Add(new Skill { Id = "ora", Name = "欧拉欧拉", EnergyCost = 1, ActionCost = 1, Range = 1, Damage = 2, TargetType = TargetType.Enemy });
                skills.Add(new Skill { Id = "star_finger", Name = "流星指刺", EnergyCost = 2, ActionCost = 1, Range = 4, Damage = 2, TargetType = TargetType.Enemy });
            }
            // 可以在这里继续添加 DIO, 也就是 [cite: 95] 等
            return skills;
        }
    }
}