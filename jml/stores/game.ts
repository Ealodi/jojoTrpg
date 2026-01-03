// stores/game.ts
import { defineStore } from 'pinia';
import { io, Socket } from 'socket.io-client';
import type { GameState, GameCharacter, Skill, UserInfo } from '@/types/game';
import { RuleEngine } from '@/utils/ruleEngine';

const SERVER_URL = 'http://192.168.199.242:3000'; // 真机调试请改成本机局域网IP

export const useGameStore = defineStore('game', {
  state: () => ({
    // === 用户信息 (本地，不通过网络同步) ===
    currentUser: null as UserInfo | null,
    socket: null as Socket | null,
    isConnected: false,

    // === 游戏核心状态 (需要全网同步的数据) ===
    gameState: {
      roomName: 'JOJO_Room_1',
      round: 1,
      ringLevel: 0,
      currentActorId: '',
      characters: [] as GameCharacter[],
    } as GameState,

    // === 日志系统 ===
    logs: [] as string[],
  }),

  getters: {
    isDM: (state) => state.currentUser?.role === 'DM',
    // 计算行动顺序 (按先攻排序)
    turnOrder: (state) => {
      return [...state.gameState.characters]
        .filter(c => !c.isDead)
        .sort((a, b) => b.initiative - a.initiative);
    },
    // 获取当前操作的角色对象
    currentActor: (state) => state.gameState.characters.find(c => c.id === state.gameState.currentActorId),
  },

  actions: {
    // === 1. 连接与同步逻辑 ===
    login(name: string, role: 'DM'|'PLAYER') {
      this.currentUser = { id: this.socket?.id || 'guest', name, role };
      // 登录后尝试连接
      this.initSocket();
    },

    initSocket() {
      if (this.socket) return;

      this.socket = io(SERVER_URL, { transports: ['websocket'] });

      this.socket.on('connect', () => {
        this.isConnected = true;
        if(this.currentUser) this.currentUser.id = this.socket!.id; // 绑定 Socket ID
        console.log('🔗 已连接服务器');
      });

      this.socket.on('disconnect', () => {
        this.isConnected = false;
      });

      // 核心：监听来自服务器的“全量状态覆盖”
      this.socket.on('sync_state', (remoteState: GameState) => {
        console.log('🔄 收到状态同步');
        this.gameState = remoteState;
      });

      // 监听广播日志
      this.socket.on('new_log', (msg: string) => {
        this.logs.push(msg);
      });
    },

    // ⚡ 核心动作：同步状态到服务器
    // 每当你修改了 gameState，必须调用这个函数！
    sync() {
      if (this.socket && this.isConnected) {
        this.socket.emit('update_state', this.gameState);
      }
    },

    // 发送日志
    broadcastLog(msg: string) {
      this.logs.push(msg); // 本地先显示
      if (this.socket) this.socket.emit('send_log', msg);
    },

    // === 2. 游戏业务逻辑 (所有修改都必须最后调用 this.sync()) ===

    // 部署新角色
    addCharacter(char: GameCharacter) {
      // 自动绑定当前创建者的ID
      if (this.currentUser) char.ownerId = this.currentUser.id;
      this.gameState.characters.push(char);
      this.broadcastLog(`🆕 ${char.name} 加入了战场`);
      
      // 如果是场上第一个人，自动设为当前回合
      if(this.gameState.characters.length === 1) {
        this.gameState.currentActorId = char.id;
      }
      this.sync();
    },

    // 移动
    characterMove(charId: string, x: number, y: number) {
      const char = this.gameState.characters.find(c => c.id === charId);
      if (char) {
        // (可选) 这里可以接入 RuleEngine 判断移动力够不够
        // 简单扣除移动力示例: 
        // if(char.actionPoints <= 0) return; 
        
        char.x = x;
        char.y = y;
        this.broadcastLog(`🏃 ${char.name} 移动到了 (${x}, ${y})`);
        this.sync();
      }
    },

    // 释放技能
    useSkill(sourceId: string, targetId: string | null, skill: Skill, targetPos: {x:number, y:number}) {
      const source = this.gameState.characters.find(c => c.id === sourceId);
      if (!source) return;

      // 1. 扣除消耗
      const cost = RuleEngine.parseCost(skill.cost);
      source.mp -= cost.mp;
      source.actionPoints -= cost.action;
      source.bonusPoints -= cost.bonus;
      source.reactionPoints -= cost.reaction;

      this.broadcastLog(`⚔️ ${source.name} 释放了 [${skill.name}]`);

      // 2. 简单的伤害逻辑 (如果选中了目标)
      if (targetId && skill.damage) {
        const target = this.gameState.characters.find(c => c.id === targetId);
        if (target) {
            const dmg = RuleEngine.calcDamage(skill.damage);
            target.hp -= dmg;
            this.broadcastLog(`💥 命中 ${target.name}！造成 ${dmg} 点伤害`);
            if (target.hp <= 0) {
                target.isDead = true;
                this.broadcastLog(`💀 ${target.name} 再起不能！`);
            }
        }
      }
      
      this.sync();
    },

    // 结束回合逻辑
    nextTurn() {
      const order = this.turnOrder;
      if (order.length === 0) return;

      // 找到当前那个人的索引
      const currIdx = order.findIndex(c => c.id === this.gameState.currentActorId);
      
      // 结算旧人的状态 (例如燃烧)
      if(currIdx !== -1) {
          const processed = RuleEngine.processStatus(order[currIdx], 'END');
          // 更新回数组
          const realCharIndex = this.gameState.characters.findIndex(c=>c.id === order[currIdx].id);
          if(realCharIndex !== -1) {
              this.gameState.characters[realCharIndex] = processed.char;
              processed.logs.forEach(l => this.broadcastLog(l));
          }
      }

      // 找下一个人
      let nextIdx = currIdx + 1;
      if (nextIdx >= order.length) {
        nextIdx = 0;
        this.gameState.round++;
        this.broadcastLog(`=== 第 ${this.gameState.round} 回合开始 ===`);
        // (可选) 缩圈逻辑
        if(this.gameState.round % 3 === 0) {
            this.gameState.ringLevel++;
            this.broadcastLog(`☠️ 毒圈缩小了！当前等级: ${this.gameState.ringLevel}`);
        }
      }

      const nextChar = order[nextIdx];
      this.gameState.currentActorId = nextChar.id;

      // 重置新人的行动点
      nextChar.actionPoints = 1;
      nextChar.bonusPoints = 1; // 假设默认回复
      // (可选) 结算 Start 状态...

      this.broadcastLog(`👉 轮到 ${nextChar.name} 行动`);
      this.sync();
    },
    
    // 权限判断 helper
    canControl(charId: string) {
        if (!this.currentUser) return false;
        if (this.isDM) return true; // DM 只有上帝权限
        const char = this.gameState.characters.find(c => c.id === charId);
        return char && char.ownerId === this.currentUser.id;
    },
    
    // 属性修改
    updateStat(charId: string, key: string, val: number) {
       const char = this.gameState.characters.find(c => c.id === charId);
       if(char) {
           (char as any)[key] = val; // 简单粗暴修改
           this.sync();
       }
    }
  }
});