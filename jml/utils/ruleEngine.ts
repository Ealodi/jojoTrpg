// src/utils/ruleEngine.ts
import type { GameCharacter, RollResult } from '@/types/game';

export const RuleEngine = {
  rollD20(bonus: number = 0): { total:number, msg:string } {
    const d20 = Math.floor(Math.random() * 20) + 1;
    let msg = `D20=${d20}`;
    if(d20===20) msg += ' (大成功)';
    if(d20===1) msg += ' (大失败)';
    return { total: d20+bonus, msg: `${msg} + ${bonus} = ${d20+bonus}` };
  },

  processStatus(char: GameCharacter, trigger: 'START' | 'END'): { char: GameCharacter, logs: string[] } {
    const logs: string[] = [];
    const newChar = JSON.parse(JSON.stringify(char));
    
    newChar.statuses = newChar.statuses.filter((s: any) => {
      if (s.name === '燃烧' && trigger === 'END') {
        newChar.hp = Math.max(0, newChar.hp - (s.value||1));
        logs.push(`${newChar.name} 燃烧扣血 -${s.value||1}`);
      }
      if (trigger === 'END') {
        s.duration--;
        if(s.duration <= 0) {
          logs.push(`${newChar.name} 的 ${s.name} 结束了`);
          return false;
        }
      }
      return true;
    });
    
    return { char: newChar, logs };
  },
  parseCost(costStr: string) {
    const result = { mp: 0, action: 0, bonus: 0, reaction: 0 };
    
    // 解析能量 (例如 "3能")
    const mpMatch = costStr.match(/(\d+)能/);
    if (mpMatch) result.mp = parseInt(mpMatch[1]);

    // 解析动作
    if (costStr.includes('动')) result.action = 1;
    
    // 解析附赠
    if (costStr.includes('附')) result.bonus = 1;
    
    // 解析反应
    if (costStr.includes('反')) result.reaction = 1;
    
    // 特殊：镇魂曲通常消耗巨大，这里暂不自动处理，交由DM判断
    
    return result;
  },
  // 1. 计算两点距离 (切比雪夫距离：允许斜向移动，距离 = max(|dx|, |dy|))
  getDistance(x1: number, y1: number, x2: number, y2: number) {
    return Math.max(Math.abs(x1 - x2), Math.abs(y1 - y2));
  },
  // 2. 简单的伤害计算 (预留接口)
  // 目前你的规则里大部分是固定伤害，这里仅做简单处理
  calcDamage(skillDmg: number, isCrit: boolean = false) {
    return skillDmg * (isCrit ? 2 : 1);
 }
};