<template>
  <div class="game-container">
    
    <div class="full-screen-modal" v-if="!store.currentUser">
      <div class="login-card">
        <span class="game-title">JOJO TRPG</span>
        <div class="login-form">
          <input class="input-field" v-model="loginForm.name" placeholder="请输入你的昵称" />
          <div class="role-switch">
             <div class="switch-item" :class="{active: loginForm.role === 'PLAYER'}" @click="loginForm.role='PLAYER'">玩家</div>
             <div class="switch-item dm" :class="{active: loginForm.role === 'DM'}" @click="loginForm.role='DM'">DM</div>
          </div>
          <button class="start-btn" @click="handleLogin">开始游戏</button>
        </div>
      </div>
    </div>

    <template v-else>
      <div class="map-viewport">
        <div class="map-padding-box">
          <div class="grid-layer">
            <div v-for="y in 15" :key="y" class="grid-row">
              <div v-for="x in 15" :key="x" 
                class="grid-cell"
                :class="{ 
                  'poison': isPoison(x-1, y-1), 
                  'active': isCurrentPos(x-1, y-1),
                  'target-mode': isTargeting && checkRange(x-1, y-1),
                  'move-mode': moveMode && !getCharByPos(x-1, y-1)
                }"
                @click="handleGridClick(x-1, y-1)"
              >
                <span class="coord-text" v-if="moveMode">{{x-1}},{{y-1}}</span>
                
                <div v-if="getCharByPos(x-1, y-1)" class="char-unit" 
                  :class="[
                    getCharByPos(x-1, y-1)?.team, 
                    { 
                      'self': getCharByPos(x-1, y-1)?.ownerId === store.currentUser.id,
                      'dead': getCharByPos(x-1, y-1)?.isDead
                    }
                  ]">
                  <span class="unit-name">{{ getCharByPos(x-1, y-1)?.name[0] }}</span>
                  <div class="hp-ring" :style="{
                     borderTopColor: getHpColor(getCharByPos(x-1, y-1)),
                     borderRightColor: getHpColor(getCharByPos(x-1, y-1))
                  }"></div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="hud-top">
        <div class="status-pill">
          <span class="pill-text">第 {{ gameState.round }} 回合</span>
          <div class="sep"></div>
          <span class="pill-text warning">毒圈 Lv{{ gameState.ringLevel }}</span>
        </div>
        <div class="status-pill mini" :class="{offline: !isConnected}">
          <div class="dot"></div> {{ isConnected ? '在线' : '离线' }}
        </div>
      </div>

      <div class="hud-timeline">
        <div class="timeline-scroll">
          <div class="timeline-flex">
            <div v-for="c in turnOrder" :key="c.id" 
               class="avatar-circle" 
               :class="{active: c.id === gameState.currentActorId, dead: c.isDead}">
               {{ c.name[0] }}
            </div>
          </div>
        </div>
      </div>

      <div class="fab-group">
        <div class="fab-btn deploy" @click="showCreateModal=true">
          <span>➕</span>
        </div>
        <div class="fab-btn logs" @click="showLogs=!showLogs">
          <span>📝</span>
        </div>
      </div>

      <div class="log-toast" v-if="showLogs" @click="showLogs=false">
         <div class="log-list">
           <div v-for="(log,i) in logs" :key="i" class="log-item">> {{ log }}</div>
         </div>
      </div>

      <div class="bottom-drawer" :class="{ 'drawer-open': !!selectedChar }">
        
        <div class="drawer-placeholder" v-if="!selectedChar">
          <span>点击地图单位查看详情</span>
          <button v-if="store.isDM" class="mini-btn warn" @click="store.nextTurn">DM: 强制下回合</button>
        </div>

        <div class="drawer-content" v-else>
          <div class="char-header">
             <div class="char-info">
               <span class="c-name">{{ selectedChar.name }}</span>
               <span class="c-badge" v-if="selectedChar.ownerId === store.currentUser.id">我</span>
               <span class="c-badge team-red" v-if="selectedChar.team==='RED'">红</span>
               <span class="c-badge team-blue" v-if="selectedChar.team==='BLUE'">蓝</span>
             </div>
             <div class="char-bars">
                <div class="bar-row">
                  <span class="bar-label">HP</span>
                  <div class="prog-bg"><div class="prog-fill red" :style="{width: (selectedChar.hp/selectedChar.maxHp)*100 + '%'}"></div></div>
                  <span class="bar-val">{{selectedChar.hp}}</span>
                </div>
                <div class="bar-row">
                  <span class="bar-label">MP</span>
                  <div class="prog-bg"><div class="prog-fill blue" :style="{width: (selectedChar.mp/selectedChar.maxMp)*100 + '%'}"></div></div>
                  <span class="bar-val">{{selectedChar.mp}}</span>
                </div>
             </div>
          </div>

          <div class="divider"></div>

          <template v-if="store.canControl(selectedChar.id)">
            <div class="ap-grid">
               <div class="ap-box" :class="{has: selectedChar.actionPoints>0}" @click="toggleStat('actionPoints')">
                 <span class="ap-num">{{selectedChar.actionPoints}}</span>
                 <span class="ap-txt">动作</span>
               </div>
               <div class="ap-box" :class="{has: selectedChar.bonusPoints>0}" @click="toggleStat('bonusPoints')">
                 <span class="ap-num">{{selectedChar.bonusPoints}}</span>
                 <span class="ap-txt">附赠</span>
               </div>
               <div class="ap-box" :class="{has: selectedChar.reactionPoints>0}" @click="toggleStat('reactionPoints')">
                 <span class="ap-num">{{selectedChar.reactionPoints}}</span>
                 <span class="ap-txt">反应</span>
               </div>
            </div>

            <div class="skill-scroll" v-if="!viewingSkill">
               <div class="skill-container">
                  <div class="skill-chip move" :class="{active: moveMode}" @click="moveMode = !moveMode">
                     <span>🦶 移动</span>
                  </div>
                  <div class="skill-chip" v-for="(s, idx) in selectedChar.skills" :key="idx" @click="selectSkill(s)">
                     <span>{{s.name}}</span>
                     <span class="s-cost">{{s.cost}}</span>
                  </div>
               </div>
            </div>

            <div class="skill-confirm" v-else>
               <div class="sc-info">
                  <span class="sc-name">{{viewingSkill.name}}</span>
                  <span class="sc-desc">{{viewingSkill.description}}</span>
               </div>
               <div class="sc-actions">
                  <button class="mini-btn" @click="viewingSkill=null;isTargeting=false">返回</button>
                  <button class="mini-btn primary" :disabled="isTargeting" @click="startTargeting">
                    {{ isTargeting ? '请点击地图对象' : '释放' }}
                  </button>
               </div>
            </div>

            <div class="bottom-actions">
               <div class="tool-btn" @click="rollD20">🎲 D20</div>
               <div class="tool-btn" v-if="store.isDM" @click="modifyHP(-1)">-1 HP</div>
               <div class="tool-btn" v-if="store.isDM" @click="modifyHP(1)">+1 HP</div>
               <button class="end-turn-btn" 
                  v-if="selectedChar.id === gameState.currentActorId" 
                  @click="store.nextTurn">
                  结束回合
               </button>
            </div>

          </template>

          <template v-else>
             <div class="observer-msg">👁️ 正在观察模式</div>
          </template>
        </div>
      </div>

      <div class="modal-mask" v-if="showCreateModal">
         <div class="modal-body">
            <div class="modal-title">部署单位</div>
            
            <div class="select-wrapper">
                <select class="native-select" v-model="form.presetIndex" @change="onPresetChange">
                    <option :value="-1" disabled>点击选择模板</option>
                    <option v-for="(name, idx) in presetNames" :key="idx" :value="idx">{{ name }}</option>
                </select>
                <div class="select-arrow">▼</div>
            </div>

            <div class="coord-row">
               <input type="number" v-model.number="form.x" placeholder="X" />
               <input type="number" v-model.number="form.y" placeholder="Y" />
            </div>
            <div class="team-tabs">
               <div :class="{active: form.team==='RED'}" @click="form.team='RED'" style="color:#f44336">红队</div>
               <div :class="{active: form.team==='BLUE'}" @click="form.team='BLUE'" style="color:#2196f3">蓝队</div>
            </div>
            <div class="modal-btns">
               <button class="cancel-btn" @click="showCreateModal=false">取消</button>
               <button class="confirm-btn" @click="confirmCreate">确定部署</button>
            </div>
         </div>
      </div>

    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, toRefs } from 'vue';
import { useGameStore } from '@/stores/game';
import { RuleEngine } from '@/utils/ruleEngine';
import { CHARACTER_PRESETS } from '@/data/presets';
import type { Skill, GameCharacter } from '@/types/game';

const store = useGameStore();
const { gameState, logs, isConnected, turnOrder, currentUser } = toRefs(store);

// UI State
const showLogs = ref(false);
const showCreateModal = ref(false);
const selectedCharId = ref('');
const moveMode = ref(false);
const isTargeting = ref(false);
const viewingSkill = ref<Skill | null>(null);

// Login & Form
const loginForm = reactive({ name: '', role: 'PLAYER' as 'DM'|'PLAYER' });
const presetNames = CHARACTER_PRESETS.map(c => c.name);
const form = reactive({ name: '', team: 'RED', x: 0, y: 0, presetIndex: -1 });

// === Helpers ===
// H5 提示工具函数 (替代 uni.showToast)
const showToast = (title: string) => {
    // 简单实现，实际项目中可以使用 vant 或 element-plus 的 toast
    alert(title); 
};

const getCharByPos = (x:number, y:number) => gameState.value.characters?.find(c => c.x === x && c.y === y && !c.isDead);
const isPoison = (x:number, y:number) => {
  const l = gameState.value.ringLevel;
  return x < l || x > 14-l || y < l || y > 14-l;
};
const isCurrentPos = (x:number, y:number) => {
  const c = gameState.value.characters?.find(c => c.id === gameState.value.currentActorId);
  return c && c.x === x && c.y === y;
};
const selectedChar = computed(() => gameState.value.characters?.find(c => c.id === selectedCharId.value));

const getHpColor = (char: GameCharacter | undefined) => {
    if(!char) return 'transparent';
    const pct = char.hp / char.maxHp;
    if(pct > 0.6) return '#4caf50'; // Green
    if(pct > 0.3) return '#ff9800'; // Orange
    return '#f44336'; // Red
}

// === Interaction ===
const handleLogin = () => { 
  if(!loginForm.name) return;
  store.login(loginForm.name, loginForm.role); 
};

const handleGridClick = (x: number, y: number) => {
  const targetChar = getCharByPos(x, y);

  // 1. Skill Target
  if (isTargeting.value && selectedChar.value && viewingSkill.value) {
    if (!store.canControl(selectedChar.value.id)) return;
    const dist = RuleEngine.getDistance(selectedChar.value.x, selectedChar.value.y, x, y);
    if (dist > viewingSkill.value.range) {
      showToast('距离太远');
      return;
    }
    store.useSkill(selectedChar.value.id, targetChar?.id || null, viewingSkill.value, {x, y});
    resetModes();
    return;
  }

  // 2. Move
  if (moveMode.value && selectedChar.value) {
    if (!store.canControl(selectedChar.value.id)) {
       showToast('无权限'); return;
    }
    store.characterMove(selectedChar.value.id, x, y);
    moveMode.value = false;
    return;
  }

  // 3. Select
  if (targetChar) {
    selectedCharId.value = targetChar.id;
    resetModes();
  } else {
    // 点击空白处取消选中，收起面板
    selectedCharId.value = '';
    resetModes();
  }
};

const resetModes = () => {
    isTargeting.value = false;
    viewingSkill.value = null;
    moveMode.value = false;
}

// === Controls ===
const selectSkill = (skill: Skill) => {
  moveMode.value = false;
  viewingSkill.value = skill;
};

const startTargeting = () => {
  if (!selectedChar.value || !viewingSkill.value) return;
  const cost = RuleEngine.parseCost(viewingSkill.value.cost);
  if(selectedChar.value.mp < cost.mp || selectedChar.value.actionPoints < cost.action) {
    showToast('资源不足');
    return;
  }
  isTargeting.value = true;
};

const checkRange = (x:number, y:number) => {
  if(!selectedChar.value || !viewingSkill.value) return false;
  const dist = RuleEngine.getDistance(selectedChar.value.x, selectedChar.value.y, x, y);
  return dist <= viewingSkill.value.range;
};

const toggleStat = (key: string) => {
    if(selectedChar.value && store.canControl(selectedChar.value.id)){
        store.updateStat(selectedChar.value.id, key, selectedChar.value[key]>0?0:1);
    }
};

const rollD20 = () => {
  const res = RuleEngine.rollD20();
  store.broadcastLog(`${selectedChar.value?.name} 掷骰: ${res.msg}`);
};

const modifyHP = (v:number) => {
  if(selectedChar.value) {
    selectedChar.value.hp += v;
    store.sync(); 
  }
}

// === Create ===
const onPresetChange = (e: Event) => {
  // H5 Event Handling
  const target = e.target as HTMLSelectElement;
  const idx = parseInt(target.value);
  form.presetIndex = idx;
  if (idx >= 0 && CHARACTER_PRESETS[idx]) {
      form.name = CHARACTER_PRESETS[idx].name;
  }
};

const confirmCreate = () => {
  if(form.presetIndex < 0) return;
  const base = CHARACTER_PRESETS[form.presetIndex];
  const newChar = {
    id: 'c_' + Date.now() + Math.floor(Math.random()*100),
    ownerId: '', 
    name: form.name, team: form.team,
    hp: base.maxHp, maxHp: base.maxHp,
    mp: base.maxMp, maxMp: base.maxMp,
    speed: base.speed, range: base.range, initiative: base.initiative,
    actionPoints: 1, bonusPoints: 1, reactionPoints: 1,
    x: form.x, y: form.y,
    skills: base.skills, passives: base.passives, statuses: [], isDead: false
  };
  store.addCharacter(newChar as any);
  showCreateModal.value = false;
};
</script>

<style scoped>
/* 🌑 深色游戏主题 */
:root {
  --bg-dark: #121212;
  --bg-panel: #1e1e1e;
  --primary: #ffd700;
  --accent-red: #ff5252;
  --accent-blue: #448aff;
  --grid-size: 50px; 
}

/* 全局滚动条隐藏 */
::-webkit-scrollbar {
  display: none;
}

.game-container {
  width: 100vw; height: 100vh;
  background-color: #121212; /* var(--bg-dark) fallback */
  position: relative;
  overflow: hidden;
  color: #eee;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  user-select: none; /* 防止长按选中文本 */
}

/* 1. 地图层 */
.map-viewport {
  width: 100%; height: 100%;
  position: absolute; top: 0; left: 0;
  z-index: 1; 
  overflow: auto; /* 允许滚动 */
  -webkit-overflow-scrolling: touch;
}
.map-padding-box {
  padding: 100px; 
  min-width: calc(15 * 50px + 200px);
  min-height: calc(15 * 50px + 200px);
  display: flex; justify-content: center; align-items: center;
}
.grid-layer {
  display: inline-block;
  background: #000;
  border: 2px solid #333;
  box-shadow: 0 0 20px rgba(0,0,0,0.5);
}
.grid-row { display: flex; }
.grid-cell {
  width: 50px; height: 50px;
  border: 1px solid #2a2a2a;
  position: relative;
  display: flex; align-items: center; justify-content: center;
  box-sizing: border-box;
}
.grid-cell.poison { background: rgba(100, 0, 100, 0.2); border-color: #500050; }
.grid-cell.active { background: rgba(255, 215, 0, 0.15); box-shadow: inset 0 0 10px rgba(255, 215, 0, 0.2); }
.grid-cell.target-mode { background: rgba(255, 0, 0, 0.15); cursor: crosshair; }
.grid-cell.move-mode { background: rgba(0, 255, 0, 0.1); border: 1px dashed #00ff00; }

.coord-text { position: absolute; top: 2px; left: 2px; font-size: 8px; color: #555; pointer-events: none; }

/* 棋子 */
.char-unit {
  width: 80%; height: 80%;
  border-radius: 50%;
  background: #444; color: #fff;
  display: flex; align-items: center; justify-content: center;
  font-weight: bold; font-size: 16px;
  position: relative;
  box-shadow: 0 4px 6px rgba(0,0,0,0.5);
  transition: transform 0.1s;
  cursor: pointer;
}
.char-unit.RED { background: #b71c1c; }
.char-unit.BLUE { background: #0d47a1; }
.char-unit.self { border: 2px solid #00e676; z-index: 10; transform: scale(1.1); }
.char-unit.dead { filter: grayscale(1); opacity: 0.5; transform: scale(0.8); }

.hp-ring {
    position: absolute; inset: -3px; border-radius: 50%;
    border: 3px solid transparent; 
    transform: rotate(-45deg);
}

/* 2. HUD 悬浮层 */
.hud-top {
  position: absolute; top: 0; left: 0; width: 100%;
  z-index: 50; padding: 10px; box-sizing: border-box;
  display: flex; justify-content: space-between; pointer-events: none;
}
.status-pill {
  background: rgba(0,0,0,0.8); backdrop-filter: blur(4px);
  padding: 6px 12px; border-radius: 20px;
  display: flex; align-items: center; gap: 8px;
  font-size: 12px; border: 1px solid #444;
}
.sep { width: 1px; height: 12px; background: #555; }
.warning { color: #ff9800; }
.dot { width: 6px; height: 6px; background: #00e676; border-radius: 50%; }
.offline .dot { background: #666; }

.hud-timeline {
  position: absolute; top: 50px; left: 0; width: 100%; z-index: 40;
  pointer-events: none;
}
.timeline-scroll { 
    width: 100%; white-space: nowrap; padding-left: 10px; 
    overflow-x: auto;
}
.timeline-flex { display: flex; gap: 8px; padding-bottom: 10px; }
.avatar-circle {
  width: 32px; height: 32px; border-radius: 50%;
  background: #333; border: 2px solid #555;
  display: flex; align-items: center; justify-content: center;
  font-size: 12px; color: #aaa; flex-shrink: 0;
  box-shadow: 0 2px 4px rgba(0,0,0,0.5);
}
.avatar-circle.active { border-color: #ffd700; transform: scale(1.2); color: #fff; background: #444; z-index: 2; }

/* 悬浮按钮 (FAB) */
.fab-group {
  position: absolute; right: 20px; bottom: 240px; 
  display: flex; flex-direction: column; gap: 15px; z-index: 60;
}
.fab-btn {
  width: 48px; height: 48px; border-radius: 50%;
  background: #1e1e1e; border: 1px solid #444;
  display: flex; align-items: center; justify-content: center;
  font-size: 20px; box-shadow: 0 4px 10px rgba(0,0,0,0.5);
  cursor: pointer;
}
.fab-btn.deploy { background: #ffd700; color: #000; border: none; }

/* 3. 底部抽屉 */
.bottom-drawer {
  position: absolute; bottom: 0; left: 0; width: 100%;
  background: #1e1e1e;
  border-top-left-radius: 16px; border-top-right-radius: 16px;
  box-shadow: 0 -4px 20px rgba(0,0,0,0.6);
  z-index: 100;
  transition: transform 0.3s cubic-bezier(0.25, 0.8, 0.5, 1);
  transform: translateY(calc(100% - 40px));
}
.bottom-drawer.drawer-open {
  transform: translateY(0);
}

.drawer-placeholder {
  height: 40px; display: flex; align-items: center; justify-content: center;
  color: #666; font-size: 12px; gap: 10px; cursor: pointer;
}
.drawer-content {
  padding: 15px; padding-bottom: 30px;
  display: flex; flex-direction: column; gap: 15px;
}

.char-header { display: flex; justify-content: space-between; align-items: flex-start; }
.c-name { font-size: 20px; font-weight: bold; color: #fff; margin-right: 8px; }
.c-badge { font-size: 10px; background: #333; padding: 2px 6px; border-radius: 4px; margin-right: 4px; vertical-align: middle; }
.team-red { background: #b71c1c; } .team-blue { background: #0d47a1; }

.char-bars { flex: 1; max-width: 150px; margin-left: 10px; }
.bar-row { display: flex; align-items: center; gap: 5px; font-size: 10px; margin-bottom: 4px; }
/* 模拟进度条 */
.prog-bg { flex: 1; height: 6px; background: #333; border-radius: 3px; overflow: hidden; }
.prog-fill { height: 100%; transition: width 0.3s; }
.prog-fill.red { background: #ff5252; }
.prog-fill.blue { background: #448aff; }

.divider { height: 1px; background: #333; width: 100%; }

.ap-grid { display: flex; gap: 10px; }
.ap-box { 
  flex: 1; background: #252525; border-radius: 8px; padding: 8px;
  display: flex; flex-direction: column; align-items: center;
  opacity: 0.4; border: 1px solid transparent; cursor: pointer;
}
.ap-box.has { opacity: 1; border-color: #555; background: #333; }
.ap-num { font-size: 18px; font-weight: bold; color: #ffd700; }
.ap-txt { font-size: 10px; color: #888; }

.skill-scroll { width: 100%; overflow-x: auto; }
.skill-container { display: flex; gap: 10px; padding: 5px 0; }
.skill-chip {
  background: #333; padding: 8px 16px; border-radius: 20px;
  display: inline-flex; flex-direction: column; align-items: center;
  min-width: 80px; border: 1px solid #444; white-space: nowrap;
  cursor: pointer;
}
.skill-chip.move { border-color: #00e676; color: #00e676; }
.skill-chip.move.active { background: #00e676; color: #000; }
.s-cost { font-size: 10px; color: #666; margin-top: 2px; }

.skill-confirm { background: #2a2a2a; padding: 10px; border-radius: 8px; display: flex; justify-content: space-between; align-items: center; }
.sc-info { max-width: 60%; }
.sc-name { color: #ffd700; font-size: 14px; font-weight: bold; display: block; }
.sc-desc { color: #aaa; font-size: 12px; }
.sc-actions { display: flex; gap: 10px; }

.bottom-actions { display: flex; gap: 10px; margin-top: 5px; }
.tool-btn { flex: 1; background: #333; color: #ccc; text-align: center; line-height: 40px; border-radius: 8px; font-size: 14px; cursor: pointer; }
.end-turn-btn { flex: 2; background: #d32f2f; color: #fff; line-height: 40px; border-radius: 8px; border: none; cursor: pointer; font-size: 14px; }

/* 登录 & 弹窗 */
.full-screen-modal {
  position: fixed; inset: 0; background: #000; z-index: 999;
  display: flex; align-items: center; justify-content: center;
}
.login-card { width: 80%; text-align: center; max-width: 400px; }
.game-title { font-size: 40px; font-weight: 900; letter-spacing: 4px; color: #ffd700; margin-bottom: 40px; display: block; }
.input-field { 
    background: #222; height: 50px; border-radius: 25px; text-align: center; color: #fff; 
    margin-bottom: 20px; border: 1px solid #333; width: 100%; box-sizing: border-box; font-size: 16px;
}
.role-switch { display: flex; background: #222; padding: 5px; border-radius: 25px; margin-bottom: 30px; }
.switch-item { flex: 1; line-height: 40px; border-radius: 20px; color: #666; cursor: pointer; }
.switch-item.active { background: #333; color: #fff; font-weight: bold; }
.start-btn { 
    background: #ffd700; color: #000; border-radius: 25px; font-weight: bold; 
    width: 100%; height: 50px; border: none; font-size: 16px; cursor: pointer;
}

/* 辅助类 & Modal */
.modal-mask { position: fixed; inset: 0; background: rgba(0,0,0,0.8); z-index: 200; display: flex; align-items: center; justify-content: center; }
.modal-body { width: 80%; max-width: 400px; background: #222; padding: 20px; border-radius: 12px; }
.modal-title { text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 20px; color: #fff; }

/* 简单的 Select 样式 */
.select-wrapper { position: relative; margin-bottom: 15px; }
.native-select {
    width: 100%; background: #333; color: #fff; border: 1px solid #444;
    padding: 10px; border-radius: 8px; appearance: none; font-size: 16px; text-align: center;
}
.select-arrow { position: absolute; right: 10px; top: 12px; color: #888; font-size: 12px; pointer-events: none; }

.coord-row { display: flex; gap: 10px; margin-bottom: 15px; }
.coord-row input { flex: 1; background: #333; height: 40px; text-align: center; border-radius: 8px; border: 1px solid #444; color: #fff; }
.team-tabs { display: flex; border: 1px solid #444; border-radius: 8px; margin-bottom: 20px; overflow: hidden; cursor: pointer; }
.team-tabs div { flex: 1; text-align: center; padding: 10px 0; background: #222; }
.team-tabs div.active { background: #333; font-weight: bold; }
.modal-btns { display: flex; gap: 10px; }
.modal-btns button { flex: 1; height: 40px; border-radius: 6px; border: none; font-weight: bold; cursor: pointer; }
.cancel-btn { background: #444; color: #ccc; }
.confirm-btn { background: #4caf50; color: #fff; }
.mini-btn { font-size: 12px; padding: 4px 10px; border-radius: 4px; border: none; cursor: pointer; background: #444; color: #fff; }
.mini-btn.primary { background: #4caf50; }
.mini-btn.warn { background: #d32f2f; }

.log-toast { position: fixed; bottom: 200px; left: 20px; width: 200px; background: rgba(0,0,0,0.8); padding: 10px; border-radius: 8px; z-index: 90; }
.log-list { height: 100px; overflow-y: auto; }
.log-item { font-size: 10px; color: #ccc; margin-bottom: 4px; }
</style>