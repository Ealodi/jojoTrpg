<template>
  <div class="game-layout" v-if="store.currentRoom">
    <div class="map-container">
      <div class="grid-map" :style="gridStyle">
        <div 
          v-for="index in totalCells" 
          :key="index"
          class="cell"
          :class="{ 
            'highlight-move': currentMode === 'move' && isMovable(index),
            'highlight-skill': currentMode === 'skill' && isSkillInRange(index)
          }"
          @click="handleCellClick(index)"
        >
          <span class="coord-debug">{{ getX(index) }},{{ getY(index) }}</span>
          
          <div 
            v-if="getPlayerAt(index)" 
            class="player-token"
            :class="{ 'is-me': getPlayerAt(index)?.userId === store.myPlayer?.userId }"
          >
            {{ getPlayerAt(index)?.stats.name[0] }} 
            <div class="hp-bar">{{ getPlayerAt(index)?.stats.currentHp }}</div>
          </div>
        </div>
      </div>
    </div>

    <div class="status-panel">
      <div v-if="store.myPlayer" class="my-stats">
        <h3>{{ store.myPlayer.stats.name }}</h3>
        <p>HP: {{ store.myPlayer.stats.currentHp }} / {{ store.myPlayer.stats.maxHp }}</p>
        <p>能量: {{ store.myPlayer.stats.currentEnergy }}</p>
        <div class="resources">
          <span class="tag">动作: {{ store.myPlayer.stats.actions }}</span>
          <span class="tag">附赠: {{ store.myPlayer.stats.bonusActions }}</span>
        </div>
      </div>
      
      <hr />

      <div class="actions-area">
        <h4>行动指令</h4>
        
        <button 
          class="action-btn" 
          :class="{ active: currentMode === 'move' }"
          @click="toggleMoveMode"
          :disabled="store.myPlayer?.stats.bonusActions <= 0"
        >
          移动 (消耗附赠)
        </button>

        <div class="skills-list" v-if="mySkills.length > 0">
          <button 
            v-for="skill in mySkills" 
            :key="skill.id"
            class="skill-btn"
            :class="{ active: currentMode === 'skill' && selectedSkill?.id === skill.id }"
            @click="selectSkill(skill)"
            :disabled="store.myPlayer && (store.myPlayer.stats.actions < skill.actionCost || store.myPlayer.stats.currentEnergy < skill.energyCost)"
          >
            {{ skill.name }} 
            <span class="cost">({{ skill.energyCost }}能/{{ skill.range }}距)</span>
          </button>
        </div>
        
        <button class="cancel-btn" v-if="currentMode !== 'none'" @click="currentMode = 'none'">取消选择</button>
      </div>

      <hr />
      
      <div class="logs">
         <p>房间: {{ store.currentRoom.roomId }}</p>
      </div>
    </div>

    <div v-if="store.isReacting" class="reaction-overlay">
       <div class="reaction-box">
         <h3>⚠️ 警告！即将受到攻击</h3>
         <p><strong>{{ store.reactionContext.attackerName }}</strong> 正在对你使用 <strong>{{ store.reactionContext.skillName }}</strong></p>
         <p class="warning-text">消耗 1 反应 + 下回合 1 附赠动作</p>
         
         <div class="reaction-buttons">
           <el-button type="warning" @click="store.sendReaction(1)">
             🏃 闪避 (增加闪避率)
           </el-button>
           <el-button type="primary" @click="store.sendReaction(2)">
             🛡️ 格挡 (伤害减半)
           </el-button>
           <el-button type="danger" @click="store.sendReaction(3)">
             ⚔️ 反击 (先手一击)
           </el-button>
           <el-divider>或者</el-divider>
           <el-button type="info" @click="store.sendReaction(0)">
             ❌ 不反应 (直接承受)
           </el-button>
         </div>
       </div>
     </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useGameStore } from '../stores/gameStore'

const store = useGameStore()

// --- 状态管理 ---
type Mode = 'none' | 'move' | 'skill'
const currentMode = ref<Mode>('none')
const selectedSkill = ref<any>(null)

// --- 地图基础数据 ---
const mapSize = computed(() => store.currentRoom?.mapSize || 20)
const totalCells = computed(() => mapSize.value * mapSize.value)
const gridStyle = computed(() => ({
  gridTemplateColumns: `repeat(${mapSize.value}, 1fr)`,
  gridTemplateRows: `repeat(${mapSize.value}, 1fr)`
}))

// --- 坐标辅助 ---
const getX = (index: number) => (index - 1) % mapSize.value
const getY = (index: number) => Math.floor((index - 1) / mapSize.value)
const getPlayerAt = (index: number) => {
  const x = getX(index)
  const y = getY(index)
  return store.currentRoom?.players.find((p: any) => p.x === x && p.y === y)
}

// --- 硬编码的前端技能数据 (理想情况应从后端获取) ---
const mySkills = computed(() => {
  const name = store.myPlayer?.stats.name
  if (name === '承太郎') {
    return [
      { id: 'ora', name: '欧拉欧拉', energyCost: 1, actionCost: 1, range: 1 },
      { id: 'star_finger', name: '流星指刺', energyCost: 2, actionCost: 1, range: 4 }
    ]
  }
  if (name === 'DIO') {
    return [
      { id: 'muda', name: '木大木大', energyCost: 1, actionCost: 1, range: 1 },
      { id: 'knives', name: '飞刀', energyCost: 1, actionCost: 1, range: 3 }
    ]
  }
  return []
})

// --- 模式切换逻辑 ---
const toggleMoveMode = () => {
  if (currentMode.value === 'move') currentMode.value = 'none'
  else currentMode.value = 'move'
  selectedSkill.value = null
}

const selectSkill = (skill: any) => {
  if (currentMode.value === 'skill' && selectedSkill.value?.id === skill.id) {
    currentMode.value = 'none'
    selectedSkill.value = null
  } else {
    currentMode.value = 'skill'
    selectedSkill.value = skill
  }
}

// --- 范围高亮判定 ---
const isMovable = (index: number) => {
  if (!store.myPlayer) return false
  const x = getX(index)
  const y = getY(index)
  const dist = Math.abs(store.myPlayer.x - x) + Math.abs(store.myPlayer.y - y)
  // 必须小于速度且不是自己脚下
  return dist <= store.myPlayer.stats.speed && dist > 0
}

const isSkillInRange = (index: number) => {
  if (!store.myPlayer || !selectedSkill.value) return false
  const x = getX(index)
  const y = getY(index)
  const dist = Math.abs(store.myPlayer.x - x) + Math.abs(store.myPlayer.y - y)
  return dist <= selectedSkill.value.range
}

// --- 核心点击处理 ---
const handleCellClick = (index: number) => {
  const x = getX(index)
  const y = getY(index)

  if (currentMode.value === 'move') {
    if (isMovable(index)) {
      store.movePiece(x, y)
      currentMode.value = 'none'
    }
  } else if (currentMode.value === 'skill' && selectedSkill.value) {
    // 允许点击空地(某些位移技能)或点击敌人
    if (isSkillInRange(index)) {
      store.useSkill(selectedSkill.value.id, x, y)
      currentMode.value = 'none'
      selectedSkill.value = null
    }
  }
}
</script>

<style scoped>
.game-layout { display: flex; height: 100vh; font-family: sans-serif; }
.map-container { flex: 3; background: #222; padding: 20px; display: flex; justify-content: center; align-items: center; }
.grid-map { display: grid; width: 600px; height: 600px; background: #fff; gap: 1px; border: 4px solid #444; }

.cell { background: #eee; position: relative; cursor: default; display: flex; justify-content: center; align-items: center; }
/* 高亮样式 */
.highlight-move { background: #a8d8ea; cursor: pointer; }
.highlight-move:hover { background: #8ecae6; }
.highlight-skill { background: #ffcccb; cursor: crosshair; }
.highlight-skill:hover { background: #ffaaaa; }

.player-token { width: 80%; height: 80%; border-radius: 50%; background: #e74c3c; color: white; display: flex; justify-content: center; align-items: center; position: relative; font-weight: bold; z-index: 2; }
.player-token.is-me { background: #2ecc71; border: 2px solid white; box-shadow: 0 0 5px rgba(0,0,0,0.5); }
.hp-bar { position: absolute; bottom: -6px; background: #000; color: #fff; font-size: 9px; padding: 0 4px; border-radius: 4px; }
.coord-debug { position: absolute; top: 1px; left: 1px; font-size: 8px; color: #ccc; }

.status-panel { flex: 1; background: #f9f9f9; padding: 20px; border-left: 1px solid #ccc; display: flex; flex-direction: column; }
.resources { margin-top: 10px; display: flex; gap: 10px; }
.tag { background: #ddd; padding: 2px 8px; border-radius: 4px; font-size: 0.9em; }

.actions-area { margin-top: 20px; }
.action-btn, .skill-btn, .cancel-btn { display: block; width: 100%; margin-bottom: 8px; padding: 10px; border: 1px solid #ccc; background: white; cursor: pointer; text-align: left; }
.action-btn:hover, .skill-btn:hover { background: #f0f0f0; }
.action-btn.active, .skill-btn.active { background: #333; color: white; border-color: #333; }
.cancel-btn { background: #ffebeb; color: #d00; text-align: center; margin-top: 10px; }
.skill-btn .cost { float: right; font-size: 0.8em; color: #888; }
.skill-btn.active .cost { color: #ccc; }
button:disabled { opacity: 0.5; cursor: not-allowed; }
</style>