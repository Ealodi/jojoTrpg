// src/data/presets.ts
import type { CharacterPreset, Talent } from '@/types/game';

// === 天赋池 ===
export const ALL_TALENTS: Talent[] = [
  { id: 'hp', name: '强壮 (血+2)', cost: 1, effect: { hp: 2 } },
  { id: 'mp', name: '精神 (蓝+2)', cost: 1, effect: { mp: 2 } },
  { id: 'hit', name: '精准 (命中+2)', cost: 1, effect: {} },
  { id: 'dodge', name: '灵巧 (闪避+2)', cost: 1, effect: {} },
  { id: 'init', name: '先发 (先攻+1)', cost: 1, effect: { init: 1 } },
  { id: 'speed', name: '疾风 (速+1)', cost: 2, effect: { speed: 1 } },
  { id: 'range', name: '鹰眼 (范围+1)', cost: 2, effect: { range: 1 } },
  { id: 'mp_regen', name: '源泉 (回蓝+1)', cost: 3, effect: {} },
  { id: 'extra_bonus', name: '战术 (附赠+1)', cost: 4, effect: {} },
  { id: 'extra_react', name: '反击 (反应+1)', cost: 4, effect: {} },
  { id: 'god_mode', name: '武神 (融合)', cost: 5, desc: '动作/附赠/反应融合，技能蓝耗翻倍', effect: {} },
  { id: 'berserk', name: '狂暴 (双动)', cost: 6, desc: '动作+1，先攻-5', effect: { action: 1, init: -5 } },
];

// === 角色预设 ===
export const CHARACTER_PRESETS: CharacterPreset[] = [
  {
    name: '承太郎',
    maxHp: 15, maxMp: 5, speed: 2, range: 1, initiative: 10, talentPoints: 6,
    passives: ['对非人类造成双倍伤害', '格挡成功受到的伤害减半（而非无效）', '可在非自己的时停中行动1回合'],
    skills: [
      { name: '欧拉欧拉', cost: '1能 1动', range: 1, damage: 2, description: '基础替身攻击' },
      { name: '流星指刺', cost: '2能 1动', range: 3, damage: 2, description: '远程突袭' },
      { name: '白金吸尘器', cost: '0能 1附', range: 3, description: '将敌人拉向自己2格' },
      { name: 'THE WORLD', cost: '3能 1动', range: 0, description: '时停2回合 (获得2动作2附赠)' }
    ],
    tags: ['乔斯达家族']
  },
  {
    name: 'DIO',
    maxHp: 15, maxMp: 6, speed: 2, range: 1, initiative: 10, talentPoints: 6,
    passives: ['复活需消耗一名队友一半血量', '吸血：对近战位尸体恢复1HP (1能)', '受波纹伤害双倍'],
    skills: [
      { name: '木大木大', cost: '1能 1动', range: 1, damage: 2, description: '基础攻击' },
      { name: '空烈眼刺惊', cost: '2能 1动', range: 4, damage: 2, description: '直线贯穿伤害' },
      { name: 'THE WORLD', cost: '5能 1动', range: 0, description: '时停3回合 (获得3动作3附赠)' },
      { name: '食我压路机', cost: '3能 1动', range: 1, damage: 3, description: 'AOE伤害 (仅时停可用)' }
    ],
    tags: ['非人类', '吸血鬼']
  },
  {
    name: '花京院',
    maxHp: 15, maxMp: 5, speed: 2, range: 4, initiative: 10, talentPoints: 8,
    passives: ['没人能躲过绿宝石水花 (特定条件下必中)', '触手陷阱触发后减速'],
    skills: [
      { name: '绿宝石水花', cost: '1能 1动', range: 4, damage: 2, description: '远程射击' },
      { name: '触手延伸', cost: '0能 1附', range: 5, description: '制造一个延伸点进行攻击' },
      { name: '法皇之网', cost: '3能 1动', range: 4, description: '布下陷阱，敌人进入减速' },
      { name: '半径20米水花', cost: '5能 1动', range: 5, description: '范围内敌人移动即受击' }
    ]
  },
  {
    name: '波波',
    maxHp: 15, maxMp: 5, speed: 2, range: 1, initiative: 12, talentPoints: 8,
    passives: ['闪避成功回复1HP', '基础闪避+4'],
    skills: [
      { name: '吼啦吼啦', cost: '1能 1动', range: 1, damage: 2, description: '快速连刺' },
      { name: '剑身射出', cost: '1能 1动', range: 3, damage: 2, description: '折弹道射击' },
      { name: '爆甲', cost: '0能 1动', range: 0, description: '速度+2 闪避+2，但受双倍伤害' },
      { name: '灵魂交换', cost: '镇魂曲', range: 5, description: '与目标交换位置' }
    ]
  },
  {
    name: '米斯达',
    maxHp: 15, maxMp: 6, speed: 2, range: 5, initiative: 10, talentPoints: 5,
    passives: ['造成伤害获得动作(单回合限5次,命中递减)', '受投射物伤害劣势'],
    skills: [
      { name: '射击', cost: '1能 1动', range: 5, damage: 1, description: '普通射击' },
      { name: '分裂射击', cost: '1能 1动', range: 3, damage: 2, description: '近距离爆发' },
      { name: 'Sex Pistols', cost: '3能 1动', range: 0, description: '回满子弹，回血，锁血Buff' }
    ]
  },
  {
    name: '阿布德尔',
    maxHp: 15, maxMp: 5, speed: 2, range: 3, initiative: 10, talentPoints: 6,
    passives: ['火焰余热 (造成伤害附加燃烧)', '不死鸟 (死后满状态复活一次)'],
    skills: [
      { name: '火焰喷射', cost: '1能 1动', range: 3, damage: 2, description: '扇形AOE' },
      { name: '赤红之绳', cost: '2能 1动', range: 3, description: '束缚并燃烧敌人' },
      { name: '高温领域', cost: '3能 1动', range: 0, description: '周围禁止回能' }
    ]
  },
  {
    name: '茸茸',
    maxHp: 15, maxMp: 5, speed: 2, range: 1, initiative: 10, talentPoints: 7,
    passives: ['攻击赋予生命 (反伤)', '复活仅需1血量'],
    skills: [
      { name: '木大木大', cost: '1能 1动', range: 1, damage: 2, description: '连打' },
      { name: '制造器官', cost: '1能 1动', range: 1, description: '治疗队友' },
      { name: '精神暴走', cost: '2能 1动', range: 1, description: '敌方减速并易伤' },
      { name: '无效化', cost: '镇魂曲', range: 5, description: '无效化目标的动作/技能' }
    ],
    tags: ['乔斯达家族']
  },
  {
    name: '天气预报',
    maxHp: 15, maxMp: 8, speed: 2, range: 5, initiative: 10, talentPoints: 7,
    passives: ['云层护体 (远程攻击劣势)', '技能需专注 (受伤检定DC=10+伤害)'],
    skills: [
      { name: '暴雨阻断', cost: '3能 1动', range: 99, description: '全场移动困难' },
      { name: '箭毒蛙雨', cost: '3能 1动', range: 4, description: '区域中毒' },
      { name: '纯氧供应', cost: '5能 1动', range: 99, description: '全场全员每动作扣血' }
    ]
  }
];