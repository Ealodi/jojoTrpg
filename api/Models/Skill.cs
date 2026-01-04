namespace api.Models
{
    // 定义目标类型：对自己、对敌人、对队友、对空地(如位移)
    public enum TargetType { Self, Enemy, Ally, EmptyGrid }

    public class Skill
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // 资源消耗
        public int EnergyCost { get; set; }
        public int ActionCost { get; set; } = 1; // 默认消耗1个动作
        public int BonusActionCost { get; set; } = 0;

        // 战斗属性
        public int Range { get; set; }
        public int Damage { get; set; }
        public TargetType TargetType { get; set; }
    }

    // 静态技能库：根据角色名获取技能列表
    public static class SkillLibrary
    {
        public static List<Skill> GetSkillsForCharacter(string charName)
        {
            var skills = new List<Skill>();

            // --- 通用技能 ---
            // 移动通常是基础操作，但如果做成消耗附赠动作的技能也可以

            // --- 承太郎 ---
            if (charName == "承太郎")
            {
                skills.Add(new Skill { Id = "ora", Name = "欧拉欧拉", EnergyCost = 1, ActionCost = 1, Range = 1, Damage = 2, TargetType = TargetType.Enemy, Description = "造成2点伤害" });
                skills.Add(new Skill { Id = "star_finger", Name = "流星指刺", EnergyCost = 2, ActionCost = 1, Range = 4, Damage = 2, TargetType = TargetType.Enemy, Description = "远程攻击" });
            }
            // --- DIO ---
            else if (charName == "DIO")
            {
                skills.Add(new Skill { Id = "muda", Name = "木大木大", EnergyCost = 1, ActionCost = 1, Range = 1, Damage = 2, TargetType = TargetType.Enemy });
                skills.Add(new Skill { Id = "knives", Name = "飞刀", EnergyCost = 1, ActionCost = 1, Range = 3, Damage = 1, TargetType = TargetType.Enemy });
            }

            return skills;
        }
    }
}