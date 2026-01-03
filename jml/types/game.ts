// src/types/game.ts

export type Team = 'RED' | 'BLUE' | 'NEUTRAL';

export interface CharacterStatus {
  name: string;
  duration: number;
  ownerId: string;
  value?: number;
  effectDescription?: string;
}

export interface Skill {
  name: string;
  cost: string; // 如 "1能 1动"
  range: number;
  damage?: number;
  description: string;
}

export interface Talent {
  id: string;
  name: string;
  cost: number;
  desc?: string;
  effect: {
    hp?: number;
    mp?: number;
    speed?: number;
    range?: number;
    init?: number;
    action?: number;
  };
}

export interface GameCharacter {
  id: string;
  name: string;
  team: Team;
  
  // 核心属性
  hp: number; maxHp: number;
  mp: number; maxMp: number;
  speed: number; 
  range: number;
  initiative: number;
  
  // 战棋属性
  x: number; y: number;
  actionPoints: number; 
  bonusPoints: number; 
  reactionPoints: number;
  
  // 技能与状态
  skills: Skill[];
  passives: string[];
  statuses: CharacterStatus[];
  isDead: boolean;
  tags?: string[];
}

export interface CharacterPreset {
  name: string;
  maxHp: number;
  maxMp: number;
  speed: number;
  range: number;
  initiative: number;
  talentPoints: number;
  passives: string[];
  skills: Skill[];
  tags?: string[];
}

export interface UserInfo {
  id: string;
  name: string;
  role: 'DM' | 'PLAYER';
}

export interface GameState {
  roomName: string;     // 房间名
  round: number;        // 当前回合数
  ringLevel: number;    // 缩圈等级
  currentActorId: string; // 当前行动的角色ID
  characters: GameCharacter[]; // 所有的棋子
}

// 简单的日志结构
export interface GameLog {
  id: string;
  content: string;
  timestamp: number;
}